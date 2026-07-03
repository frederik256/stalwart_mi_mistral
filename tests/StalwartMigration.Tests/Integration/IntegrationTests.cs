// <copyright file="IntegrationTests.cs" company="Stalwart Labs">
// Copyright (c) Stalwart Labs 2024. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using StalwartMigration.Core;
using StalwartMigration.Core.Exporters;
using StalwartMigration.Core.Importers;
using StalwartMigration.Core.Models;
using StalwartMigration.Core.Services;
using StalwartMigration.Infrastructure.FileSystem;
using StalwartMigration.Infrastructure.HMailServer;
using StalwartMigration.Infrastructure.Stalwart;

namespace StalwartMigration.Tests.Integration;

/// <summary>
/// Integration tests for component interactions.
/// </summary>
[TestClass]
[TestCategory("Integration")]
public class IntegrationTests
{
    private string _tempDir = string.Empty;

    [TestInitialize]
    public void TestInitialize()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, true);
            }
            catch { /* Ignore */ }
        }
    }

    [TestMethod]
    public async Task CheckpointService_CreateAndLoadCheckpoint_WorksCorrectly()
    {
        // Arrange
        var checkpointDir = Path.Combine(_tempDir, "checkpoints");
        var checkpointService = new CheckpointService(checkpointDir);
        
        var state = new Dictionary<string, object>
        {
            { "phase", "migration" },
            { "domainsProcessed", 5 },
            { "accountsProcessed", 100 },
            { "timestamp", DateTime.UtcNow }
        };

        // Act - Create checkpoint
        var checkpointPath = await checkpointService.CreateCheckpointAsync("test_migration", state, CancellationToken.None);
        
        // Assert - Checkpoint was created
        Assert.IsNotNull(checkpointPath);
        Assert.IsTrue(File.Exists(checkpointPath), "Checkpoint file should exist");
        
        // Act - Load checkpoint
        var loadedState = await checkpointService.LoadCheckpointAsync(checkpointPath, CancellationToken.None);
        
        // Assert - State was loaded correctly
        Assert.IsNotNull(loadedState);
        Assert.AreEqual("migration", loadedState["phase"] as string);
        Assert.AreEqual(5, loadedState["domainsProcessed"]);
        Assert.AreEqual(100, loadedState["accountsProcessed"]);
    }

    [TestMethod]
    public async Task CheckpointService_MultipleCheckpoints_ManagesCorrectly()
    {
        // Arrange
        var checkpointDir = Path.Combine(_tempDir, "checkpoints2");
        var checkpointService = new CheckpointService(checkpointDir);
        
        var state1 = new Dictionary<string, object> { { "step", 1 } };
        var state2 = new Dictionary<string, object> { { "step", 2 } };

        // Act
        var path1 = await checkpointService.CreateCheckpointAsync("migration1", state1, CancellationToken.None);
        var path2 = await checkpointService.CreateCheckpointAsync("migration2", state2, CancellationToken.None);

        // Assert
        var checkpoint1 = await checkpointService.LoadCheckpointAsync(path1, CancellationToken.None);
        var checkpoint2 = await checkpointService.LoadCheckpointAsync(path2, CancellationToken.None);
        
        Assert.IsNotNull(checkpoint1);
        Assert.IsNotNull(checkpoint2);
        Assert.AreEqual(1, checkpoint1["step"]);
        Assert.AreEqual(2, checkpoint2["step"]);
    }

    [TestMethod]
    [ExpectedException(typeof(FileNotFoundException))]
    public async Task CheckpointService_NonExistentCheckpoint_ThrowsFileNotFoundException()
    {
        // Arrange
        var checkpointDir = Path.Combine(_tempDir, "checkpoints3");
        var checkpointService = new CheckpointService(checkpointDir);

        // Act & Assert
        await checkpointService.LoadCheckpointAsync(Path.Combine(checkpointDir, "nonexistent.json"), CancellationToken.None);
    }

    [TestMethod]
    public async Task ArchiveManager_CreateArchive_WorksCorrectly()
    {
        // Arrange
        var archiveDir = Path.Combine(_tempDir, "archives");
        Directory.CreateDirectory(archiveDir);
        
        var archiveManager = new ArchiveManager(archiveDir);
        var testData = new { Name = "Test Domain", Accounts = 5 };

        // Act - Create archive
        var archivePath = await archiveManager.CreateDomainArchiveAsync("test_domain", testData, false, CancellationToken.None);
        
        // Assert
        Assert.IsNotNull(archivePath);
        Assert.IsTrue(File.Exists(archivePath), "Archive file should exist");
        Assert.AreEqual(".zip", Path.GetExtension(archivePath), "Archive should be a ZIP file");
    }

    [TestMethod]
    public async Task ArchiveManager_ExtractArchive_ExtractsCorrectly()
    {
        // Arrange
        var archiveDir = Path.Combine(_tempDir, "archives2");
        Directory.CreateDirectory(archiveDir);
        
        var archiveManager = new ArchiveManager(archiveDir);
        var testData = new { Name = "Test Domain", Accounts = new List<string> { "user1", "user2" } };
        
        // Create an archive first
        var archivePath = await archiveManager.CreateDomainArchiveAsync("test_domain2", testData, false, CancellationToken.None);
        
        // Act - Extract archive
        var extractDir = Path.Combine(_tempDir, "extracted");
        Directory.CreateDirectory(extractDir);
        
        await archiveManager.ExtractArchiveAsync(archivePath, extractDir, false, CancellationToken.None);
        
        // Assert - Check that files were extracted
        var extractedFiles = Directory.GetFiles(extractDir, "*", SearchOption.AllDirectories);
        Assert.IsTrue(extractedFiles.Length > 0, "Should have extracted files");
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void CheckpointService_InvalidDirectory_ThrowsException()
    {
        // Arrange & Act
        _ = new CheckpointService(string.Empty);
    }

    [TestMethod]
    public void MigrationOptions_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var options = new MigrationOptions();

        // Assert
        Assert.IsFalse(options.SkipMessages, "SkipMessages should default to false");
        Assert.IsFalse(options.SkipValidation, "SkipValidation should default to false");
        Assert.IsTrue(options.UseVandelay, "UseVandelay should default to true");
        Assert.IsNull(options.StalwartUsername, "StalwartUsername should default to null");
        Assert.IsNull(options.StalwartPassword, "StalwartPassword should default to null");
        Assert.IsNotNull(options.DomainNames, "DomainNames should be initialized");
        Assert.AreEqual(0, options.DomainNames.Count, "DomainNames should be empty by default");
        Assert.AreEqual(10, options.BatchSize, "BatchSize should default to 10");
    }

    [TestMethod]
    public void MigrationResult_CreateSuccess_CreatesValidResult()
    {
        // Arrange & Act
        var result = MigrationResult.CreateSuccess(5, 100, 10, 500);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNull(result.ErrorMessage);
        Assert.AreEqual(5, result.DomainsProcessed);
        Assert.AreEqual(100, result.AccountsProcessed);
        Assert.AreEqual(10, result.AliasesProcessed);
        Assert.AreEqual(500, result.MessagesProcessed);
    }

    [TestMethod]
    public void MigrationResult_CreateFail_CreatesValidResult()
    {
        // Arrange & Act
        var result = MigrationResult.CreateFail("Test error message");

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual("Test error message", result.ErrorMessage);
        Assert.AreEqual(0, result.DomainsProcessed);
        Assert.AreEqual(0, result.AccountsProcessed);
        Assert.AreEqual(0, result.AliasesProcessed);
        Assert.AreEqual(0, result.MessagesProcessed);
    }

    [TestMethod]
    public void DomainResult_Creation_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var result = new DomainResult
        {
            DomainName = "test.com",
            Success = true,
            AccountsProcessed = 10,
            AliasesProcessed = 5,
            ErrorMessage = null
        };

        // Assert
        Assert.AreEqual("test.com", result.DomainName);
        Assert.IsTrue(result.Success);
        Assert.AreEqual(10, result.AccountsProcessed);
        Assert.AreEqual(5, result.AliasesProcessed);
        Assert.IsNull(result.ErrorMessage);
    }

    [TestMethod]
    public void DomainResult_FailedResult_SetsErrorMessage()
    {
        // Arrange & Act
        var result = new DomainResult
        {
            DomainName = "failed.com",
            Success = false,
            ErrorMessage = "Failed to create domain"
        };

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual("Failed to create domain", result.ErrorMessage);
    }

    [TestMethod]
    public void Exporter_InvalidOutputDirectory_CreatesDirectory()
    {
        // Arrange
        var mockHMail = new Mock<IHMailServerClient>();
        var newOutputDir = Path.Combine(_tempDir, "new_output");

        // Act
        var exporter = new HMailServerExporter(mockHMail.Object, newOutputDir);

        // Assert
        Assert.IsTrue(Directory.Exists(newOutputDir), "Output directory should be created");
    }

    [TestMethod]
    public async Task Exporter_NullDomain_ThrowsArgumentNullException()
    {
        // Arrange
        var mockHMail = new Mock<IHMailServerClient>();
        var exporter = new HMailServerExporter(mockHMail.Object, _tempDir);

        // Act & Assert
        try
        {
            await exporter.ExportDomainAsync(null!, CancellationToken.None);
            Assert.Fail("Should have thrown ArgumentNullException");
        }
        catch (ArgumentNullException)
        {
            // Expected
        }
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task Importer_ImportNullDomain_ThrowsArgumentNullException()
    {
        // Arrange
        var mockStalwart = new Mock<IStalwartClient>();
        var importer = new StalwartImporter(mockStalwart.Object, _tempDir);

        // Act & Assert
        await importer.ImportDomainAsync(null!, CancellationToken.None);
    }
}
