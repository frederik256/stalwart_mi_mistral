// <copyright file="VandelayRunner.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace StalwartMigration.Infrastructure.Vandelay;

/// <summary>
/// Executes Vandelay commands as external processes.
/// </summary>
public class VandelayRunner : IDisposable
{
    private readonly VandelayValidator _validator;
    private readonly VandelayResultParser _parser;
    private readonly ILogger<VandelayRunner> _logger;
    private Process? _currentProcess;
    private bool _disposed;

    /// <summary>
    /// Gets or sets the default timeout for Vandelay operations.
    /// </summary>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Initializes a new instance of the VandelayRunner class.
    /// </summary>
    /// <param name="validator">The Vandelay validator.</param>
    /// <param name="parser">The Vandelay result parser.</param>
    /// <param name="logger">The logger instance.</param>
    public VandelayRunner(VandelayValidator? validator = null, VandelayResultParser? parser = null, ILogger<VandelayRunner>? logger = null)
    {
        _validator = validator ?? new VandelayValidator();
        _parser = parser ?? new VandelayResultParser();
        _logger = logger ?? NullLogger<VandelayRunner>.Instance;
    }

    /// <summary>
    /// Runs a Vandelay command.
    /// </summary>
    /// <param name="command">The Vandelay command to run (e.g., "import", "export").</param>
    /// <param name="arguments">The arguments for the command.</param>
    /// <param name="config">The Vandelay configuration.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the Vandelay operation.</returns>
    public async Task<VandelayResult> RunAsync(
        string command,
        IEnumerable<string>? arguments = null,
        VandelayConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        // Build the argument list
        var args = BuildArgumentList(command, arguments, config);

        // Determine the executable path
        var executablePath = config?.ExecutablePath ?? "vandelay";

        // Create the result object
        var result = new VandelayResult
        {
            Command = executablePath,
            Arguments = args.ToList(),
            WorkingDirectory = config?.WorkingDirectory ?? string.Empty
        };

        try
        {
            // Validate Vandelay is available
            if (!await ValidateVandelayAsync(executablePath, config, cancellationToken).ConfigureAwait(false))
            {
                result.Success = false;
                result.ExitCode = -1;
                result.ErrorMessage = "Vandelay validation failed. Check that Vandelay is installed and accessible.";
                return result;
            }

            // Create process start info
            var processInfo = CreateProcessStartInfo(executablePath, args, config);

            // Start the process
            using var process = new Process { StartInfo = processInfo };
            _currentProcess = process;

            _logger.LogInformation("Starting Vandelay: {Command} {Arguments}", processInfo.FileName, processInfo.Arguments);

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            // Set up output and error readers
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                    _logger.LogDebug("Vandelay: {Data}", e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorBuilder.AppendLine(e.Data);
                    _logger.LogError("Vandelay Error: {Data}", e.Data);
                }
            };

            // Start the process
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for completion or timeout
            var timeout = config?.TimeoutSeconds > 0 ? TimeSpan.FromSeconds(config.TimeoutSeconds) : DefaultTimeout;
            
            try
            {
                // Use cancellation token with timeout
                using var cts = new CancellationTokenSource(timeout);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
                
                await process.WaitForExitAsync(linkedCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Vandelay operation was cancelled");
                result.Success = false;
                result.ExitCode = -3;
                result.ErrorMessage = "Operation was cancelled.";
                result.StandardOutput = outputBuilder.ToString();
                result.StandardError = errorBuilder.ToString();
                result.Complete();
                return result;
            }
            catch (OperationCanceledException) // This is from the timeout
            {
                // Timeout occurred
                _logger.LogWarning("Vandelay operation timed out after {Timeout}", timeout);
                result.Success = false;
                result.ExitCode = -2;
                result.ErrorMessage = "Operation timed out.";
                result.StandardOutput = outputBuilder.ToString();
                result.StandardError = errorBuilder.ToString();
                result.Complete();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error waiting for Vandelay process to exit");
                result.Success = false;
                result.ExitCode = -4;
                result.ErrorMessage = ex.Message;
                result.StandardOutput = outputBuilder.ToString();
                result.StandardError = errorBuilder.ToString();
                result.Complete();
                return result;
            }

            // Read any remaining output
            result.StandardOutput = outputBuilder.ToString();
            result.StandardError = errorBuilder.ToString();
            result.ExitCode = process.ExitCode;
            result.Success = process.ExitCode == 0;

            // Parse the result for additional information
            _parser.ParseResult(result);

            _logger.LogInformation("Vandelay completed: {Command} {Arguments} - Exit Code: {ExitCode}",
                processInfo.FileName, processInfo.Arguments, process.ExitCode);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Vandelay operation was cancelled");
            result.Success = false;
            result.ExitCode = -3;
            result.ErrorMessage = "Operation was cancelled.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running Vandelay command: {Command}", command);
            result.Success = false;
            result.ExitCode = -4;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            _currentProcess = null;
            result.Complete();
        }

