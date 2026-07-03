// <copyright file="CoreServicesTests.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StalwartMigration.Core.Models;
using StalwartMigration.Core.Services;

namespace StalwartMigration.Tests.Unit.CoreTests;

[TestClass]
public class CompressionServiceTests
{
    private ILogger<CompressionService> _logger = NullLogger<CompressionService>.Instance;
    private string _testDir = Path.Combine(Path.GetTempPath(), "CompressionServiceTests");

    [TestInitialize]
    public void Setup()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
        Directory.CreateDirectory(_testDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }

    [TestMethod]
    public void Constructor_WithDefaultLogger_CreatesService()
    {
        var service = new CompressionService();
        Assert.IsNotNull(service);
    }

    [TestMethod]
    public void Constructor_WithCustomLogger_SetsLogger()
    {
        var service = new CompressionService(_logger);
        Assert.IsNotNull(service);
    }
}

[TestClass]
public class CheckpointServiceTests
{
    private ILogger<CheckpointService> _logger = NullLogger<CheckpointService>.Instance;
    private string _testDir = Path.Combine(Path.GetTempPath(), "CheckpointServiceTests");

    [TestInitialize]
    public void Setup()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
        Directory.CreateDirectory(_testDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }

    [TestMethod]
    public void Constructor_WithValidBaseDirectory_CreatesService()
    {
        var service = new CheckpointService(_testDir, _logger);
        Assert.IsNotNull(service);
        Assert.AreEqual(_testDir, service.BaseDirectory);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Constructor_WithNullBaseDirectory_ThrowsArgumentException()
    {
        new CheckpointService(null!, _logger);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Constructor_WithEmptyBaseDirectory_ThrowsArgumentException()
    {
        new CheckpointService(string.Empty, _logger);
    }

    [TestMethod]
    public async Task CreateCheckpointAsync_WithValidState_SavesFile()
    {
        var service = new CheckpointService(_testDir, _logger);
        string checkpointName = "test-checkpoint";
        var state = new Dictionary<string, object>
        {
            { "id", "test-1" },
            { "startedAt", DateTimeOffset.UtcNow.ToString() },
            { "lastUpdatedAt", DateTimeOffset.UtcNow.ToString() }
        };

        await service.CreateCheckpointAsync(checkpointName, state);

        string checkpointPath = Path.Combine(_testDir, "checkpoints", checkpointName + ".json");
        Assert.IsTrue(File.Exists(checkpointPath));
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task CreateCheckpointAsync_WithNullName_ThrowsArgumentException()
    {
        var service = new CheckpointService(_testDir, _logger);
        var state = new Dictionary<string, object>();
        await service.CreateCheckpointAsync(null!, state);
    }

    [TestMethod]
    [ExpectedException(typeof(NullReferenceException))]
    public async Task CreateCheckpointAsync_WithNullState_ThrowsNullReferenceException()
    {
        var service = new CheckpointService(_testDir, _logger);
        await service.CreateCheckpointAsync("test", null!);
    }

    [TestMethod]
    public async Task LoadCheckpointAsync_WithValidFile_ReturnsState()
    {
        var service = new CheckpointService(_testDir, _logger);
        string checkpointName = "test-checkpoint";
        var originalState = new Dictionary<string, object>
        {
            { "id", "test-1" },
            { "startedAt", DateTimeOffset.UtcNow.ToString() }
        };

        await service.CreateCheckpointAsync(checkpointName, originalState);
        string checkpointPath = service.GetCheckpointPath(checkpointName);
        var loadedState = await service.LoadCheckpointAsync(checkpointPath);

        Assert.IsNotNull(loadedState);
        Assert.AreEqual("test-1", loadedState["id"]);
    }

    [TestMethod]
    [ExpectedException(typeof(FileNotFoundException))]
    public async Task LoadCheckpointAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        var service = new CheckpointService(_testDir, _logger);
        string checkpointPath = Path.Combine(_testDir, "nonexistent.json");
        await service.LoadCheckpointAsync(checkpointPath);
    }

    [TestMethod]
    public async Task CheckpointExistsAsync_WithExistingFile_ReturnsTrue()
    {
        var service = new CheckpointService(_testDir, _logger);
        string checkpointName = "test-exists";
        var state = new Dictionary<string, object>();
        await service.CreateCheckpointAsync(checkpointName, state);

        bool exists = await service.CheckpointExistsAsync(checkpointName);
        Assert.IsTrue(exists);
    }

    [TestMethod]
    public async Task CheckpointExistsAsync_WithNonExistentFile_ReturnsFalse()
    {
        var service = new CheckpointService(_testDir, _logger);
        bool exists = await service.CheckpointExistsAsync("nonexistent");
        Assert.IsFalse(exists);
    }
}

[TestClass]
public class ValidationServiceTests
{
    private ILogger<ValidationService> _logger = NullLogger<ValidationService>.Instance;

    [TestMethod]
    public void Constructor_WithDefaultLogger_CreatesService()
    {
        var service = new ValidationService();
        Assert.IsNotNull(service);
    }

    [TestMethod]
    public void Constructor_WithCustomLogger_SetsLogger()
    {
        var service = new ValidationService(_logger);
        Assert.IsNotNull(service);
    }

    [TestMethod]
    public void ValidateNotNull_WithValidObject_ReturnsSuccess()
    {
        var service = new ValidationService(_logger);
        var result = service.ValidateNotNull("test", "value");
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void ValidateNotNull_WithNull_ReturnsFailure()
    {
        var service = new ValidationService(_logger);
        var result = service.ValidateNotNull(null, "value");
        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public void ValidateString_WithValidString_ReturnsSuccess()
    {
        var service = new ValidationService(_logger);
        var result = service.ValidateString("test string", "value");
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void ValidateString_WithNull_ReturnsFailure()
    {
        var service = new ValidationService(_logger);
        var result = service.ValidateString(null, "value");
        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public void ValidateString_WithEmptyAndNotAllowed_ReturnsFailure()
    {
        var service = new ValidationService(_logger);
        // Note: With minLength=0 (default), empty string passes. Need minLength=1 to reject empty strings.
        var result = service.ValidateString(string.Empty, "value", minLength: 1, allowNullOrEmpty: false);
        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public void ValidateString_WithEmptyAndAllowed_ReturnsSuccess()
    {
        var service = new ValidationService(_logger);
        var result = service.ValidateString(string.Empty, "value", allowNullOrEmpty: true);
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void ValidateEmail_WithValidEmail_ReturnsSuccess()
    {
        var service = new ValidationService(_logger);
        var result = service.ValidateEmail("test@example.com");
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void ValidateEmail_WithInvalidEmail_ReturnsFailure()
    {
        var service = new ValidationService(_logger);
        var result = service.ValidateEmail("not-an-email");
        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public void ValidateDomain_WithValidDomain_ReturnsSuccess()
    {
        var service = new ValidationService(_logger);
        var result = service.ValidateDomain("example.com");
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void ValidateDomain_WithInvalidDomain_ReturnsFailure()
    {
        var service = new ValidationService(_logger);
        var result = service.ValidateDomain("");
        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public void ValidateCollection_WithValidCollection_ReturnsSuccess()
    {
        var service = new ValidationService(_logger);
        var result = service.ValidateCollection(new List<string> { "item1", "item2" }, "collection");
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void ValidateCollection_WithNull_ReturnsFailure()
    {
        var service = new ValidationService(_logger);
        var result = service.ValidateCollection(null, "collection");
        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public void ValidateCollection_WithEmptyAndNotAllowed_ReturnsFailure()
    {
        var service = new ValidationService(_logger);
        var result = service.ValidateCollection(new List<string>(), "collection", allowEmpty: false);
        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public void ValidateCollection_WithEmptyAndAllowed_ReturnsSuccess()
    {
        var service = new ValidationService(_logger);
        var result = service.ValidateCollection(new List<string>(), "collection", allowEmpty: true);
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void ValidateFilePath_WithValidPath_ReturnsSuccess()
    {
        var service = new ValidationService(_logger);
        // Use a simple valid path - the PathSanitizer might be strict
        var result = service.ValidateFilePath("test.txt", "path");
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void ValidateFilePath_WithInvalidPath_ReturnsFailure()
    {
        var service = new ValidationService(_logger);
        var result = service.ValidateFilePath("", "path");
        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public void ValidateDirectoryPath_WithValidPath_ReturnsSuccess()
    {
        var service = new ValidationService(_logger);
        // Use a simple valid path
        var result = service.ValidateDirectoryPath("test", "path");
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void ValidateDirectoryPath_WithInvalidPath_ReturnsFailure()
    {
        var service = new ValidationService(_logger);
        var result = service.ValidateDirectoryPath("", "path");
        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public void ValidateRange_WithValidValue_ReturnsSuccess()
    {
        var service = new ValidationService(_logger);
        var result = service.ValidateRange(50, "value", 0, 100);
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public void ValidateRange_WithOutOfRangeValue_ReturnsFailure()
    {
        var service = new ValidationService(_logger);
        var result = service.ValidateRange(150, "value", 0, 100);
        Assert.IsFalse(result.IsValid);
    }
}
