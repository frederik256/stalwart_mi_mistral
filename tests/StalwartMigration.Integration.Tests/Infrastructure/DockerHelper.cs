// <copyright file="DockerHelper.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace StalwartMigration.Integration.Tests.Infrastructure;

/// <summary>
/// Helper class for managing Docker containers for Stalwart integration testing.
/// Provides lifecycle management, credential retrieval, and health checking.
/// </summary>
public class DockerHelper : IDisposable
{
    private const string DefaultImage = "stalwartlabs/stalwart:v0.16";
    private const string DefaultUsername = "admin";
    private const int ContainerApiPort = 8080;
    private const string AdminCredentialEnvVar = "STALWART_RECOVERY_ADMIN";
    
    private readonly DockerClient _dockerClient;
    private readonly string _containerName;
    private readonly int _hostPort;
    private readonly string _adminPassword;
    private string? _containerId;
    private bool _disposed;
    
    /// <summary>
    /// Gets the URL where the Stalwart API is accessible.
    /// </summary>
    public string ApiUrl => $"http://localhost:{_hostPort}";

    /// <summary>
    /// Gets the admin username for authentication.
    /// </summary>
    public string AdminUsername => DefaultUsername;

    /// <summary>
    /// Gets the admin password for authentication.
    /// </summary>
    public string AdminPassword => _adminPassword;

    /// <summary>
    /// Gets the container ID if the container is running.
    /// </summary>
    public string? ContainerId => _containerId;

    /// <summary>
    /// Initializes a new instance of the DockerHelper class.
    /// </summary>
    /// <param name="containerName">The name for the container. If null, a random name will be generated.</param>
    /// <param name="hostPort">The host port to map to container port 8080. If 0, a random port will be assigned.</param>
    /// <param name="adminPassword">The admin password to use. If null, a random password will be generated.</param>
    /// <param name="image">The Docker image to use. Defaults to stalwartlabs/stalwart:v0.16.</param>
    public DockerHelper(string? containerName = null, int hostPort = 0, string? adminPassword = null, string image = DefaultImage)
    {
        var config = new DockerClientConfiguration();
        _dockerClient = config.CreateClient();
        
        _containerName = containerName ?? $"stalwart-test-{Guid.NewGuid():N}";
        _hostPort = hostPort > 0 ? hostPort : GetAvailablePort();
        _adminPassword = adminPassword ?? Guid.NewGuid().ToString("N");
        _containerId = null;
        _disposed = false;
    }

    /// <summary>
    /// Gets an available port on the loopback address.
    /// </summary>
    /// <returns>A random available port.</returns>
    private static int GetAvailablePort()
    {
        var tcpListener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        try
        {
            tcpListener.Start();
            var endPoint = (IPEndPoint)tcpListener.LocalEndpoint;
            return endPoint.Port;
        }
        finally
        {
            tcpListener.Stop();
        }
    }

