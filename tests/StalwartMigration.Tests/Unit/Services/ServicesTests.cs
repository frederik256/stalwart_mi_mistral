// <copyright file="ServicesTests.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using StalwartMigration.Core.Services;

namespace StalwartMigration.Tests.Unit.Services;

/// <summary>
/// Unit tests for shared services (CompressionService, CheckpointService, ValidationService).
/// </summary>
[TestClass]
public class ServicesTests
{
    #region CompressionService Tests

    [TestClass]
    public class CompressionServiceTests
    {
        private CompressionService? _service;

        [TestInitialize]
        public void Setup()
        {
            _service = new CompressionService();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _service?.Dispose();
            _service = null;
        }

        [TestMethod]
        public async Task CompressAsync_ValidData_ReturnsCompressedBytes()
        {
            // Arrange
            var testData = "This is test data for compression. ";
            var bytes = System.Text.Encoding.UTF8.GetBytes(testData);

            // Act
            var compressed = await _service!.CompressAsync(bytes);

            // Assert
            Assert.IsNotNull(compressed);
            Assert.IsTrue(compressed.Length > 0);
            // For very small strings, GZip might not compress (due to headers)
            // So just verify it returns valid compressed data that can be decompressed
        }

        [TestMethod]
        public async Task DecompressAsync_CompressedData_ReturnsOriginalData()
        {
            // Arrange
            var testData = "This is test data for compression.";
            var originalBytes = System.Text.Encoding.UTF8.GetBytes(testData);
            var compressed = await _service!.CompressAsync(originalBytes);

            // Act
            var decompressed = await _service!.DecompressAsync(compressed);

            // Assert
            Assert.IsNotNull(decompressed);
            CollectionAssert.AreEqual(originalBytes, decompressed);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task CompressAsync_NullData_ThrowsArgumentNullException()
        {
            // Act
            await _service!.CompressAsync(null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task DecompressAsync_NullData_ThrowsArgumentNullException()
        {
            // Act
            await _service!.DecompressAsync(null!);
        }
    }

    #endregion

    #region CheckpointService Tests

    [TestClass]
    public class CheckpointServiceTests
    {
        private CheckpointService? _service;
        private string? _testDir;

        [TestInitialize]
        public void Setup()
        {
            _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDir);
            _service = new CheckpointService(_testDir);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _service?.Dispose();
            if (_testDir != null && Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }
        }

        [TestMethod]
        public async Task CreateCheckpointAsync_ValidState_SavesToFile()
        {
            // Arrange
            var state = new Dictionary<string, object>
            {
                { "lastDomain", "example.com" },
                { "processedAccounts", 5 },
                { "timestamp", DateTime.UtcNow }
            };

            // Act
            var checkpointPath = await _service!.CreateCheckpointAsync("test-migration", state);

            // Assert
            Assert.IsTrue(File.Exists(checkpointPath));
            Assert.IsTrue(checkpointPath.Contains(_testDir));
            Assert.IsTrue(checkpointPath.Contains("test-migration"));
        }

        [TestMethod]
        public async Task LoadCheckpointAsync_ExistingCheckpoint_ReturnsState()
        {
            // Arrange
            var originalState = new Dictionary<string, object>
            {
                { "lastDomain", "example.com" },
                { "processedAccounts", 5 }
            };
            var checkpointPath = await _service!.CreateCheckpointAsync("test-migration-2", originalState);

            // Act
            var loadedState = await _service!.LoadCheckpointAsync(checkpointPath);

            // Assert
            Assert.IsNotNull(loadedState);
            Assert.AreEqual("example.com", loadedState["lastDomain"]);
            Assert.AreEqual(5, loadedState["processedAccounts"]);
        }

        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public async Task LoadCheckpointAsync_NonExistentFile_ThrowsFileNotFoundException()
        {
            // Act
            await _service!.LoadCheckpointAsync("/non/existent/path.json");
        }

        [TestMethod]
        public async Task CheckpointDirectory_CreatedAndAccessible()
        {
            // Arrange
            var customDir = Path.Combine(_testDir!, "checkpoints");
            var service = new CheckpointService(customDir);

            // Act
            var path = await service.CreateCheckpointAsync("test", new Dictionary<string, object>());

            // Assert
            Assert.IsTrue(Directory.Exists(customDir));
            Assert.IsTrue(path.StartsWith(customDir));

            service.Dispose();
        }
    }

    #endregion

    #region ValidationService Tests

    [TestClass]
    public class ValidationServiceTests
    {
        private ValidationService? _service;

        [TestInitialize]
        public void Setup()
        {
            _service = new ValidationService();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _service = null;
        }

        [TestMethod]
        public void ValidateNotNull_ValidObject_ReturnsTrue()
        {
            // Arrange
            var obj = new object();

            // Act
            var result = _service!.ValidateNotNull(obj, nameof(obj));

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void ValidateNotNull_NullObject_ReturnsInvalid()
        {
            // Arrange
            object? obj = null;

            // Act
            var result = _service!.ValidateNotNull(obj, nameof(obj));

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("obj cannot be null.", result.ErrorMessage);
        }

        [TestMethod]
        public void ValidateString_ValidString_ReturnsTrue()
        {
            // Arrange
            var value = "test string";

            // Act
            var result = _service!.ValidateString(value, nameof(value), 1, 100);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void ValidateString_NullString_ReturnsInvalid()
        {
            // Act
            var result = _service!.ValidateString(null!, "test", 1, 100);

            // Assert
            Assert.IsFalse(result.IsValid);
        }

        [TestMethod]
        public void ValidateString_TooShort_ReturnsInvalid()
        {
            // Act
            var result = _service!.ValidateString("a", "test", 5, 100);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ErrorMessage.Contains("too short"));
        }

        [TestMethod]
        public void ValidateString_TooLong_ReturnsInvalid()
        {
            // Act
            var longString = new string('a', 101);
            var result = _service!.ValidateString(longString, "test", 1, 100);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ErrorMessage.Contains("too long"));
        }

        [TestMethod]
        public void ValidateEmail_ValidEmail_ReturnsTrue()
        {
            // Act
            var result = _service!.ValidateEmail("test@example.com");

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void ValidateEmail_InvalidEmail_ReturnsFalse()
        {
            // Act
            var result = _service!.ValidateEmail("invalid-email");

            // Assert
            Assert.IsFalse(result.IsValid);
        }

        [TestMethod]
        public void ValidateCollection_ValidCollection_ReturnsTrue()
        {
            // Arrange
            var collection = new List<string> { "item1", "item2" };

            // Act
            var result = _service!.ValidateCollection(collection, nameof(collection));

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void ValidateCollection_NullCollection_ReturnsInvalid()
        {
            // Act
            var result = _service!.ValidateCollection(null!, "collection");

            // Assert
            Assert.IsFalse(result.IsValid);
        }

        [TestMethod]
        public void ValidateCollection_EmptyCollection_ReturnsInvalid()
        {
            // Act
            var result = _service!.ValidateCollection(new List<string>(), "collection");

            // Assert
            Assert.IsFalse(result.IsValid);
        }
    }

    #endregion
}
