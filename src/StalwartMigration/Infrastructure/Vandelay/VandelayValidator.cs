// <copyright file="VandelayValidator.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace StalwartMigration.Infrastructure.Vandelay;

/// <summary>
/// Validates Vandelay installation and configuration.
/// </summary>
public class VandelayValidator
{
    private readonly ILogger<VandelayValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the VandelayValidator class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public VandelayValidator(ILogger<VandelayValidator>? logger = null)
    {
        _logger = logger ?? NullLogger<VandelayValidator>.Instance;
    }

    /// <summary>
    /// Validates that Vandelay is installed and accessible.
    /// </summary>
    /// <param name="executablePath">Optional path to the Vandelay executable. If not provided, searches for Vandelay.</param>
    /// <returns>A validation result containing the resolved path and validation status.</returns>
    public async Task<VandelayValidationResult> ValidateAsync(string? executablePath = null, CancellationToken cancellationToken = default)
    {
        var result = new VandelayValidationResult();

        // If path provided, use it
        if (!string.IsNullOrWhiteSpace(executablePath))
        {
            result.ExecutablePath = executablePath;
            result.ValidationPath = ValidationPath.ExplicitPath;
        }
        else
        {
            // Search for Vandelay
            result.ExecutablePath = await SearchForVandelayAsync().ConfigureAwait(false);
            result.ValidationPath = ValidationPath.Search;
        }

        // If still no path, try PATH environment variable
        if (string.IsNullOrWhiteSpace(result.ExecutablePath))
        {
            result.ExecutablePath = FindInPath("vandelay");
            if (string.IsNullOrWhiteSpace(result.ExecutablePath))
            {
                result.ExecutablePath = FindInPath("vandelay.exe");
            }
            result.ValidationPath = ValidationPath.PathEnvironment;
        }

        // Validate the found path
        if (!string.IsNullOrWhiteSpace(result.ExecutablePath))
        {
            result.Exists = File.Exists(result.ExecutablePath);

            if (result.Exists)
            {
                // Check if it's executable
                result.IsExecutable = await IsExecutableAsync(result.ExecutablePath).ConfigureAwait(false);

                if (result.IsExecutable)
                {
                    // Try to get version to verify it works
                    result.Version = await GetVandelayVersionAsync(result.ExecutablePath, cancellationToken).ConfigureAwait(false);
                    result.IsValid = !string.IsNullOrWhiteSpace(result.Version);

                    if (result.IsValid)
                    {
                        _logger.LogInformation("Vandelay found at: {Path} (Version: {Version})", result.ExecutablePath, result.Version);
                    }
                    else
                    {
                        _logger.LogWarning("Vandelay found at {Path} but version could not be determined", result.ExecutablePath);
                    }
                }
                else
                {
                    _logger.LogWarning("Vandelay found at {Path} but is not executable", result.ExecutablePath);
                }
            }
            else
            {
                _logger.LogWarning("Vandelay path {Path} does not exist", result.ExecutablePath);
            }
        }
        else
        {
            _logger.LogError("Vandelay executable not found");
            result.IsValid = false;
            result.ErrorMessage = "Vandelay executable not found in any standard location.";
        }

        return result;
    }

    /// <summary>
    /// Searches for Vandelay in common installation directories.
    /// </summary>
    /// <returns>The path to the Vandelay executable if found; otherwise, null.</returns>
    private async Task<string?> SearchForVandelayAsync()
    {
        foreach (var directory in VandelayConfig.DefaultInstallationDirectories)
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    foreach (var executableName in VandelayConfig.DefaultExecutableNames)
                    {
                        var path = Path.Combine(directory, executableName);
                        if (File.Exists(path))
                        {
                            _logger.LogDebug("Found potential Vandelay executable at: {Path}", path);
                            return path;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error checking directory {Directory} for Vandelay", directory);
                // Continue searching
            }
        }

        return null;
    }

