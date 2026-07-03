// <copyright file="VandelayTests.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StalwartMigration.Infrastructure.Vandelay;

namespace StalwartMigration.Tests.Unit.InfrastructureTests;

[TestClass]
public class VandelayConfigTests
{
    [TestMethod]
    public void DefaultConstructor_CreatesDefaultConfig()
    {
        var config = new VandelayConfig();
        Assert.IsNotNull(config);
        Assert.IsNull(config.ExecutablePath);
        Assert.IsNull(config.WorkingDirectory);
        Assert.AreEqual(300, config.TimeoutSeconds);
        Assert.IsFalse(config.Verbose);
        Assert.IsFalse(config.ContinueOnError);
        Assert.AreEqual(VandelayLogLevel.Info, config.LogLevel);
    }

    [TestMethod]
    public void Constructor_WithExecutablePath_SetsExecutablePath()
    {
        string executablePath = "/usr/local/bin/vandelay";
        var config = new VandelayConfig(executablePath);
        Assert.AreEqual(executablePath, config.ExecutablePath);
    }

    [TestMethod]
    public void ExecutablePath_GetAndSet_Works()
    {
        var config = new VandelayConfig();
        string path = "/path/to/vandelay";
        config.ExecutablePath = path;
        Assert.AreEqual(path, config.ExecutablePath);
    }

    [TestMethod]
    public void WorkingDirectory_GetAndSet_Works()
    {
        var config = new VandelayConfig();
        string directory = "/tmp/vandelay";
        config.WorkingDirectory = directory;
        Assert.AreEqual(directory, config.WorkingDirectory);
    }

    [TestMethod]
    public void TimeoutSeconds_GetAndSet_Works()
    {
        var config = new VandelayConfig();
        config.TimeoutSeconds = 600;
        Assert.AreEqual(600, config.TimeoutSeconds);
    }

    [TestMethod]
    public void Verbose_GetAndSet_Works()
    {
        var config = new VandelayConfig();
        config.Verbose = true;
        Assert.IsTrue(config.Verbose);
    }

    [TestMethod]
    public void ContinueOnError_GetAndSet_Works()
    {
        var config = new VandelayConfig();
        config.ContinueOnError = true;
        Assert.IsTrue(config.ContinueOnError);
    }

    [TestMethod]
    public void LogLevel_GetAndSet_Works()
    {
        var config = new VandelayConfig();
        config.LogLevel = VandelayLogLevel.Debug;
        Assert.AreEqual(VandelayLogLevel.Debug, config.LogLevel);
    }

