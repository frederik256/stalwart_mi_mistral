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
    private string _adminPassword;
    private string? _containerId;
    private bool _disposed;
    
    /// <summary>
    /// Gets the URL where the Stalwart API is accessible.
    /// </summary>
    public string ApiUrl => $"http://localhost:{_hostPort}";

    // Volumes are per-container so parallel helpers never share config or
    // RocksDB data, and cleanup of one instance cannot break another.
    private string EtcVolumeName => $"{_containerName}-etc";
    private string DataVolumeName => $"{_containerName}-data";

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
        await CreateVolumeIfNotExistsAsync(EtcVolumeName, cancellationToken);
        await CreateVolumeIfNotExistsAsync(DataVolumeName, cancellationToken);

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
                        $"{EtcVolumeName}:/etc/stalwart",
                        $"{DataVolumeName}:/var/lib/stalwart"
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
    /// Retrieves the admin credentials from container logs or environment variables.
    /// Stalwart outputs bootstrap credentials when started in recovery mode.
    /// Priority: logs > env var > constructor password
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task RetrieveCredentialsFromLogsAsync(CancellationToken cancellationToken)
    {
        if (_containerId == null)
            return;

        // Try to extract the password from container logs first
        string? password = null;
        if (string.IsNullOrEmpty(_adminPassword))
        {
            await Task.Delay(3000, cancellationToken);

            // Get container logs to extract the actual password
            var logs = await _dockerClient.Containers.GetContainerLogsAsync(
                _containerId,
                true, // TTY-enabled for proper log demultiplexing
                new ContainerLogsParameters
                {
                    ShowStdout = true,
                    ShowStderr = true,
                    Follow = false,
                    Timestamps = false
                },
                cancellationToken
            );

            // Parse the logs to extract the password from the bootstrap banner
            // The password appears after "password: " in the bootstrap mode output
            // Read all output (stdout and stderr) from the multiplexed stream
            var (stdout, stderr) = await logs.ReadOutputToEndAsync(cancellationToken);
            
            // Search both output streams for the password
            foreach (var text in new[] { stdout, stderr })
            {
                var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var passwordIndex = line.IndexOf("password:", StringComparison.OrdinalIgnoreCase);
                    if (passwordIndex >= 0)
                    {
                        var start = passwordIndex + "password:".Length;
                        while (start < line.Length && char.IsWhiteSpace(line[start]))
                            start++;
                        
                        if (start < line.Length)
                        {
                            password = line.Substring(start).Trim();
                            break;
                        }
                    }
                }
                if (password != null)
                    break;
            }
        }

        // Try to extract the password from container environment variables
        // This is the most reliable method as it reads the actual env var set in the container
        if (string.IsNullOrEmpty(password) && _containerId != null)
        {
            // Retry a few times in case the container is not ready yet
            int retryCount = 0;
            while (retryCount < 5)
            {
                try
                {
                    var inspect = _dockerClient.Containers.InspectContainerAsync(_containerId).GetAwaiter().GetResult();
                    if (inspect.Config?.Env != null)
                    {
                        foreach (var env in inspect.Config.Env)
                        {
                            if (env.StartsWith("STALWART_RECOVERY_ADMIN=") && env.Length > 22)
                            {
                                // The env var format is "STALWART_RECOVERY_ADMIN=username:password"
                                // We need to extract just the password part
                                var fullValue = env.Substring(22);
                                var parts = fullValue.Split(':', 2);
                                if (parts.Length == 2)
                                {
                                    password = parts[1];
                                }
                                else
                                {
                                    password = fullValue;
                                }
                                break;
                            }
                        }
                    }
                    // If we got here, the inspect succeeded - break out of retry loop
                    break;
                }
                catch
                {
                    // Inspect failed, retry after a delay
                    await Task.Delay(500);
                    retryCount++;
                }
            }
        }

        // If we found a password from logs or env vars, use it
        if (password != null)
        {
            _adminPassword = password;
        }
        // Otherwise, fall back to the constructor password (may be random)
    }

    /// <summary>
    /// Waits for the Stalwart API to be healthy.
    /// In bootstrap/recovery mode, Stalwart serves the WebUI at /admin.
    /// Once configured, it serves the management API which we check for health.
    /// We specifically check /api/auth POST to ensure the management API is ready.
    /// </summary>
    /// <param name="timeoutSeconds">Maximum time to wait in seconds. Default is 120.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="TimeoutException">Thrown if the API does not become healthy within the timeout.</exception>
    public async Task WaitForHealthyAsync(int timeoutSeconds = 120, CancellationToken cancellationToken = default)
    {
        var client = new HttpClient();
        var startTime = DateTime.UtcNow;
        
        // Try multiple possible endpoints - Stalwart v0.16 uses /admin in bootstrap mode
        var healthEndpoints = new[]
        {
            "/admin",           // Bootstrap mode WebUI
            "/api/health",      // Some versions
            "/",               // Root redirect
        };
        
        // Phase 1: Wait for basic endpoints to be available (quick check)
        while ((DateTime.UtcNow - startTime).TotalSeconds < 30)
        {
            try
            {
                foreach (var endpoint in healthEndpoints)
                {
                    var response = await client.GetAsync($"{ApiUrl}{endpoint}", cancellationToken);
                    if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Redirect)
                    {
                        goto Phase2;
                    }
                }
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
        
        throw new TimeoutException($"Stalwart container at {ApiUrl} did not become reachable within 30 seconds.");
        
    Phase2:
        // Phase 2: Wait for the management API to accept a real login.
        // An empty JSON body always gets 400 ("JSON deserialization failed"),
        // so probe with the actual admin credentials using the same authCode
        // flow that StalwartClient.AuthenticateAsync uses.
        var authUrl = $"{ApiUrl}/api/auth";
        var authBody = System.Text.Json.JsonSerializer.Serialize(new
        {
            type = "authCode",
            accountName = DefaultUsername,
            accountSecret = _adminPassword,
            clientId = "stalwart-webui",
            redirectUri = $"{ApiUrl}/admin/oauth/callback"
        });
        while ((DateTime.UtcNow - startTime).TotalSeconds < timeoutSeconds)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, authUrl);
                request.Content = new StringContent(authBody, System.Text.Encoding.UTF8, "application/json");
                var response = await client.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
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
        await RemoveVolumeIfExistsAsync(EtcVolumeName, cancellationToken);
        await RemoveVolumeIfExistsAsync(DataVolumeName, cancellationToken);
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
            await _dockerClient.Volumes.RemoveAsync(volumeName, force: true, cancellationToken: cancellationToken);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound || ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            // NotFound: volume doesn't exist. Conflict: still in use by another
            // container; cleanup must not fail the test run over it.
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
        // Priority 1: Use password set via constructor (from STALWART_RECOVERY_ADMIN env on host)
        // Priority 2: Extract from container logs (bootstrap mode with random password)
        // Priority 3: Read from container's own STALWART_RECOVERY_ADMIN env var
        var password = _adminPassword;
        if (string.IsNullOrEmpty(password) && _containerId != null)
        {
            // Try to get the password from the container's environment variables
            try
            {
                var inspect = _dockerClient.Containers.InspectContainerAsync(_containerId).GetAwaiter().GetResult();
                if (inspect.Config?.Env != null)
                {
                    foreach (var env in inspect.Config.Env)
                    {
                        if (env.StartsWith("STALWART_RECOVERY_ADMIN=") && env.Length > 22)
                        {
                            password = env.Substring(22);
                            break;
                        }
                    }
                }
            }
            catch
            {
                // Inspect failed, continue with other methods
            }
        }

        return new StalwartMigration.Infrastructure.Stalwart.ApiCredentials
        {
            Username = AdminUsername,
            Password = password ?? string.Empty
        };
    }
}
