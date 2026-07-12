// <copyright file="DockerHelperTests.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using Xunit;

namespace StalwartMigration.Integration.Tests.Fixtures;

// Note: DockerHelper is in the Infrastructure namespace
using StalwartMigration.Integration.Tests.Infrastructure;

/// <summary>
/// Tests for the DockerHelper class.
/// These tests verify the Docker infrastructure functionality.
/// Note: These tests require Docker to be installed and running.
/// </summary>
public class DockerHelperTests : IAsyncLifetime
{
    private readonly DockerHelper _dockerHelper;
    
    /// <summary>
    /// Initializes a new instance of the DockerHelperTests class.
    /// </summary>
    public DockerHelperTests()
    {
        // Use a fixed port for testing
        _dockerHelper = new DockerHelper(
            containerName: "stalwart-test-dockerhelper",
            hostPort: 0, // Random port
            adminPassword: "testpassword123"
        );
    }

    /// <summary>
    /// IAsyncLifetime initialization.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        // Start the container
        await _dockerHelper.StartContainerAsync();
        
        // Wait for health check
        await _dockerHelper.WaitForHealthyAsync(timeoutSeconds: 120);
    }

    /// <summary>
    /// IAsyncLifetime disposal.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DisposeAsync()
    {
        await _dockerHelper.CleanupAsync();
        _dockerHelper.Dispose();
    }

    /// <summary>
    /// Tests that the DockerHelper can be created.
    /// </summary>
    [Fact]
    public void DockerHelper_Create_Succeeds()
    {
        // Act
        var helper = new DockerHelper();
        
        // Assert
        Assert.NotNull(helper);
        Assert.False(helper.IsRunning);
        Assert.Null(helper.ContainerId);
    }

    /// <summary>
    /// Tests that the ApiUrl is correctly formatted.
    /// </summary>
    [Fact]
    public void ApiUrl_IsCorrectlyFormatted()
    {
        // Arrange
        var expectedPort = _dockerHelper.ApiUrl.Split(':').Last();
        
        // Act & Assert
        Assert.StartsWith("http://localhost:", _dockerHelper.ApiUrl);
        Assert.True(int.TryParse(expectedPort, out _));
    }

    /// <summary>
    /// Tests that admin credentials are available.
    /// </summary>
    [Fact]
    public void GetAdminCredentials_ReturnsValidCredentials()
    {
        // Act
        var credentials = _dockerHelper.GetAdminCredentials();
        
        // Assert
        Assert.NotNull(credentials);
        Assert.Equal("admin", credentials.Username);
        Assert.NotNull(credentials.Password);
        Assert.Equal("testpassword123", credentials.Password);
    }

    /// <summary>
    /// Tests that the container is running after start.
    /// </summary>
    [Fact]
    public void Container_IsRunningAfterStart()
    {
        // Assert
        Assert.True(_dockerHelper.IsRunning);
        Assert.NotNull(_dockerHelper.ContainerId);
    }
}

/// <summary>
/// Tests for the StalwartTestFixture class.
/// </summary>
public class StalwartTestFixtureTests : IClassFixture<StalwartTestFixture>, IAsyncLifetime
{
    private readonly StalwartTestFixture _fixture;
    
    /// <summary>
    /// Initializes a new instance of the StalwartTestFixtureTests class.
    /// </summary>
    /// <param name="fixture">The shared fixture.</param>
    public StalwartTestFixtureTests(StalwartTestFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// IAsyncLifetime initialization.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
    }

    /// <summary>
    /// IAsyncLifetime disposal.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task DisposeAsync()
    {
        return Task.CompletedTask; // Fixture handles its own cleanup
    }

    /// <summary>
    /// Tests that the fixture initializes correctly.
    /// </summary>
    [Fact]
    public void Fixture_Initialize_Succeeds()
    {
        // Assert
        Assert.True(_fixture.IsInitialized);
        Assert.NotNull(_fixture.ApiUrl);
        Assert.NotNull(_fixture.Credentials);
        Assert.NotNull(_fixture.StalwartClient);
    }

    /// <summary>
    /// Tests that the fixture client is authenticated.
    /// </summary>
    [Fact]
    public void FixtureClient_IsAuthenticated()
    {
        // Assert
        Assert.True(_fixture.StalwartClient.IsAuthenticated);
    }

    /// <summary>
    /// Tests that the fixture can create new clients.
    /// </summary>
    [Fact]
    public void Fixture_CreateClient_Succeeds()
    {
        // Act
        var client = _fixture.CreateClient();
        
        // Assert
        Assert.NotNull(client);
        Assert.Equal(_fixture.ApiUrl, client.BaseUrl);
    }
}
