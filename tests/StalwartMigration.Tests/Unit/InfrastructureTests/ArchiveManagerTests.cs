// <copyright file="ArchiveManagerTests.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StalwartMigration.Infrastructure.FileSystem;

namespace StalwartMigration.Tests.Unit.InfrastructureTests;

[TestClass]
public class ArchiveManagerTests
{
    private ILogger<ArchiveManager> _logger = NullLogger<ArchiveManager>.Instance;
    private string _testDir = Path.Combine(Path.GetTempPath(), "ArchiveManagerTests");

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
    public void Constructor_WithValidDirectory_CreatesManager()
    {
        var manager = new ArchiveManager(_testDir, _logger);
        Assert.IsNotNull(manager);
        Assert.AreEqual(_testDir, manager.BaseDirectory);
    }

    [TestMethod]
    public void Constructor_WithNullDirectory_CreatesManagerWithNullBase()
    {
        var manager = new ArchiveManager(null, _logger);
        Assert.IsNotNull(manager);
        Assert.IsNull(manager.BaseDirectory);
    }

    [TestMethod]
    public void Constructor_WithEmptyDirectory_CreatesManagerWithEmptyBase()
    {
        var manager = new ArchiveManager(string.Empty, _logger);
        Assert.IsNotNull(manager);
        Assert.AreEqual(string.Empty, manager.BaseDirectory);
    }

