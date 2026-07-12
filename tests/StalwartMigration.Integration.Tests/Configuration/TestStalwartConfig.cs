// <copyright file="TestStalwartConfig.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using Microsoft.Extensions.Configuration;
using System.IO;

namespace StalwartMigration.Integration.Tests.Configuration;

/// <summary>
/// Configuration model for Stalwart integration tests.
/// Loads settings from appsettings.test.json and environment variables.
/// </summary>
public class TestStalwartConfig
{
    /// <summary>
    /// Gets the Stalwart API configuration.
    /// </summary>
    public StalwartApiConfig Stalwart { get; set; } = new();
    
    /// <summary>
    /// Gets the Docker configuration.
    /// </summary>
    public DockerConfig Docker { get; set; } = new();
    
    /// <summary>
    /// Gets the test credentials configuration.
    /// </summary>
    public TestCredentialsConfig TestCredentials { get; set; } = new();
    
    /// <summary>
    /// Gets the test data configuration.
    /// </summary>
    public TestDataConfig TestData { get; set; } = new();
    
    /// <summary>
    /// Gets the logging configuration.
    /// </summary>
    public LoggingConfig Logging { get; set; } = new();
    
    /// <summary>
    /// Gets the timeout configuration.
    /// </summary>
    public TimeoutConfig Timeouts { get; set; } = new();
}

/// <summary>
/// Stalwart API configuration.
/// </summary>
public class StalwartApiConfig
{
    /// <summary>
    /// Gets or sets the API base URL.
    /// </summary>
    public string ApiUrl { get; set; } = "http://localhost:8080";
    
    /// <summary>
    /// Gets or sets the timeout in seconds for API requests.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;
    
    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetries { get; set; } = 5;
    
    /// <summary>
    /// Gets or sets a value indicating whether test mode is enabled.
    /// </summary>
    public bool TestMode { get; set; } = true;
}

/// <summary>
/// Docker configuration for test containers.
/// </summary>
public class DockerConfig
{
    /// <summary>
    /// Gets or sets the Docker image to use.
    /// </summary>
    public string Image { get; set; } = "stalwartlabs/stalwart:v0.16";
    
    /// <summary>
    /// Gets or sets the container name.
    /// </summary>
    public string ContainerName { get; set; } = "stalwart-test";
    
    /// <summary>
    /// Gets or sets the host port to map.
    /// </summary>
    public int HostPort { get; set; } = 8080;
    
    /// <summary>
    /// Gets or sets the container port.
    /// </summary>
    public int ContainerPort { get; set; } = 8080;
    
    /// <summary>
    /// Gets or sets the volume bindings.
    /// </summary>
    public VolumeConfig Volumes { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the environment variables.
    /// </summary>
    public Dictionary<string, string> Environment { get; set; } = new();
}

/// <summary>
/// Volume configuration for Docker containers.
/// </summary>
public class VolumeConfig
{
    /// <summary>
    /// Gets or sets the config volume binding.
    /// </summary>
    public string Config { get; set; } = "stalwart-test-etc:/etc/stalwart";
    
    /// <summary>
    /// Gets or sets the data volume binding.
    /// </summary>
    public string Data { get; set; } = "stalwart-test-data:/var/lib/stalwart";
}

/// <summary>
/// Test credentials configuration.
/// </summary>
public class TestCredentialsConfig
{
    /// <summary>
    /// Gets or sets the admin username.
    /// </summary>
    public string AdminUsername { get; set; } = "admin";
    
    /// <summary>
    /// Gets or sets the minimum password length.
    /// </summary>
    public int MinPasswordLength { get; set; } = 16;
    
    /// <summary>
    /// Gets or sets the password complexity requirements.
    /// </summary>
    public PasswordComplexityConfig PasswordComplexity { get; set; } = new();
}

/// <summary>
/// Password complexity requirements.
/// </summary>
public class PasswordComplexityConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether uppercase letters are required.
    /// </summary>
    public bool RequireUppercase { get; set; } = true;
    
    /// <summary>
    /// Gets or sets a value indicating whether lowercase letters are required.
    /// </summary>
    public bool RequireLowercase { get; set; } = true;
    
    /// <summary>
    /// Gets or sets a value indicating whether digits are required.
    /// </summary>
    public bool RequireDigits { get; set; } = true;
    
    /// <summary>
    /// Gets or sets a value indicating whether special characters are required.
    /// </summary>
    public bool RequireSpecialChars { get; set; } = true;
}

/// <summary>
/// Test data configuration.
/// </summary>
public class TestDataConfig
{
    /// <summary>
    /// Gets or sets the default domain name for tests.
    /// </summary>
    public string DefaultDomain { get; set; } = "test.example.com";
    
    /// <summary>
    /// Gets or sets the test account prefix.
    /// </summary>
    public string TestAccountPrefix { get; set; } = "testuser";
    
    /// <summary>
    /// Gets or sets the maximum number of test accounts.
    /// </summary>
    public int MaxTestAccounts { get; set; } = 10;
    
    /// <summary>
    /// Gets or sets a value indicating whether to cleanup test data on teardown.
    /// </summary>
    public bool CleanupOnTeardown { get; set; } = true;
}

/// <summary>
/// Logging configuration.
/// </summary>
public class LoggingConfig
{
    /// <summary>
    /// Gets or sets the log level configuration.
    /// </summary>
    public Dictionary<string, string> LogLevel { get; set; } = new();
    
    /// <summary>
    /// Gets or sets a value indicating whether to capture API requests.
    /// </summary>
    public bool CaptureApiRequests { get; set; } = true;
    
    /// <summary>
    /// Gets or sets a value indicating whether to sanitize sensitive data in logs.
    /// </summary>
    public bool SanitizeSensitiveData { get; set; } = true;
}

/// <summary>
/// Timeout configuration.
/// </summary>
public class TimeoutConfig
{
    /// <summary>
    /// Gets or sets the container startup timeout in seconds.
    /// </summary>
    public int ContainerStartupSeconds { get; set; } = 120;
    
    /// <summary>
    /// Gets or sets the health check timeout in seconds.
    /// </summary>
    public int HealthCheckSeconds { get; set; } = 30;
    
    /// <summary>
    /// Gets or sets the API request timeout in seconds.
    /// </summary>
    public int ApiRequestSeconds { get; set; } = 30;
    
    /// <summary>
    /// Gets or sets the test execution timeout in seconds.
    /// </summary>
    public int TestExecutionSeconds { get; set; } = 300;
}

/// <summary>
/// Configuration loader for test settings.
/// </summary>
public static class TestConfigurationLoader
{
    private static IConfigurationRoot? _configuration;
    
    /// <summary>
    /// Loads the test configuration from appsettings.test.json and environment variables.
    /// </summary>
    /// <returns>An IConfigurationRoot instance.</returns>
    public static IConfigurationRoot LoadConfiguration()
    {
        if (_configuration != null)
            return _configuration;
        
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.test.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables(prefix: "STALWART_TEST_")
            .AddUserSecrets<TestStalwartConfig>(optional: true);
        
        _configuration = builder.Build();
        return _configuration;
    }
    
    /// <summary>
    /// Gets the test Stalwart configuration.
    /// </summary>
    /// <returns>A TestStalwartConfig instance.</returns>
    public static TestStalwartConfig GetConfiguration()
    {
        var config = LoadConfiguration();
        return config.Get<TestStalwartConfig>() ?? new TestStalwartConfig();
    }
    
    /// <summary>
    /// Resets the cached configuration. Useful for testing.
    /// </summary>
    public static void Reset()
    {
        _configuration = null;
    }
}