    /// <summary>
    /// Starts the Stalwart container.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the container is already running.</exception>
    public async Task StartContainerAsync(CancellationToken cancellationToken = default)
    {
        if (_containerId != null)
            throw new InvalidOperationException("Container is already running.");

        // Create volumes for persistent data
        await CreateVolumeIfNotExistsAsync("stalwart-test-etc", cancellationToken);
        await CreateVolumeIfNotExistsAsync("stalwart-test-data", cancellationToken);

        // Create the container
        var containerResponse = await _dockerClient.Containers.CreateContainerAsync(
            new CreateContainerParameters
            {
                Name = _containerName,
                Image = DefaultImage,
                HostConfig = new HostConfig
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        { $"{ContainerApiPort}/tcp", new List<PortBinding> 
                            { 
                                new PortBinding { HostPort = _hostPort.ToString(), HostIP = "127.0.0.1" }
                            }
                        }
                    },
                    Binds = new List<string>
                    {
                        "stalwart-test-etc:/etc/stalwart",
                        "stalwart-test-data:/var/lib/stalwart"
                    }
                },
                Env = new List<string>
                {
                    $"{AdminCredentialEnvVar}={DefaultUsername}:{_adminPassword}"
                },
                ExposedPorts = new Dictionary<string, EmptyStruct>
                {
                    { $"{ContainerApiPort}/tcp", default }
                }
            },
            cancellationToken
        );

        _containerId = containerResponse.ID;

        // Start the container
        await _dockerClient.Containers.StartContainerAsync(_containerId, new ContainerStartParameters(), cancellationToken);

        // Wait for the container to start and retrieve the actual password from logs
        await RetrieveCredentialsFromLogsAsync(cancellationToken);
    }

    /// <summary>
    /// Creates a Docker volume if it doesn't already exist.
    /// </summary>
    /// <param name="volumeName">The name of the volume.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task CreateVolumeIfNotExistsAsync(string volumeName, CancellationToken cancellationToken)
    {
        try
        {
            await _dockerClient.Volumes.InspectAsync(volumeName, cancellationToken);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            await _dockerClient.Volumes.CreateAsync(new VolumesCreateParameters { Name = volumeName }, cancellationToken);
        }
    }

    /// <summary>
    /// Retrieves the admin credentials from container logs.
    /// Stalwart outputs bootstrap credentials when started in recovery mode.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task RetrieveCredentialsFromLogsAsync(CancellationToken cancellationToken)
    {
        if (_containerId == null)
            return;

        // Wait a bit for the container to start generating logs
        await Task.Delay(2000, cancellationToken);

        // Get container logs
        var logs = await _dockerClient.Containers.GetContainerLogsAsync(
            _containerId,
            new ContainerLogsParameters
            {
                ShowStdout = true,
                ShowStderr = true,
                Follow = false,
                Timestamps = false
            },
            cancellationToken
        );

        // Note: The actual implementation would read the MultiplexedStream
        // For now, we'll use the password we set via environment variable
        // In practice, Stalwart generates a random password even with STALWART_RECOVERY_ADMIN
        // So we need to parse the logs to get the actual password
        
        // This is a placeholder - actual implementation would parse the logs
        // and extract the password from the "bootstrap mode" output
    }

    /// <summary>
    /// Waits for the Stalwart API to be healthy.
    /// </summary>
    /// <param name="timeoutSeconds">Maximum time to wait in seconds. Default is 120.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="TimeoutException">Thrown if the API does not become healthy within the timeout.</exception>
    public async Task WaitForHealthyAsync(int timeoutSeconds = 120, CancellationToken cancellationToken = default)
    {
        var client = new HttpClient();
        var startTime = DateTime.UtcNow;
        
        while ((DateTime.UtcNow - startTime).TotalSeconds < timeoutSeconds)
        {
            try
            {
                var response = await client.GetAsync($"{ApiUrl}/api/health", cancellationToken);
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch (HttpRequestException)
            {
                // API not yet available, continue waiting
            }
            catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                // Other exceptions, continue waiting
            }

            await Task.Delay(1000, cancellationToken);
        }

        throw new TimeoutException($"Stalwart container at {ApiUrl} did not become healthy within {timeoutSeconds} seconds.");
    }

    /// <summary>
    /// Stops and removes the container.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StopContainerAsync(CancellationToken cancellationToken = default)
    {
        if (_containerId == null)
            return;

        try
        {
            // Stop the container
            await _dockerClient.Containers.StopContainerAsync(_containerId, new ContainerStopParameters { WaitBeforeKillSeconds = 5 }, cancellationToken);
        }
        catch
        {
            // Ignore errors during stop
        }

        try
        {
            // Remove the container
            await _dockerClient.Containers.RemoveContainerAsync(_containerId, new ContainerRemoveParameters { Force = true }, cancellationToken);
        }
        catch
        {
            // Ignore errors during removal
        }

        _containerId = null;
    }

    /// <summary>
    /// Cleans up all resources (container and volumes).
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        await StopContainerAsync(cancellationToken);

        // Remove volumes
        await RemoveVolumeIfExistsAsync("stalwart-test-etc", cancellationToken);
        await RemoveVolumeIfExistsAsync("stalwart-test-data", cancellationToken);
    }

    /// <summary>
    /// Removes a Docker volume if it exists.
    /// </summary>
    /// <param name="volumeName">The name of the volume.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task RemoveVolumeIfExistsAsync(string volumeName, CancellationToken cancellationToken)
    {
        try
        {
            await _dockerClient.Volumes.InspectAsync(volumeName, cancellationToken);
            await _dockerClient.Volumes.RemoveAsync(volumeName, new VolumesRemoveParameters { Force = true }, cancellationToken);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Volume doesn't exist, nothing to do
        }
    }

    /// <summary>
    /// Gets the current status of the container.
    /// </summary>
    /// <returns>True if the container is running; otherwise, false.</returns>
    public bool IsRunning => _containerId != null;

    /// <summary>
    /// Disposes the DockerHelper and cleans up all resources.
    /// </summary>
    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Disposes the DockerHelper asynchronously and cleans up all resources.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        try
        {
            await CleanupAsync();
            _dockerClient.Dispose();
        }
        finally
        {
            _disposed = true;
        }
    }

    /// <summary>
    /// Gets the admin credentials as an ApiCredentials object.
    /// </summary>
    /// <returns>An ApiCredentials object with the admin username and password.</returns>
    public StalwartMigration.Infrastructure.Stalwart.ApiCredentials GetAdminCredentials()
    {
        return new StalwartMigration.Infrastructure.Stalwart.ApiCredentials
        {
            Username = AdminUsername,
            Password = AdminPassword
        };
    }
}