    /// <summary>
    /// Finds an executable in the system PATH.
    /// </summary>
    /// <param name="executableName">The name of the executable to find.</param>
    /// <returns>The full path if found; otherwise, null.</returns>
    private string? FindInPath(string executableName)
    {
        try
        {
            // Try to find the executable in PATH
            var pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrWhiteSpace(pathEnv))
                return null;

            foreach (var path in pathEnv.Split(Path.PathSeparator))
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                    {
                        var fullPath = Path.Combine(path, executableName);
                        if (File.Exists(fullPath))
                        {
                            _logger.LogDebug("Found {Executable} in PATH at: {Path}", executableName, fullPath);
                            return fullPath;
                        }
                    }
                }
                catch
                {
                    // Continue to next path
                }
            }
        }
        catch
        {
            // PATH lookup failed
        }

        return null;
    }

    /// <summary>
    /// Checks if a file is executable.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the file is executable; otherwise, false.</returns>
    private async Task<bool> IsExecutableAsync(string path)
    {
        try
        {
            // On Unix-like systems, check file permissions
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var fileInfo = new FileInfo(path);
                // Check if user has execute permission
                return fileInfo.Exists && (fileInfo.UnixFileMode & UnixFileMode.UserExecute) != 0;
            }
            // On Windows, if it exists and has .exe extension, assume it's executable
            return File.Exists(path) && 
                   (path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".bat", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the Vandelay version by running the version command.
    /// </summary>
    /// <param name="executablePath">The path to the Vandelay executable.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The Vandelay version string; otherwise, null.</returns>
    public async Task<string?> GetVandelayVersionAsync(string executablePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = processInfo };
            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                // Parse version from output (first line typically contains version)
                var lines = output.Split('\n');
                if (lines.Length > 0 && !string.IsNullOrWhiteSpace(lines[0]))
                {
                    // Remove any command name prefix (e.g., "vandelay 1.2.3" -> "1.2.3")
                    var versionLine = lines[0].Trim();
                    if (versionLine.StartsWith("vandelay", StringComparison.OrdinalIgnoreCase))
                    {
                        // Extract version number
                        var parts = versionLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 1)
                        {
                            return parts[1];
                        }
                    }
                    return versionLine;
                }
            }

            _logger.LogDebug("Vandelay version check output: {Output}, error: {Error}, exit code: {ExitCode}",
                output, error, process.ExitCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get Vandelay version from {Path}", executablePath);
        }

        return null;
    }

    /// <summary>
    /// Validates Vandelay configuration.
    /// </summary>
    /// <param name="config">The configuration to validate.</param>
    /// <returns>True if the configuration is valid; otherwise, false.</returns>
    public async Task<bool> ValidateConfigAsync(VandelayConfig config, CancellationToken cancellationToken = default)
    {
        if (config == null)
        {
            _logger.LogError("Vandelay configuration is null");
            return false;
        }

        // Check if executable path is valid
        if (!config.IsValid())
        {
            _logger.LogError("Vandelay configuration is invalid: executable path is missing");
            return false;
        }

        // Validate the executable path
        var validationResult = await ValidateAsync(config.ExecutablePath, cancellationToken).ConfigureAwait(false);

        if (!validationResult.IsValid)
        {
            _logger.LogError("Vandelay configuration validation failed: {ErrorMessage}", validationResult.ErrorMessage);
            return false;
        }

        // Additional configuration validation
        if (config.Source != null)
        {
            if (string.IsNullOrWhiteSpace(config.Source.Host))
            {
                _logger.LogError("Source IMAP host is required");
                return false;
            }

            if (config.Source.Port <= 0 || config.Source.Port > 65535)
            {
                _logger.LogError("Source IMAP port must be between 1 and 65535");
                return false;
            }
        }

        if (config.Destination != null)
        {
            if (string.IsNullOrWhiteSpace(config.Destination.Url))
            {
                _logger.LogError("Destination JMAP URL is required");
                return false;
            }
        }

        if (config.TimeoutSeconds <= 0)
        {
            _logger.LogError("Timeout must be positive");
            return false;
        }

        _logger.LogInformation("Vandelay configuration is valid");
        return true;
    }

    /// <summary>
    /// Checks if the required Vandelay version is installed.
    /// </summary>
    /// <param name="executablePath">The path to the Vandelay executable.</param>
    /// <param name="requiredVersion">The required version (e.g., "1.0.0").</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the required version is installed; otherwise, false.</returns>
    public async Task<bool> CheckVersionAsync(string executablePath, string requiredVersion, CancellationToken cancellationToken = default)
    {
        var currentVersion = await GetVandelayVersionAsync(executablePath, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(currentVersion))
        {
            _logger.LogWarning("Could not determine Vandelay version");
            return false;
        }

        // Simple version comparison (could be enhanced with proper version parsing)
        if (currentVersion.StartsWith(requiredVersion, StringComparison.Ordinal))
        {
            _logger.LogInformation("Vandelay version {CurrentVersion} meets requirement {RequiredVersion}",
                currentVersion, requiredVersion);
            return true;
        }

        _logger.LogWarning("Vandelay version {CurrentVersion} does not meet requirement {RequiredVersion}",
            currentVersion, requiredVersion);
        return false;
    }
}

/// <summary>
/// Represents the result of Vandelay validation.
/// </summary>
public class VandelayValidationResult
{
    /// <summary>
    /// Gets or sets the path to the Vandelay executable.
    /// </summary>
    public string? ExecutablePath { get; set; }

    /// <summary>
    /// Gets or sets whether the executable exists.
    /// </summary>
    public bool Exists { get; set; }

    /// <summary>
    /// Gets or sets whether the executable is executable.
    /// </summary>
    public bool IsExecutable { get; set; }

    /// <summary>
    /// Gets or sets whether the validation was successful.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the Vandelay version if determined.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the error message if validation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets how the path was determined.
    /// </summary>
    public ValidationPath ValidationPath { get; set; }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    public override string ToString()
    {
        if (IsValid)
        {
            return $"Vandelay validation PASSED - Version: {Version}, Path: {ExecutablePath}, Method: {ValidationPath}";
        }
        else
        {
            return $"Vandelay validation FAILED - {ErrorMessage}";
        }
    }
}

/// <summary>
/// Enumeration of validation path methods.
/// </summary>
public enum ValidationPath
{
    /// <summary>
    /// Explicit path was provided.
    /// </summary>
    ExplicitPath,

    /// <summary>
    /// Path was found by searching known directories.
    /// </summary>
    Search,

    /// <summary>
    /// Path was found in PATH environment variable.
    /// </summary>
    PathEnvironment
}
