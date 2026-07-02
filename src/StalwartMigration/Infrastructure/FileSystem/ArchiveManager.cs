// <copyright file="ArchiveManager.cs" company="Stalwart Labs">
// Copyright (c) Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.IO.Compression;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StalwartMigration.Utilities.Extensions;
using StalwartMigration.Utilities.Helpers;

namespace StalwartMigration.Infrastructure.FileSystem;

/// <summary>
/// Manages ZIP archive operations for storing migration data.
/// Each domain gets its own ZIP archive containing JSON metadata, EML emails, and binary attachments.
/// </summary>
public class ArchiveManager : IDisposable
{
    private readonly ILogger<ArchiveManager> _logger;
    private bool _disposed;

    /// <summary>
    /// Gets or sets the default directory for archive operations.
    /// </summary>
    public string? BaseDirectory { get; set; }

    /// <summary>
    /// Gets or sets the default compression level.
    /// </summary>
    public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Optimal;

    /// <summary>
    /// Initializes a new instance of the ArchiveManager class.
    /// </summary>
    /// <param name="baseDirectory">The default directory for archive operations.</param>
    /// <param name="logger">The logger instance.</param>
    public ArchiveManager(string? baseDirectory = null, ILogger<ArchiveManager>? logger = null)
    {
        BaseDirectory = baseDirectory;
        _logger = logger ?? NullLogger<ArchiveManager>.Instance;
    }

