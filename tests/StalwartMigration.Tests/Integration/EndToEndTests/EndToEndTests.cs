// <copyright file="EndToEndTests.cs" company="Stalwart Labs">
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

namespace StalwartMigration.Tests.Integration.EndToEndTests;

/// <summary>
/// End-to-end tests for complete migration workflows.
/// </summary>
[TestClass]
[TestCategory("EndToEnd")]
public class EndToEndTests
{
    private string _tempDir = string.Empty;
    private Mock<IHMailServerClient> _mockHMailServerClient = null!;
    private Mock<IStalwartClient> _mockStalwartClient = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        
        _mockHMailServerClient = new Mock<IHMailServerClient>();
        _mockStalwartClient = new Mock<IStalwartClient>();
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
    public async Task EndToEnd_ExportThenImport_WorkflowSucceeds()
    {
        // Arrange - Setup mocks
        var testDomain = new Domain
        {
            Id = "d1",
            Name = "test.com",
            Description = "Test Domain",
            IsEnabled = true
        };

        var testAccounts = new List<Account>
        {
            new Account { Id = "a1", Name = "user1", Email = "user1@test.com", DisplayName = "User 1", IsEnabled = true }
        };

        _mockHMailServerClient.Setup(c => c.GetAccountsAsync("d1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(testAccounts);
        _mockHMailServerClient.Setup(c => c.GetAliasesByDomainAsync("d1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailAlias>());
        _mockHMailServerClient.Setup(c => c.GetMessagesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailMessage>());

        _mockStalwartClient.Setup(c => c.CreateDomainAsync(It.IsAny<Domain>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(testDomain);
        _mockStalwartClient.Setup(c => c.CreateAccountAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account a, CancellationToken ct) => a);

        // Act - Export
        var archiveManager = new ArchiveManager(_tempDir);
        var checkpointService = new CheckpointService(_tempDir);
        var exporter = new HMailServerExporter(_mockHMailServerClient.Object, _tempDir, archiveManager, checkpointService);
        
        var exportResult = await exporter.ExportDomainAsync(testDomain, CancellationToken.None);
        
        // Assert - Export succeeded
        Assert.IsTrue(exportResult.IsSuccess, "Export should succeed");
        
        // Act - Import
        var importer = new StalwartImporter(_mockStalwartClient.Object, _tempDir, archiveManager, checkpointService);
        var importResult = await importer.ImportDomainAsync(testDomain, CancellationToken.None);
        
        // Assert - Import succeeded
        Assert.IsTrue(importResult.IsSuccess, "Import should succeed");
    }

    [TestMethod]
    public async Task EndToEnd_SetupWorkflow_WithMultipleDomains()
    {
        // Arrange
        var testDomains = new List<Domain>
        {
            new Domain { Id = "d1", Name = "domain1.com", IsEnabled = true },
            new Domain { Id = "d2", Name = "domain2.com", IsEnabled = true }
        };

        var testAccounts1 = new List<Account>
        {
            new Account { Id = "a1", Name = "user1", Email = "user1@domain1.com", IsEnabled = true }
        };

        var testAccounts2 = new List<Account>
        {
            new Account { Id = "a2", Name = "user2", Email = "user2@domain2.com", IsEnabled = true }
        };

        _mockHMailServerClient.Setup(c => c.GetDomainsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(testDomains);

        _mockHMailServerClient.Setup(c => c.GetAccountsAsync("d1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(testAccounts1);
        _mockHMailServerClient.Setup(c => c.GetAccountsAsync("d2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(testAccounts2);

        _mockHMailServerClient.Setup(c => c.GetAliasesByDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailAlias>());

        _mockHMailServerClient.Setup(c => c.GetMessagesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailMessage>());

        _mockStalwartClient.Setup(c => c.AuthenticateAsync(It.IsAny<ApiCredentials>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockStalwartClient.Setup(c => c.CreateDomainAsync(It.IsAny<Domain>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain d, CancellationToken ct) => d);

        _mockStalwartClient.Setup(c => c.CreateAccountAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account a, CancellationToken ct) => a);

        var options = new MigrationOptions
        {
            StalwartUsername = "admin",
            StalwartPassword = "password"
        };

        var checkpointService = new CheckpointService(_tempDir);
        var archiveManager = new ArchiveManager(_tempDir);
        var exporter = new HMailServerExporter(_mockHMailServerClient.Object, _tempDir, archiveManager, checkpointService);
        var importer = new StalwartImporter(_mockStalwartClient.Object, _tempDir, archiveManager, checkpointService);

        var orchestrator = new MigrationOrchestrator(
            _mockHMailServerClient.Object,
            _mockStalwartClient.Object,
            checkpointService,
            archiveManager,
            exporter,
            importer);

        // Act
        var result = await orchestrator.SetupAsync(options, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success, "Setup workflow should succeed");
        Assert.AreEqual(2, result.DomainsProcessed, "Should have processed 2 domains");
        Assert.AreEqual(2, result.AccountsProcessed, "Should have processed 2 accounts");
    }

    [TestMethod]
    public async Task EndToEnd_ValidationWorkflow_WithExistingDomains()
    {
        // Arrange
        var testDomains = new List<Domain>
        {
            new Domain { Id = "d1", Name = "domain1.com", IsEnabled = true }
        };

        _mockHMailServerClient.Setup(c => c.GetDomainsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(testDomains);

        _mockHMailServerClient.Setup(c => c.GetAccountsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account>());

        _mockHMailServerClient.Setup(c => c.GetAliasesByDomainAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailAlias>());

        _mockStalwartClient.Setup(c => c.AuthenticateAsync(It.IsAny<ApiCredentials>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockStalwartClient.Setup(c => c.GetDomainAsync("domain1.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Domain { Name = "domain1.com", IsEnabled = true });

        var options = new MigrationOptions
        {
            StalwartUsername = "admin",
            StalwartPassword = "password"
        };

        var checkpointService = new CheckpointService(_tempDir);
        var archiveManager = new ArchiveManager(_tempDir);
        var exporter = new HMailServerExporter(_mockHMailServerClient.Object, _tempDir, archiveManager, checkpointService);
        var importer = new StalwartImporter(_mockStalwartClient.Object, _tempDir, archiveManager, checkpointService);

        var orchestrator = new MigrationOrchestrator(
            _mockHMailServerClient.Object,
            _mockStalwartClient.Object,
            checkpointService,
            archiveManager,
            exporter,
            importer);

        // Act
        var result = await orchestrator.ValidateAsync(options, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success, "Validation workflow should succeed");
        Assert.AreEqual(1, result.DomainsProcessed);
    }

    [TestMethod]
    public async Task EndToEnd_FullWorkflow_WithVandelayDisabled()
    {
        // Arrange
        var testDomains = new List<Domain>
        {
            new Domain { Id = "d1", Name = "domain1.com", IsEnabled = true }
        };

        _mockHMailServerClient.Setup(c => c.GetDomainsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(testDomains);

        _mockHMailServerClient.Setup(c => c.GetAccountsAsync("d1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account>());

        _mockHMailServerClient.Setup(c => c.GetAliasesByDomainAsync("d1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailAlias>());

        _mockHMailServerClient.Setup(c => c.GetMessagesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailMessage>());

        _mockStalwartClient.Setup(c => c.AuthenticateAsync(It.IsAny<ApiCredentials>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockStalwartClient.Setup(c => c.CreateDomainAsync(It.IsAny<Domain>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain d, CancellationToken ct) => d);

        _mockStalwartClient.Setup(c => c.GetDomainAsync("domain1.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Domain { Name = "domain1.com", IsEnabled = true });

        var options = new MigrationOptions
        {
            StalwartUsername = "admin",
            StalwartPassword = "password",
            UseVandelay = false,
            SkipMessages = true,
            SkipValidation = false
        };

        var checkpointService = new CheckpointService(_tempDir);
        var archiveManager = new ArchiveManager(_tempDir);
        var exporter = new HMailServerExporter(_mockHMailServerClient.Object, _tempDir, archiveManager, checkpointService);
        var importer = new StalwartImporter(_mockStalwartClient.Object, _tempDir, archiveManager, checkpointService);

        var orchestrator = new MigrationOrchestrator(
            _mockHMailServerClient.Object,
            _mockStalwartClient.Object,
            checkpointService,
            archiveManager,
            exporter,
            importer);

        // Act
        var result = await orchestrator.MigrateAsync(options, CancellationToken.None);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Success, "Full workflow should succeed");
        Assert.AreEqual(1, result.DomainsProcessed);
    }

    [TestMethod]
    public async Task EndToEnd_ResumeFromCheckpoint_Workflow()
    {
        // This test verifies that checkpoint creation and loading works
        // which is a prerequisite for resume functionality
        
        // Arrange
        var checkpointService = new CheckpointService(_tempDir);
        var checkpointName = "test_migration_resume";
        var state = new Dictionary<string, object>
        {
            { "phase", "migration" },
            { "domainsProcessed", 3 },
            { "accountsProcessed", 50 },
            { "timestamp", DateTime.UtcNow }
        };

        // Act - Create checkpoint (simulating a partial migration)
        var checkpointPath = await checkpointService.CreateCheckpointAsync(checkpointName, state, CancellationToken.None);
        
        // Assert - Checkpoint was created
        Assert.IsNotNull(checkpointPath);
        Assert.IsTrue(File.Exists(checkpointPath), "Checkpoint file should exist");
        
        // Act - Load checkpoint (simulating resume)
        var loadedState = await checkpointService.LoadCheckpointAsync(checkpointPath, CancellationToken.None);
        
        // Assert - State was loaded correctly
        Assert.IsNotNull(loadedState);
        Assert.AreEqual("migration", loadedState["phase"] as string);
        Assert.AreEqual(3, loadedState["domainsProcessed"]);
        Assert.AreEqual(50, loadedState["accountsProcessed"]);
    }

    [TestMethod]
    public async Task EndToEnd_ExportImportFallback_Workflow()
    {
        // Arrange
        var testDomain = new Domain
        {
            Id = "d1",
            Name = "fallback.com",
            Description = "Test Domain for fallback",
            IsEnabled = true
        };

        var testAccounts = new List<Account>
        {
            new Account { Id = "a1", Name = "user1", Email = "user1@fallback.com", IsEnabled = true }
        };

        _mockHMailServerClient.Setup(c => c.GetAccountsAsync("d1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(testAccounts);
        _mockHMailServerClient.Setup(c => c.GetAliasesByDomainAsync("d1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailAlias>());
        _mockHMailServerClient.Setup(c => c.GetMessagesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailMessage>());

        _mockStalwartClient.Setup(c => c.CreateDomainAsync(It.IsAny<Domain>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(testDomain);
        _mockStalwartClient.Setup(c => c.CreateAccountAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account a, CancellationToken ct) => a);

        var archiveManager = new ArchiveManager(_tempDir);
        var checkpointService = new CheckpointService(_tempDir);
        var exporter = new HMailServerExporter(_mockHMailServerClient.Object, _tempDir, archiveManager, checkpointService);
        
        // Act - Export
        var exportResult = await exporter.ExportDomainAsync(testDomain, CancellationToken.None);
        
        // Assert - Export succeeded
        Assert.IsTrue(exportResult.IsSuccess, "Export should succeed");
        
        // Act - Import
        var importer = new StalwartImporter(_mockStalwartClient.Object, _tempDir, archiveManager, checkpointService);
        var importResult = await importer.ImportDomainAsync(testDomain, CancellationToken.None);
        
        // Assert - Import succeeded
        Assert.IsTrue(importResult.IsSuccess, "Import should succeed");
    }
}