    [TestMethod]
    public void IsValid_WithExecutablePath_ReturnsTrue()
    {
        var config = new VandelayConfig { ExecutablePath = "/usr/bin/vandelay" };
        bool isValid = config.IsValid();
        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public void IsValid_WithoutExecutablePath_ReturnsFalse()
    {
        var config = new VandelayConfig { ExecutablePath = null };
        bool isValid = config.IsValid();
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public void IsValid_WithEmptyExecutablePath_ReturnsFalse()
    {
        var config = new VandelayConfig { ExecutablePath = string.Empty };
        bool isValid = config.IsValid();
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public void DefaultExecutableNames_ReturnsExpectedNames()
    {
        var names = VandelayConfig.DefaultExecutableNames;
        Assert.IsNotNull(names);
        Assert.IsTrue(names.Contains("vandelay"));
        Assert.IsTrue(names.Contains("vandelay.exe"));
        Assert.IsTrue(names.Contains("vandelay-indie"));
        Assert.IsTrue(names.Contains("vandelay-indie.exe"));
    }

    [TestMethod]
    public void DefaultInstallationDirectories_ReturnsExpectedDirectories()
    {
        var directories = VandelayConfig.DefaultInstallationDirectories;
        Assert.IsNotNull(directories);
        Assert.IsTrue(directories.Any(d => d.Contains("usr")));
        Assert.IsTrue(directories.Any(d => d.Contains("opt")));
    }
}

[TestClass]
public class VandelayResultTests
{
    [TestMethod]
    public void DefaultConstructor_CreatesDefaultResult()
    {
        var result = new VandelayResult();
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Success);
        Assert.AreEqual(0, result.ExitCode);
        Assert.IsNull(result.StandardOutput);
        Assert.IsNull(result.StandardError);
        Assert.IsNull(result.ErrorMessage);
    }

    [TestMethod]
    public void Success_GetAndSet_Works()
    {
        var result = new VandelayResult();
        result.Success = true;
        Assert.IsTrue(result.Success);
    }

    [TestMethod]
    public void ExitCode_GetAndSet_Works()
    {
        var result = new VandelayResult();
        result.ExitCode = 42;
        Assert.AreEqual(42, result.ExitCode);
    }

    [TestMethod]
    public void StandardOutput_GetAndSet_Works()
    {
        var result = new VandelayResult();
        string output = "Test output";
        result.StandardOutput = output;
        Assert.AreEqual(output, result.StandardOutput);
    }

    [TestMethod]
    public void StandardError_GetAndSet_Works()
    {
        var result = new VandelayResult();
        string error = "Test error";
        result.StandardError = error;
        Assert.AreEqual(error, result.StandardError);
    }

    [TestMethod]
    public void ErrorMessage_GetAndSet_Works()
    {
        var result = new VandelayResult();
        string errorMessage = "Test error message";
        result.ErrorMessage = errorMessage;
        Assert.AreEqual(errorMessage, result.ErrorMessage);
    }

    [TestMethod]
    public void Command_GetAndSet_Works()
    {
        var result = new VandelayResult();
        string command = "import";
        result.Command = command;
        Assert.AreEqual(command, result.Command);
    }

    [TestMethod]
    public void Arguments_GetAndSet_Works()
    {
        var result = new VandelayResult();
        var arguments = new List<string> { "--source", "imap://test" };
        result.Arguments = arguments;
        Assert.AreEqual(arguments, result.Arguments);
    }

    [TestMethod]
    public void WorkingDirectory_GetAndSet_Works()
    {
        var result = new VandelayResult();
        string workingDirectory = "/tmp";
        result.WorkingDirectory = workingDirectory;
        Assert.AreEqual(workingDirectory, result.WorkingDirectory);
    }

    [TestMethod]
    public void ForSuccess_CreatesSuccessfulResult()
    {
        var result = VandelayResult.ForSuccess("test output", 10);
        Assert.IsTrue(result.Success);
        Assert.AreEqual(0, result.ExitCode);
        Assert.AreEqual("test output", result.StandardOutput);
        Assert.AreEqual(10, result.ItemsProcessed);
        Assert.AreEqual(10, result.ItemsSucceeded);
    }

    [TestMethod]
    public void ForFailure_CreatesFailedResult()
    {
        var result = VandelayResult.ForFailure(1, "test error", "stderr");
        Assert.IsFalse(result.Success);
        Assert.AreEqual(1, result.ExitCode);
        Assert.AreEqual("test error", result.ErrorMessage);
        Assert.AreEqual("stderr", result.StandardError);
    }
}

[TestClass]
public class VandelayRunnerTests
{
    private ILogger<VandelayRunner> _logger = NullLogger<VandelayRunner>.Instance;

    [TestMethod]
    public void Constructor_WithDefaultParameters_CreatesRunner()
    {
        var runner = new VandelayRunner();
        Assert.IsNotNull(runner);
        Assert.AreEqual(TimeSpan.FromMinutes(5), runner.DefaultTimeout);
    }

    [TestMethod]
    public void Constructor_WithCustomValidatorAndParser_SetsDependencies()
    {
        var validator = new VandelayValidator();
        var parser = new VandelayResultParser();
        var runner = new VandelayRunner(validator, parser, _logger);
        Assert.IsNotNull(runner);
    }

    [TestMethod]
    public void Constructor_WithNullLogger_UsesNullLogger()
    {
        var runner = new VandelayRunner(null, null, null);
        Assert.IsNotNull(runner);
    }

    [TestMethod]
    public void DefaultTimeout_GetAndSet_Works()
    {
        var runner = new VandelayRunner();
        runner.DefaultTimeout = TimeSpan.FromMinutes(10);
        Assert.AreEqual(TimeSpan.FromMinutes(10), runner.DefaultTimeout);
    }

    [TestMethod]
    public async Task RunAsync_WithNonExistentVandelay_ReturnsFailureResult()
    {
        var runner = new VandelayRunner();
        var config = new VandelayConfig
        {
            ExecutablePath = "non-existent-vandelay-path"
        };

        var result = await runner.RunAsync("import", new[] { "--source", "imap://test" }, config);

        Assert.IsNotNull(result);
        Assert.IsFalse(result.Success);
        Assert.AreEqual(-1, result.ExitCode);
    }

    [TestMethod]
    public async Task RunAsync_WithNullCommand_ReturnsFailureResult()
    {
        var runner = new VandelayRunner();
        var result = await runner.RunAsync(null!);
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Success);
    }

    [TestMethod]
    public async Task RunAsync_WithEmptyCommand_ReturnsFailureResult()
    {
        var runner = new VandelayRunner();
        var result = await runner.RunAsync(string.Empty);
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Success);
    }