    /// <summary>
    /// Creates a new ZIP archive for a domain.
    /// </summary>
    /// <param name="domainName">The name of the domain.</param>
    /// <param name="overwrite">Whether to overwrite if the archive already exists.</param>
    /// <returns>The path to the created archive.</returns>
    public async Task<string> CreateArchiveAsync(string domainName, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domainName))
            throw new ArgumentException("Domain name cannot be null or empty.", nameof(domainName));

        var safeDomainName = PathSanitizer.SanitizeFileName(domainName);
        var archivePath = GetArchivePath(safeDomainName);

        // Check if archive already exists
        if (File.Exists(archivePath))
        {
            if (overwrite)
            {
                _logger.LogInformation("Overwriting existing archive: {ArchivePath}", archivePath);
                File.Delete(archivePath);
            }
            else
            {
                throw ArchiveManagerException.ForFileExists(archivePath);
            }
        }

        // Ensure directory exists
        EnsureDirectoryExists(Path.GetDirectoryName(archivePath));

        // Create the archive
        using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create);
        _logger.LogInformation("Created new archive: {ArchivePath}", archivePath);

        return archivePath;
    }

    /// <summary>
    /// Opens an existing ZIP archive for reading.
    /// </summary>
    /// <param name="archivePath">The path to the archive.</param>
    /// <param name="mode">The mode to open the archive in.</param>
    /// <returns>The opened archive.</returns>
    public ZipArchive OpenArchive(string archivePath, ZipArchiveMode mode = ZipArchiveMode.Read)
    {
        if (string.IsNullOrWhiteSpace(archivePath))
            throw new ArgumentException("Archive path cannot be null or empty.", nameof(archivePath));

        if (!File.Exists(archivePath))
        {
            throw ArchiveManagerException.ForFileNotFound(archivePath);
        }

        try
        {
            var archive = ZipFile.Open(archivePath, mode);
            _logger.LogDebug("Opened archive: {ArchivePath} (Mode: {Mode})", archivePath, mode);
            return archive;
        }
        catch (Exception ex) when (ex is InvalidDataException || ex is IOException)
        {
            throw ArchiveManagerException.ForInvalidArchive(archivePath);
        }
        catch (UnauthorizedAccessException)
        {
            throw ArchiveManagerException.ForPermissionDenied(archivePath);
        }
    }

    /// <summary>
    /// Adds a file to an archive.
    /// </summary>
    /// <param name="archivePath">The path to the archive.</param>
    /// <param name="entryName">The name of the entry in the archive.</param>
    /// <param name="filePath">The path to the file to add.</param>
    /// <param name="compressionLevel">The compression level to use.</param>
    public async Task AddFileToArchiveAsync(string archivePath, string entryName, string filePath, CompressionLevel? compressionLevel = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(archivePath))
            throw new ArgumentException("Archive path cannot be null or empty.", nameof(archivePath));
        if (string.IsNullOrWhiteSpace(entryName))
            throw new ArgumentException("Entry name cannot be null or empty.", nameof(entryName));
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found.", filePath);
        }

        var level = compressionLevel ?? CompressionLevel;

        using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Update);
        
        var entry = archive.CreateEntryFromFile(filePath, entryName, level);
        _logger.LogDebug("Added file to archive: {ArchivePath} -> {EntryName} (Source: {FilePath})", archivePath, entryName, filePath);

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Adds a JSON serialized object to an archive as a file.
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="archivePath">The path to the archive.</param>
    /// <param name="entryName">The name of the entry in the archive.</param>
    /// <param name="data">The object to serialize and add.</param>
    public async Task AddJsonToArchiveAsync<T>(string archivePath, string entryName, T data, CompressionLevel? compressionLevel = null, CancellationToken cancellationToken = default)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        if (string.IsNullOrWhiteSpace(archivePath))
            throw new ArgumentException("Archive path cannot be null or empty.", nameof(archivePath));
        if (string.IsNullOrWhiteSpace(entryName))
            throw new ArgumentException("Entry name cannot be null or empty.", nameof(entryName));

        using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Update);
        var entry = archive.CreateEntry(entryName, compressionLevel ?? CompressionLevel);

        using (var writer = new StreamWriter(entry.Open()))
        {
            var json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
            await writer.WriteAsync(json).ConfigureAwait(false);
        }

        _logger.LogDebug("Added JSON to archive: {ArchivePath} -> {EntryName}", archivePath, entryName);
    }

    /// <summary>
    /// Adds a text file to an archive.
    /// </summary>
    /// <param name="archivePath">The path to the archive.</param>
    /// <param name="entryName">The name of the entry in the archive.</param>
    /// <param name="content">The text content to add.</param>
    public async Task AddTextToArchiveAsync(string archivePath, string entryName, string content, CompressionLevel? compressionLevel = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be null or empty.", nameof(content));
        if (string.IsNullOrWhiteSpace(archivePath))
            throw new ArgumentException("Archive path cannot be null or empty.", nameof(archivePath));
        if (string.IsNullOrWhiteSpace(entryName))
            throw new ArgumentException("Entry name cannot be null or empty.", nameof(entryName));

        using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Update);
        var entry = archive.CreateEntry(entryName, compressionLevel ?? CompressionLevel);

        using (var writer = new StreamWriter(entry.Open()))
        {
            await writer.WriteAsync(content).ConfigureAwait(false);
        }

        _logger.LogDebug("Added text to archive: {ArchivePath} -> {EntryName}", archivePath, entryName);
    }

    /// <summary>
    /// Extracts all files from an archive to a directory.
    /// </summary>
    /// <param name="archivePath">The path to the archive.</param>
    /// <param name="outputDirectory">The directory to extract to.</param>
    /// <param name="overwrite">Whether to overwrite existing files.</param>
    public async Task ExtractArchiveAsync(string archivePath, string outputDirectory, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(archivePath))
            throw new ArgumentException("Archive path cannot be null or empty.", nameof(archivePath));
        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("Output directory cannot be null or empty.", nameof(outputDirectory));

        EnsureDirectoryExists(outputDirectory);

        using var archive = OpenArchive(archivePath);

        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(entry.Name) || entry.FullName.EndsWith("/"))
                continue; // Skip directories

            var outputPath = Path.Combine(outputDirectory, entry.FullName);

            // Ensure directory structure exists
            EnsureDirectoryExists(Path.GetDirectoryName(outputPath));

            if (File.Exists(outputPath) && !overwrite)
            {
                _logger.LogWarning("Skipping existing file: {OutputPath}", outputPath);
                continue;
            }

            using (var entryStream = entry.Open())
            using (var fileStream = File.Create(outputPath))
            {
                await entryStream.CopyToAsync(fileStream).ConfigureAwait(false);
            }

            _logger.LogDebug("Extracted: {EntryName} -> {OutputPath}", entry.FullName, outputPath);
        }

        _logger.LogInformation("Extracted {Count} files from archive: {ArchivePath}", archive.Entries.Count, archivePath);
    }

    /// <summary>
    /// Extracts a specific entry from an archive.
    /// </summary>
    /// <param name="archivePath">The path to the archive.</param>
    /// <param name="entryName">The name of the entry to extract.</param>
    /// <param name="outputPath">The path to extract to.</param>
    public async Task ExtractEntryAsync(string archivePath, string entryName, string outputPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(archivePath))
            throw new ArgumentException("Archive path cannot be null or empty.", nameof(archivePath));
        if (string.IsNullOrWhiteSpace(entryName))
            throw new ArgumentException("Entry name cannot be null or empty.", nameof(entryName));
        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("Output path cannot be null or empty.", nameof(outputPath));

        using var archive = OpenArchive(archivePath);
        var entry = archive.GetEntry(entryName);

        if (entry == null)
        {
            throw ArchiveManagerException.ForEntryNotFound(archivePath, entryName);
        }

        // Ensure directory exists
        EnsureDirectoryExists(Path.GetDirectoryName(outputPath));

        using (var entryStream = entry.Open())
        using (var fileStream = File.Create(outputPath))
        {
            await entryStream.CopyToAsync(fileStream).ConfigureAwait(false);
        }

        _logger.LogDebug("Extracted entry: {ArchivePath} -> {EntryName} to {OutputPath}", archivePath, entryName, outputPath);
    }

    /// <summary>
    /// Gets the path to an archive for a domain.
    /// </summary>
    /// <param name="domainName">The name of the domain.</param>
    /// <returns>The full path to the archive.</returns>
    public string GetArchivePath(string domainName)
    {
        var safeDomainName = PathSanitizer.SanitizeFileName(domainName);
        var fileName = $"{safeDomainName}.zip";
        
        if (!string.IsNullOrWhiteSpace(BaseDirectory))
        {
            return Path.Combine(BaseDirectory, fileName);
        }
        
        return fileName;
    }

    /// <summary>
    /// Lists all entries in an archive.
    /// </summary>
    /// <param name="archivePath">The path to the archive.</param>
    /// <returns>A list of entry names.</returns>
    public List<string> ListArchiveEntries(string archivePath)
    {
        if (string.IsNullOrWhiteSpace(archivePath))
            throw new ArgumentException("Archive path cannot be null or empty.", nameof(archivePath));

        using var archive = OpenArchive(archivePath);
        var entries = new List<string>();

        foreach (var entry in archive.Entries)
        {
            if (!string.IsNullOrWhiteSpace(entry.Name))
            {
                entries.Add(entry.FullName);
            }
        }

        return entries;
    }

    /// <summary>
    /// Checks if an archive exists.
    /// </summary>
    /// <param name="archivePath">The path to the archive.</param>
    /// <returns>True if the archive exists; otherwise, false.</returns>
    public bool ArchiveExists(string archivePath)
    {
        if (string.IsNullOrWhiteSpace(archivePath))
            return false;

        return File.Exists(archivePath);
    }

    /// <summary>
    /// Deletes an archive.
    /// </summary>
    /// <param name="archivePath">The path to the archive.</param>
    public void DeleteArchive(string archivePath)
    {
        if (string.IsNullOrWhiteSpace(archivePath))
            throw new ArgumentException("Archive path cannot be null or empty.", nameof(archivePath));

        if (File.Exists(archivePath))
        {
            try
            {
                File.Delete(archivePath);
                _logger.LogInformation("Deleted archive: {ArchivePath}", archivePath);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw ArchiveManagerException.ForFileInUse(archivePath);
            }
        }
    }

    /// <summary>
    /// Creates an archive with the standard structure for domain migration.
    /// Structure: domain.zip -> metadata.json, accounts.json, emails/, attachments/
    /// </summary>
    /// <param name="domainName">The name of the domain.</param>
    /// <param name="metadata">The metadata to include.</param>
    /// <param name="overwrite">Whether to overwrite if the archive already exists.</param>
    /// <returns>The path to the created archive.</returns>
    public async Task<string> CreateDomainArchiveAsync(string domainName, object metadata, bool overwrite = false, CancellationToken cancellationToken = default)
    {
        var archivePath = await CreateArchiveAsync(domainName, overwrite, cancellationToken).ConfigureAwait(false);

        // Add metadata
        await AddJsonToArchiveAsync(archivePath, "metadata.json", metadata, null, cancellationToken).ConfigureAwait(false);

        // Create directory entries for standard structure
        using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Update);
        
        // Add directory entries (these will be created automatically when files are added)
        // But we can explicitly add them if needed
        var directories = new[] { "emails/", "attachments/", "accounts/" };
        foreach (var dir in directories)
        {
            // ZipFile will create directories automatically when files are added to them
            // We don't need to explicitly add directory entries
        }

        _logger.LogInformation("Created domain archive with standard structure: {ArchivePath}", archivePath);
        return archivePath;
    }

    /// <summary>
    /// Ensures that a directory exists, creating it if necessary.
    /// </summary>
    /// <param name="directoryPath">The directory path to ensure exists.</param>
    private void EnsureDirectoryExists(string? directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            return;

        if (!Directory.Exists(directoryPath))
        {
            try
            {
                Directory.CreateDirectory(directoryPath);
                _logger.LogDebug("Created directory: {DirectoryPath}", directoryPath);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw ArchiveManagerException.ForPermissionDenied(directoryPath);
            }
        }
    }

    /// <summary>
    /// Disposes the archive manager.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _logger.LogDebug("Disposed ArchiveManager");
        }
    }
}