        return result;
    }

    /// <summary>
    /// Runs Vandelay import command.
    /// </summary>
    /// <param name="config">The Vandelay configuration.</param>
    /// <param name="account">Optional specific account to import.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the import operation.</returns>
    public async Task<VandelayResult> RunImportAsync(VandelayConfig? config = null, string? account = null, CancellationToken cancellationToken = default)
    {
        var args = new List<string> { "import", "imap" };

        if (account != null)
        {
            args.Add("--account");
            args.Add(account);
        }

        // Add configuration-specific arguments
        AddCommonArguments(args, config);

        return await RunAsync("import", args, config, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs Vandelay export command.
    /// </summary>
    /// <param name="config">The Vandelay configuration.</param>
    /// <param name="account">Optional specific account to export.</param>
    /// <param name="outputPath">Optional output path for the export.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the export operation.</returns>
    public async Task<VandelayResult> RunExportAsync(VandelayConfig? config = null, string? account = null, string? outputPath = null, CancellationToken cancellationToken = default)
    {
        var args = new List<string> { "export", "imap" };

        if (account != null)
        {
            args.Add("--account");
            args.Add(account);
        }

        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            args.Add("--output");
            args.Add(outputPath);
        }

        // Add configuration-specific arguments
        AddCommonArguments(args, config);

        return await RunAsync("export", args, config, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs Vandelay check command to verify installation.
    /// </summary>
    /// <param name="executablePath">Optional path to the Vandelay executable.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the check operation.</returns>
    public async Task<VandelayResult> RunCheckAsync(string? executablePath = null, CancellationToken cancellationToken = default)
    {
        var args = new List<string> { "--version" };
        var config = executablePath != null ? new VandelayConfig(executablePath) : null;
        
        return await RunAsync("check", args, config, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Validates that Vandelay is available.
    /// </summary>
    /// <param name="executablePath">The path to the Vandelay executable.</param>
    /// <param name="config">The Vandelay configuration.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if Vandelay is available; otherwise, false.</returns>
    private async Task<bool> ValidateVandelayAsync(string? executablePath, VandelayConfig? config, CancellationToken cancellationToken)
    {
        // If we have a config with an explicit path, validate it
        if (config != null && !string.IsNullOrWhiteSpace(config.ExecutablePath))
        {
            var result = await _validator.ValidateAsync(config.ExecutablePath, cancellationToken).ConfigureAwait(false);
            return result.IsValid;
        }
        
        // Otherwise, validate the provided path
        if (!string.IsNullOrWhiteSpace(executablePath))
        {
            var result = await _validator.ValidateAsync(executablePath, cancellationToken).ConfigureAwait(false);
            return result.IsValid;
        }

        // Try to find Vandelay in default locations
        var validationResult = await _validator.ValidateAsync(null, cancellationToken).ConfigureAwait(false);
        return validationResult.IsValid;
    }

    /// <summary>
    /// Builds the argument list for a Vandelay command.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="arguments">Additional arguments.</param>
    /// <param name="config">The Vandelay configuration.</param>
    /// <returns>The complete argument list.</returns>
    private IEnumerable<string> BuildArgumentList(string command, IEnumerable<string>? arguments, VandelayConfig? config)
    {
        var args = new List<string> { command };

        // Add additional arguments
        if (arguments != null)
        {
            args.AddRange(arguments);
        }

        // Add configuration-specific arguments
        AddCommonArguments(args, config);

        return args;
    }

    /// <summary>
    /// Adds common arguments based on configuration.
    /// </summary>
    /// <param name="args">The argument list.</param>
    /// <param name="config">The Vandelay configuration.</param>
    private void AddCommonArguments(List<string> args, VandelayConfig? config)
    {
        if (config == null) return;

        // Add verbosity
        if (config.Verbose)
        {
            args.Add("--verbose");
        }

        switch (config.LogLevel)
        {
            case VandelayLogLevel.Error:
                args.Add("--log-level");
                args.Add("error");
                break;
            case VandelayLogLevel.Warning:
                args.Add("--log-level");
                args.Add("warning");
                break;
            case VandelayLogLevel.Debug:
                args.Add("--log-level");
                args.Add("debug");
                break;
            case VandelayLogLevel.Trace:
                args.Add("--log-level");
                args.Add("trace");
                break;
            // Info and None use defaults
        }

        // SSL configuration
        if (config.SkipSslValidation)
        {
            args.Add("--skip-ssl-validation");
        }

        // Source configuration (for import/export)
        if (config.Source != null && !string.IsNullOrWhiteSpace(config.Source.Host))
        {
            args.Add("--imap-host");
            args.Add(config.Source.Host);

            args.Add("--imap-port");
            args.Add(config.Source.Port.ToString());

            if (!config.Source.UseSsl)
            {
                args.Add("--imap-no-ssl");
            }

            if (!string.IsNullOrWhiteSpace(config.Source.Username))
            {
                args.Add("--imap-username");
                args.Add(config.Source.Username);
            }

            if (!string.IsNullOrWhiteSpace(config.Source.Password))
            {
                args.Add("--imap-password");
                args.Add(config.Source.Password);
            }
        }

        // Destination configuration (for import)
        if (config.Destination != null && !string.IsNullOrWhiteSpace(config.Destination.Url))
        {
            args.Add("--jmap-url");
            args.Add(config.Destination.Url);

            if (!string.IsNullOrWhiteSpace(config.Destination.Username))
            {
                args.Add("--jmap-username");
                args.Add(config.Destination.Username);
            }

            if (!string.IsNullOrWhiteSpace(config.Destination.Password))
            {
                args.Add("--jmap-password");
                args.Add(config.Destination.Password);
            }

            if (config.Destination.UseApiKey && !string.IsNullOrWhiteSpace(config.Destination.ApiKey))
            {
                args.Add("--jmap-api-key");
                args.Add(config.Destination.ApiKey);
            }
        }

        // Proxy configuration
        if (config.Proxy != null && !string.IsNullOrWhiteSpace(config.Proxy.Host))
        {
            args.Add("--proxy-host");
            args.Add(config.Proxy.Host);

            args.Add("--proxy-port");
            args.Add(config.Proxy.Port.ToString());

            if (!string.IsNullOrWhiteSpace(config.Proxy.Username))
            {
                args.Add("--proxy-username");
                args.Add(config.Proxy.Username);
            }

            if (!string.IsNullOrWhiteSpace(config.Proxy.Password))
            {
                args.Add("--proxy-password");
                args.Add(config.Proxy.Password);
            }
        }
    }

    /// <summary>
    /// Creates a ProcessStartInfo for running Vandelay.
    /// </summary>
    /// <param name="executablePath">The path to the Vandelay executable.</param>
    /// <param name="arguments">The arguments for the command.</param>
    /// <param name="config">The Vandelay configuration.</param>
    /// <returns>The ProcessStartInfo.</returns>
    private ProcessStartInfo CreateProcessStartInfo(string executablePath, IEnumerable<string> arguments, VandelayConfig? config)
    {
        var argList = string.Join(" ", arguments.Select(EscapeArgument));

        return new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = argList,
            WorkingDirectory = config?.WorkingDirectory ?? string.Empty,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            // Ensure environment variables are inherited
            EnvironmentVariables = { }
        };
    }

    /// <summary>
    /// Escapes a command line argument.
    /// </summary>
    /// <param name="argument">The argument to escape.</param>
    /// <returns>The escaped argument.</returns>
    private string EscapeArgument(string argument)
    {
        if (string.IsNullOrWhiteSpace(argument))
            return string.Empty;

        // If the argument contains spaces and is not already quoted, quote it
        if (argument.Contains(' ') && !argument.StartsWith('"') && !argument.EndsWith('"'))
        {
            return $"\"{argument.Replace("\"", "\\\"")}\"";
        }

        return argument;
    }

    /// <summary>
    /// Kills the current running process.
    /// </summary>
    public void Kill()
    {
        try
        {
            if (_currentProcess != null && !_currentProcess.HasExited)
            {
                _logger.LogWarning("Killing running Vandelay process");
                _currentProcess.Kill(entireProcessTree: true);
                _currentProcess.WaitForExit();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error killing Vandelay process");
        }
        finally
        {
            _currentProcess = null;
        }
    }

    /// <summary>
    /// Disposes the runner and kills any running processes.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            Kill();
            _disposed = true;
            _logger.LogDebug("Disposed VandelayRunner");
        }
    }
}