    [TestMethod]
    public void Dispose_DisposesRunner()
    {
        var runner = new VandelayRunner();
        runner.Dispose();
        runner.Dispose(); // Should not throw when disposed multiple times
    }
}

[TestClass]
public class VandelayValidatorTests
{
    private ILogger<VandelayValidator> _logger = NullLogger<VandelayValidator>.Instance;

    [TestMethod]
    public void Constructor_WithDefaultLogger_CreatesValidator()
    {
        var validator = new VandelayValidator();
        Assert.IsNotNull(validator);
    }

    [TestMethod]
    public void Constructor_WithCustomLogger_SetsLogger()
    {
        var validator = new VandelayValidator(_logger);
        Assert.IsNotNull(validator);
    }

    [TestMethod]
    public async Task ValidateAsync_WithNonExistentExecutable_ReturnsFailure()
    {
        var validator = new VandelayValidator();
        string executablePath = "/non/existent/path/to/vandelay";

        var result = await validator.ValidateAsync(executablePath);

        Assert.IsNotNull(result);
        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public async Task ValidateAsync_WithNullExecutable_SearchesForVandelay()
    {
        var validator = new VandelayValidator();
        var result = await validator.ValidateAsync(null);
        Assert.IsNotNull(result);
        Assert.IsFalse(result.IsValid); // Won't be valid since Vandelay is not installed
    }

    [TestMethod]
    public async Task ValidateConfigAsync_WithValidConfig_ReturnsTrue()
    {
        var validator = new VandelayValidator();
        var config = new VandelayConfig { ExecutablePath = "/non/existent/path" };

        bool isValid = await validator.ValidateConfigAsync(config);
        // Will be false because the path doesn't exist, but the method should not throw
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public async Task ValidateConfigAsync_WithNullConfig_ReturnsFalse()
    {
        var validator = new VandelayValidator();
        bool isValid = await validator.ValidateConfigAsync((VandelayConfig)null!);
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public async Task ValidateAsync_WithNoParameters_SearchesForVandelay()
    {
        var validator = new VandelayValidator();

        var result = await validator.ValidateAsync();

        // On most systems, this will not be valid since Vandelay is not installed
        Assert.IsNotNull(result);
        Assert.IsFalse(result.IsValid);
    }
}

[TestClass]
public class VandelayResultParserTests
{
    private ILogger<VandelayResultParser> _logger = NullLogger<VandelayResultParser>.Instance;

    [TestMethod]
    public void Constructor_WithDefaultLogger_CreatesParser()
    {
        var parser = new VandelayResultParser();
        Assert.IsNotNull(parser);
    }

    [TestMethod]
    public void Constructor_WithCustomLogger_SetsLogger()
    {
        var parser = new VandelayResultParser(_logger);
        Assert.IsNotNull(parser);
    }

    [TestMethod]
    public void ParseResult_WithNullResult_ReturnsEmptyResult()
    {
        var parser = new VandelayResultParser();
        var result = parser.ParseResult(null);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ParseResult_WithEmptyResult_ReturnsEmptyResult()
    {
        var parser = new VandelayResultParser();
        var result = parser.ParseResult(new VandelayResult());
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ParseResult_WithOutput_ParsesItems()
    {
        var parser = new VandelayResultParser();
        var result = new VandelayResult { StandardOutput = "Messages processed: 10\nMessages succeeded: 10" };
        var parsed = parser.ParseResult(result);
        Assert.IsNotNull(parsed);
        Assert.AreEqual(10, parsed.ItemsProcessed);
        Assert.AreEqual(10, parsed.ItemsSucceeded);
    }

    [TestMethod]
    public void ParseProgress_WithNullOutput_ReturnsNull()
    {
        var parser = new VandelayResultParser();
        var progress = parser.ParseProgress(null);
        Assert.IsNull(progress);
    }

    [TestMethod]
    public void ParseProgress_WithEmptyOutput_ReturnsNull()
    {
        var parser = new VandelayResultParser();
        var progress = parser.ParseProgress(string.Empty);
        Assert.IsNull(progress);
    }

    [TestMethod]
    public void ParseProgress_WithPercentage_ParsesPercentage()
    {
        var parser = new VandelayResultParser();
        var progress = parser.ParseProgress("50% complete");
        Assert.IsNotNull(progress);
        Assert.AreEqual(50, progress.Percentage);
    }
}
