// <copyright file="StalwartTestFixture.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.Diagnostics;
using StalwartMigration.Infrastructure.Stalwart;
using StalwartMigration.Integration.Tests.Infrastructure;
using Xunit;

namespace StalwartMigration.Integration.Tests.Fixtures;

/// <summary>
/// xUnit fixture class for Stalwart integration tests.
/// Provides a shared or isolated Stalwart container instance for testing.
/// Implements IAsyncLifetime for async initialization and cleanup.
/// </summary>
public class StalwartTestFixture : IAsyncLifetime, IDisposable
{
    private readonly DockerHelper _dockerHelper;
    private readonly bool _shared;
    private StalwartClient? _stalwartClient;
    private ApiCredentials? _credentials;
    private bool _disposed;
    
    /// <summary>
    /// Gets the Stalwart API URL.
    /// </summary>
    public string ApiUrl => _dockerHelper.ApiUrl;
    
    /// <summary>
    /// Gets the StalwartClient instance.
    /// </summary>
    public StalwartClient StalwartClient => _stalwartClient ?? throw new InvalidOperationException("Fixture not initialized. Call InitializeAsync first.");
    
    /// <summary>
    /// Gets the admin credentials.
    /// </summary>
    public ApiCredentials Credentials => _credentials ?? throw new InvalidOperationException("Fixture not initialized. Call InitializeAsync first.");
    
    /// <summary>
    /// Gets the DockerHelper instance for advanced container management.
    /// </summary>
    public DockerHelper DockerHelper => _dockerHelper;
    
    /// <summary>
    /// Initializes a new instance of the StalwartTestFixture class.
    /// </summary>
    public StalwartTestFixture()
    {
        _shared = true;
        
        // Read admin password from STALWART_RECOVERY_ADMIN environment variable
        // Format: username:password (e.g., admin:mySecretPass)
        string? adminPassword = null;
        var recoveryAdminEnv = Environment.GetEnvironmentVariable("STALWART_RECOVERY_ADMIN");
        if (!string.IsNullOrEmpty(recoveryAdminEnv))
        {
            var parts = recoveryAdminEnv.Split(':', 2);
            if (parts.Length == 2)
            {
                adminPassword = parts[1];
            }
        }
        
        // Unique name so a leaked container from an aborted run can never
        // block the next one with a name conflict.
        _dockerHelper = new DockerHelper(
            containerName: $"stalwart-test-shared-{Guid.NewGuid():N}",
            hostPort: 0,
            adminPassword: adminPassword
        );
        _disposed = false;
    }

    /// <summary>
    /// Initializes the fixture asynchronously.
    /// Starts the Stalwart container and creates a client.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        try
        {
            // Start the container
            await _dockerHelper.StartContainerAsync();

            // Wait for health check
            await _dockerHelper.WaitForHealthyAsync(timeoutSeconds: 120);

            // Get credentials
            _credentials = _dockerHelper.GetAdminCredentials();

            // Create the client
            var options = new StalwartClientOptions
            {
                Timeout = TimeSpan.FromSeconds(30),
                MaxRetries = 3
            };

            _stalwartClient = new StalwartClient(_dockerHelper.ApiUrl, _credentials, options);

            // Authenticate
            await _stalwartClient.AuthenticateAsync(_credentials);
        }
        catch
        {
            // xUnit does not call DisposeAsync when InitializeAsync throws,
            // so tear down here or the container leaks across runs.
            _stalwartClient?.Dispose();
            _stalwartClient = null;
            await _dockerHelper.CleanupAsync();
            throw;
        }
    }

    /// <summary>
    /// IAsyncLifetime initialization.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task IAsyncLifetime.InitializeAsync()
    {
        return InitializeAsync();
    }

    /// <summary>
    /// Cleans up the fixture asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DisposeAsync()
    {
        if (_disposed)
            return;

        try
        {
            // Dispose the client
            _stalwartClient?.Dispose();
            _stalwartClient = null;

            // Clean up Docker resources
            await _dockerHelper.CleanupAsync();
        }
        finally
        {
            _disposed = true;
        }
    }

    /// <summary>
    /// IAsyncLifetime disposal.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task IAsyncLifetime.DisposeAsync()
    {
        return DisposeAsync();
    }

    /// <summary>
    /// IDisposable disposal.
    /// </summary>
    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets a value indicating whether the fixture is initialized.
    /// </summary>
    public bool IsInitialized => _credentials != null && _stalwartClient != null;

    /// <summary>
    /// Creates a new StalwartClient for the current fixture.
    /// Useful when you need a fresh client instance.
    /// </summary>
    /// <returns>A new StalwartClient instance.</returns>
    public StalwartClient CreateClient()
    {
        if (!IsInitialized)
            throw new InvalidOperationException("Fixture not initialized.");
        
        var options = new StalwartClientOptions
        {
            Timeout = TimeSpan.FromSeconds(30),
            MaxRetries = 3
        };
        
        return new StalwartClient(ApiUrl, Credentials, options);
    }

    /// <summary>
    /// Creates a new StalwartClient with custom credentials.
    /// Useful for testing with different user accounts.
    /// </summary>
    /// <param name="credentials">The credentials to use.</param>
    /// <returns>A new StalwartClient instance.</returns>
    public StalwartClient CreateClient(ApiCredentials credentials)
    {
        if (!IsInitialized)
            throw new InvalidOperationException("Fixture not initialized.");
        
        var options = new StalwartClientOptions
        {
            Timeout = TimeSpan.FromSeconds(30),
            MaxRetries = 3
        };
        
        return new StalwartClient(ApiUrl, credentials, options);
    }
}

/// <summary>
/// Attribute to mark test classes that should use a shared Stalwart fixture.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class SharedStalwartFixtureAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the port to use for the shared fixture.
    /// </summary>
    public int Port { get; set; } = 8080;
}

/// <summary>
/// Attribute to mark test classes that should use an isolated Stalwart fixture.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class IsolatedStalwartFixtureAttribute : Attribute
{
    /// <summary>
    /// Gets or sets whether to use a random port. Default is true.
    /// </summary>
    public bool RandomPort { get; set; } = true;
}
