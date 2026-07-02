// <copyright file="VandelayConfig.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

namespace StalwartMigration.Infrastructure.Vandelay;

/// <summary>
/// Configuration for running Vandelay operations.
/// </summary>
public class VandelayConfig
{
    /// <summary>
    /// Gets or sets the path to the Vandelay executable.
    /// </summary>
    [JsonPropertyName("executablePath")]
    public string? ExecutablePath { get; set; }

    /// <summary>
    /// Gets or sets the working directory for Vandelay operations.
    /// </summary>
    [JsonPropertyName("workingDirectory")]
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Gets or sets the timeout for Vandelay operations in seconds.
    /// Default is 300 seconds (5 minutes).
    /// </summary>
    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets whether to use verbose output.
    /// </summary>
    [JsonPropertyName("verbose")]
    public bool Verbose { get; set; }

    /// <summary>
    /// Gets or sets whether to continue on error.
    /// </summary>
    [JsonPropertyName("continueOnError")]
    public bool ContinueOnError { get; set; }

    /// <summary>
    /// Gets or sets the log level for Vandelay.
    /// </summary>
    [JsonPropertyName("logLevel")]
    public VandelayLogLevel LogLevel { get; set; } = VandelayLogLevel.Info;

    /// <summary>
    /// Gets or sets the source IMAP server configuration.
    /// </summary>
    [JsonPropertyName("source")]
    public ImapConfig? Source { get; set; }

    /// <summary>
    /// Gets or sets the destination JMAP server configuration.
    /// </summary>
    [JsonPropertyName("destination")]
    public JmapConfig? Destination { get; set; }

    /// <summary>
    /// Gets or sets the authentication configuration.
    /// </summary>
    [JsonPropertyName("authentication")]
    public VandelayAuthConfig? Authentication { get; set; }

    /// <summary>
    /// Gets or sets the SSL/TLS configuration.
    /// </summary>
    [JsonPropertyName("ssl")]
    public SslConfig? Ssl { get; set; }

    /// <summary>
    /// Gets or sets the proxy configuration.
    /// </summary>
    [JsonPropertyName("proxy")]
    public ProxyConfig? Proxy { get; set; }

    /// <summary>
    /// Gets or sets whether to skip SSL certificate validation.
    /// </summary>
    [JsonPropertyName("skipSslValidation")]
    public bool SkipSslValidation { get; set; }

    /// <summary>
    /// Initializes a new instance of the VandelayConfig class.
    /// </summary>
    public VandelayConfig()
    { }

    /// <summary>
    /// Initializes a new instance of the VandelayConfig class with the specified executable path.
    /// </summary>
    /// <param name="executablePath">The path to the Vandelay executable.</param>
    public VandelayConfig(string executablePath)
    {
        ExecutablePath = executablePath;
    }

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    /// <returns>True if the configuration is valid; otherwise, false.</returns>
    public bool IsValid()
    {
        // Executable path is the only required field
        return !string.IsNullOrWhiteSpace(ExecutablePath);
    }

    /// <summary>
    /// Gets the default Vandelay executable names to search for.
    /// </summary>
    public static IEnumerable<string> DefaultExecutableNames => new[]
    {
        "vandelay",
        "vandelay.exe",
        "vandelay-indie",
        "vandelay-indie.exe"
    };

    /// <summary>
    /// Gets the default installation directories to search for Vandelay.
    /// </summary>
    public static IEnumerable<string> DefaultInstallationDirectories => new[]
    {
        "/usr/local/bin",
        "/usr/bin",
        "/opt/vandelay",
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\Vandelay",
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "\\Vandelay",
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\.local\\bin",
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\AppData\\Local\\Programs\\Vandelay"
    };
}

/// <summary>
/// IMAP server configuration for Vandelay.
/// </summary>
public class ImapConfig
{
    /// <summary>
    /// Gets or sets the IMAP server host.
    /// </summary>
    [JsonPropertyName("host")]
    public string? Host { get; set; }

    /// <summary>
    /// Gets or sets the IMAP server port.
    /// </summary>
    [JsonPropertyName("port")]
    public int Port { get; set; } = 143;