    [TestMethod]
    public async Task CreateArchiveAsync_WithValidDomainName_CreatesArchive()
    {
        var manager = new ArchiveManager(_testDir, _logger);
        string domainName = "test-domain.com";
        string archivePath = await manager.CreateArchiveAsync(domainName);
        Assert.IsTrue(File.Exists(archivePath));
        Assert.IsTrue(archivePath.Contains("test-domain.com"));
        Assert.IsTrue(archivePath.EndsWith(".zip"));
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task CreateArchiveAsync_WithNullDomainName_ThrowsArgumentException()
    {
        var manager = new ArchiveManager(_testDir, _logger);
        await manager.CreateArchiveAsync(null!);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task CreateArchiveAsync_WithEmptyDomainName_ThrowsArgumentException()
    {
        var manager = new ArchiveManager(_testDir, _logger);
        await manager.CreateArchiveAsync(string.Empty);
    }

    [TestMethod]
    [ExpectedException(typeof(ArchiveManagerException))]
    public async Task CreateArchiveAsync_WithExistingArchive_ThrowsFileExistsException()
    {
        var manager = new ArchiveManager(_testDir, _logger);
        string domainName = "test-existing.com";
        string archivePath = await manager.CreateArchiveAsync(domainName);
        await manager.CreateArchiveAsync(domainName);
    }

    [TestMethod]
    public async Task CreateArchiveAsync_WithOverwriteTrue_OverwritesExisting()
    {
        var manager = new ArchiveManager(_testDir, _logger);
        string domainName = "test-overwrite.com";
        string archivePath1 = await manager.CreateArchiveAsync(domainName);
        string archivePath2 = await manager.CreateArchiveAsync(domainName, overwrite: true);
        Assert.AreEqual(archivePath1, archivePath2);
        Assert.IsTrue(File.Exists(archivePath2));
    }

    [TestMethod]
    public void GetArchivePath_WithDomainName_ReturnsCorrectPath()
    {
        var manager = new ArchiveManager(_testDir, _logger);
        string domainName = "test-path.com";
        string path = manager.GetArchivePath(domainName);
        Assert.IsTrue(path.Contains("test-path.com"));
        Assert.IsTrue(path.Contains(_testDir));
        Assert.IsTrue(path.EndsWith(".zip"));
    }

    [TestMethod]
    public void ArchiveExists_WithExistingArchive_ReturnsTrue()
    {
        var manager = new ArchiveManager(_testDir, _logger);
        string archivePath = Path.Combine(_testDir, "test.zip");
        File.WriteAllText(archivePath, "test");
        bool exists = manager.ArchiveExists(archivePath);
        Assert.IsTrue(exists);
    }

    [TestMethod]
    public void ArchiveExists_WithNonExistingArchive_ReturnsFalse()
    {
        var manager = new ArchiveManager(_testDir, _logger);
        bool exists = manager.ArchiveExists(Path.Combine(_testDir, "non-existing.zip"));
        Assert.IsFalse(exists);
    }

    [TestMethod]
    public void ArchiveExists_WithNullPath_ReturnsFalse()
    {
        var manager = new ArchiveManager(_testDir, _logger);
        bool exists = manager.ArchiveExists(null!);
        Assert.IsFalse(exists);
    }

    [TestMethod]
    public void DeleteArchive_WithExistingArchive_DeletesArchive()
    {
        var manager = new ArchiveManager(_testDir, _logger);
        string archivePath = Path.Combine(_testDir, "test-delete.zip");
        File.WriteAllText(archivePath, "test");
        manager.DeleteArchive(archivePath);
        Assert.IsFalse(File.Exists(archivePath));
    }

    [TestMethod]
    public void DeleteArchive_WithNonExistingArchive_DoesNothing()
    {
        var manager = new ArchiveManager(_testDir, _logger);
        string archivePath = Path.Combine(_testDir, "non-existing.zip");
        manager.DeleteArchive(archivePath); // Should not throw
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void DeleteArchive_WithNullPath_ThrowsArgumentException()
    {
        var manager = new ArchiveManager(_testDir, _logger);
        manager.DeleteArchive(null!);
    }

    [TestMethod]
    public void ListArchiveEntries_WithArchive_ReturnsEntries()
    {
        var manager = new ArchiveManager(_testDir, _logger);
        string archivePath = Path.Combine(_testDir, "test-entries.zip");
        
        using (var archive = System.IO.Compression.ZipFile.Open(archivePath, System.IO.Compression.ZipArchiveMode.Create))
        {
            var entry1 = archive.CreateEntry("file1.txt");
            var entry2 = archive.CreateEntry("dir/file2.txt");
        }

        var entries = manager.ListArchiveEntries(archivePath);
        Assert.IsNotNull(entries);
        Assert.AreEqual(2, entries.Count);
        Assert.IsTrue(entries.Contains("file1.txt"));
        Assert.IsTrue(entries.Contains("dir/file2.txt"));
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ListArchiveEntries_WithNullPath_ThrowsArgumentException()
    {
        var manager = new ArchiveManager(_testDir, _logger);
        manager.ListArchiveEntries(null!);
    }

    [TestMethod]
    [ExpectedException(typeof(ArchiveManagerException))]
    public void ListArchiveEntries_WithNonExistingArchive_ThrowsFileNotFoundException()
    {
        var manager = new ArchiveManager(_testDir, _logger);
        manager.ListArchiveEntries(Path.Combine(_testDir, "non-existing.zip"));
    }

    [TestMethod]
    public async Task CreateDomainArchiveAsync_WithValidData_CreatesArchiveWithMetadata()
    {
        var manager = new ArchiveManager(_testDir, _logger);
        string domainName = "test-domain-archive.com";
        var metadata = new { Domain = domainName, Version = "1.0" };
        string archivePath = await manager.CreateDomainArchiveAsync(domainName, metadata);
        Assert.IsTrue(File.Exists(archivePath));
        
        using (var archive = System.IO.Compression.ZipFile.OpenRead(archivePath))
        {
            var entry = archive.GetEntry("metadata.json");
            Assert.IsNotNull(entry);
        }
    }

    [TestMethod]
    public void CompressionLevel_Default_IsOptimal()
    {
        var manager = new ArchiveManager(_testDir, _logger);
        Assert.AreEqual(System.IO.Compression.CompressionLevel.Optimal, manager.CompressionLevel);
    }

    [TestMethod]
    public void CompressionLevel_CanBeChanged()
    {
        var manager = new ArchiveManager(_testDir, _logger);
        manager.CompressionLevel = System.IO.Compression.CompressionLevel.Fastest;
        Assert.AreEqual(System.IO.Compression.CompressionLevel.Fastest, manager.CompressionLevel);
    }

    [TestMethod]
    public async Task OpenArchive_WithValidArchive_ReturnsArchive()
    {
        var manager = new ArchiveManager(_testDir, _logger);
        string domainName = "test-open.com";
        string archivePath = await manager.CreateArchiveAsync(domainName);
        using var archive = manager.OpenArchive(archivePath);
        Assert.IsNotNull(archive);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void OpenArchive_WithNullPath_ThrowsArgumentException()
    {
        var manager = new ArchiveManager(_testDir, _logger);
        manager.OpenArchive(null!);
    }

    [TestMethod]
    [ExpectedException(typeof(ArchiveManagerException))]
    public void OpenArchive_WithNonExistingArchive_ThrowsFileNotFoundException()
    {
        var manager = new ArchiveManager(_testDir, _logger);
        manager.OpenArchive(Path.Combine(_testDir, "non-existing.zip"));
    }
}