    /// <summary>
    /// Gets or sets whether to use SSL.
    /// </summary>
    [JsonPropertyName("useSsl")]
    public bool UseSsl { get; set; } = true;

    /// <summary>
    /// Gets or sets the username for authentication.
    /// </summary>
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the password for authentication.
    /// </summary>
    [JsonPropertyName("password")]
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets whether to use OAuth2 authentication.
    /// </summary>
    [JsonPropertyName("useOAuth2")]
    public bool UseOAuth2 { get; set; }

    /// <summary>
    /// Gets or sets the OAuth2 token URL.
    /// </summary>
    [JsonPropertyName("oauth2TokenUrl")]
    public string? OAuth2TokenUrl { get; set; }

    /// <summary>
    /// Gets or sets the OAuth2 client ID.
    /// </summary>
    [JsonPropertyName("oauth2ClientId")]
    public string? OAuth2ClientId { get; set; }

    /// <summary>
    /// Gets or sets the OAuth2 client secret.
    /// </summary>
    [JsonPropertyName("oauth2ClientSecret")]
    public string? OAuth2ClientSecret { get; set; }
}

/// <summary>
/// JMAP server configuration for Vandelay.
/// </summary>
public class JmapConfig
{
    /// <summary>
    /// Gets or sets the JMAP server URL.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the JMAP API path.
    /// </summary>
    [JsonPropertyName("apiPath")]
    public string? ApiPath { get; set; } = "/api";

    /// <summary>
    /// Gets or sets the username for authentication.
    /// </summary>
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the password for authentication.
    /// </summary>
    [JsonPropertyName("password")]
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets whether to use API key authentication.
    /// </summary>
    [JsonPropertyName("useApiKey")]
    public bool UseApiKey { get; set; }

    /// <summary>
    /// Gets or sets the API key for authentication.
    /// </summary>
    [JsonPropertyName("apiKey")]
    public string? ApiKey { get; set; }
}

/// <summary>
/// Authentication configuration for Vandelay.
/// </summary>
public class VandelayAuthConfig
{
    /// <summary>
    /// Gets or sets the authentication method.
    /// </summary>
    [JsonPropertyName("method")]
    public string? Method { get; set; } = "password";

    /// <summary>
    /// Gets or sets the authentication token.
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }
}

/// <summary>
/// SSL/TLS configuration for Vandelay.
/// </summary>
public class SslConfig
{
    /// <summary>
    /// Gets or sets whether to use SSL.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the SSL certificate path.
    /// </summary>
    [JsonPropertyName("certificatePath")]
    public string? CertificatePath { get; set; }

    /// <summary>
    /// Gets or sets the SSL certificate password.
    /// </summary>
    [JsonPropertyName("certificatePassword")]
    public string? CertificatePassword { get; set; }
}

/// <summary>
/// Proxy configuration for Vandelay.
/// </summary>
public class ProxyConfig
{
    /// <summary>
    /// Gets or sets the proxy type (http, https, socks5).
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the proxy host.
    /// </summary>
    [JsonPropertyName("host")]
    public string? Host { get; set; }

    /// <summary>
    /// Gets or sets the proxy port.
    /// </summary>
    [JsonPropertyName("port")]
    public int Port { get; set; } = 8080;

    /// <summary>
    /// Gets or sets the proxy username.
    /// </summary>
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the proxy password.
    /// </summary>
    [JsonPropertyName("password")]
    public string? Password { get; set; }
}

/// <summary>
/// Log level for Vandelay operations.
/// </summary>
public enum VandelayLogLevel
{
    /// <summary>
    /// No logging.
    /// </summary>
    None,

    /// <summary>
    /// Error level logging.
    /// </summary>
    Error,

    /// <summary>
    /// Warning level logging.
    /// </summary>
    Warning,

    /// <summary>
    /// Information level logging.
    /// </summary>
    Info,

    /// <summary>
    /// Debug level logging.
    /// </summary>
    Debug,

    /// <summary>
    /// Trace level logging.
    /// </summary>
    Trace
}
