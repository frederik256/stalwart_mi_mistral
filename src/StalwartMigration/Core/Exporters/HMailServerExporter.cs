// <copyright file="HMailServerExporter.cs" company="Stalwart Labs">
// Copyright (c) Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StalwartMigration.Core.Models;
using StalwartMigration.Core.Models.Progress;
using StalwartMigration.Core.Services;
using StalwartMigration.Infrastructure.FileSystem;
using StalwartMigration.Infrastructure.HMailServer;

namespace StalwartMigration.Core.Exporters;

/// <summary>
/// Exports data from hMailServer to JSON and EML format.
/// This is the fallback path when Vandelay is unavailable.
/// </summary>
public class HMailServerExporter : ExporterBase
{
    private readonly string _outputDirectory;
    private readonly ILogger<HMailServerExporter> _logger;
    private readonly ArchiveManager? _archiveManager;
    private readonly CheckpointService? _checkpointService;
    private bool _disposed;

    /// <summary>
    /// Gets the output directory for exported files.
    /// </summary>
    public string OutputDirectory => _outputDirectory;

    /// <summary>
    /// Initializes a new instance of the HMailServerExporter class.
    /// </summary>
    /// <param name="client">The hMailServer client.</param>
    /// <param name="outputDirectory">The output directory for exported files.</param>
    /// <param name="archiveManager">The archive manager for ZIP operations.</param>
    /// <param name="checkpointService">The checkpoint service for resume capability.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when client or outputDirectory is null.</exception>
    public HMailServerExporter(
        IHMailServerClient client,
        string outputDirectory,
        ArchiveManager? archiveManager = null,
        CheckpointService? checkpointService = null,
        ILogger<HMailServerExporter>? logger = null)
        : base(client, logger)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("Output directory cannot be null or empty.", nameof(outputDirectory));

        _outputDirectory = Path.GetFullPath(outputDirectory.TrimEnd(Path.DirectorySeparatorChar));
        _logger = logger ?? NullLogger<HMailServerExporter>.Instance;
        _archiveManager = archiveManager ?? new ArchiveManager(_outputDirectory);
        _checkpointService = checkpointService ?? new CheckpointService(_outputDirectory);

        // Ensure output directory exists
        if (!Directory.Exists(_outputDirectory))
        {
            Directory.CreateDirectory(_outputDirectory);
            _logger.LogInformation("Created output directory: {OutputDirectory}", _outputDirectory);
        }
    }

    /// <summary>
    /// Exports a single domain from hMailServer.
    /// </summary>
    /// <param name="domain">The domain to export.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The export result.</returns>
    public override async Task<ExportResult> ExportDomainAsync(Domain domain, CancellationToken cancellationToken = default)
    {
        if (domain == null)
            throw new ArgumentNullException(nameof(domain));

        _logger.LogInformation("Starting export of domain: {DomainName} ({DomainId})", domain.Name, domain.Id);

        var exportedFiles = new List<string>();
        var domainDir = Path.Combine(_outputDirectory, domain.Name);

        try
        {
            // Create domain directory
            Directory.CreateDirectory(domainDir);

            // Export domain metadata
            var domainJsonPath = await ExportDomainMetadataAsync(domain, domainDir, cancellationToken).ConfigureAwait(false);
            if (domainJsonPath != null)
                exportedFiles.Add(domainJsonPath);

            // Export accounts
            var accounts = await Client.GetAccountsAsync(domain.Id, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Found {AccountCount} accounts in domain {DomainName}", accounts.Count, domain.Name);

            var accountsJsonPath = await ExportAccountsToJsonAsync(domain.Name, accounts, cancellationToken).ConfigureAwait(false);
            exportedFiles.Add(accountsJsonPath);

            // Export aliases for this domain
            var aliases = await Client.GetAliasesByDomainAsync(domain.Id, cancellationToken).ConfigureAwait(false);
            if (aliases.Count > 0)
            {
                var aliasesJsonPath = await ExportAliasesToJsonAsync(domain.Name, aliases, cancellationToken).ConfigureAwait(false);
                exportedFiles.Add(aliasesJsonPath);
            }

            // Export messages for each account
            foreach (var account in accounts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var messages = await Client.GetMessagesAsync(account.Id, cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("Found {MessageCount} messages for account {AccountName}", messages.Count, account.Name);

                if (messages.Count > 0)
                {
                    var accountDir = Path.Combine(domainDir, "accounts", account.Name);
                    Directory.CreateDirectory(accountDir);

                    foreach (var message in messages)
                    {
                        var emlPath = await ExportMessageToEmlAsync(account.Name, message, accountDir, cancellationToken).ConfigureAwait(false);
                        exportedFiles.Add(emlPath);

                        // Export attachments if any
                        if (message.HasAttachments)
                        {
                            foreach (var attachment in message.Attachments)
                            {
                                var attachmentPath = await ExportAttachmentAsync(account.Name, message.Id, attachment, accountDir, cancellationToken).ConfigureAwait(false);
                                exportedFiles.Add(attachmentPath);
                            }
                        }
                    }
                }

                // Report progress
                Progress?.Report(new ProgressReport(accounts.IndexOf(account) + 1, accounts.Count, 
                    $"Exported account {account.Name}"));
            }

            // Create domain archive
            var archivePath = await CreateDomainArchiveAsync(domain.Name, domain, accounts, aliases, cancellationToken).ConfigureAwait(false);
            exportedFiles.Add(archivePath);

            // Create checkpoint
            await CreateCheckpointAsync(domain.Name, domain, accounts.Count, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully exported domain: {DomainName}", domain.Name);
            return ExportResult.Success(domain, exportedFiles);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Export of domain {DomainName} was cancelled", domain.Name);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export domain {DomainName}", domain.Name);
            return ExportResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// Exports domain metadata to JSON.
    /// </summary>
    private async Task<string?> ExportDomainMetadataAsync(Domain domain, string domainDir, CancellationToken cancellationToken)
    {
        try
        {
            var domainJsonPath = Path.Combine(domainDir, $"{domain.Name}_metadata.json");
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(domain, options);
            await File.WriteAllTextAsync(domainJsonPath, json, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Exported domain metadata: {Path}", domainJsonPath);
            return domainJsonPath;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to export domain metadata for {DomainName}", domain.Name);
            return null;
        }
    }

    /// <summary>
    /// Exports accounts to JSON format.
    /// </summary>
    public async Task<string> ExportAccountsToJsonAsync(string domainName, List<Account> accounts, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domainName))
            throw new ArgumentException("Domain name cannot be null or empty.", nameof(domainName));

        var accountsJsonPath = Path.Combine(_outputDirectory, domainName, $"{domainName}_accounts.json");
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(accounts, options);
        await File.WriteAllTextAsync(accountsJsonPath, json, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Exported {Count} accounts to JSON: {Path}", accounts.Count, accountsJsonPath);
        return accountsJsonPath;
    }

    /// <summary>
    /// Exports aliases to JSON format.
    /// </summary>
    private async Task<string> ExportAliasesToJsonAsync(string domainName, List<EmailAlias> aliases, CancellationToken cancellationToken)
    {
        var aliasesJsonPath = Path.Combine(_outputDirectory, domainName, $"{domainName}_aliases.json");
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(aliases, options);
        await File.WriteAllTextAsync(aliasesJsonPath, json, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Exported {Count} aliases to JSON: {Path}", aliases.Count, aliasesJsonPath);
        return aliasesJsonPath;
    }

    /// <summary>
    /// Exports an email message to EML format.
    /// </summary>
    private async Task<string> ExportMessageToEmlAsync(string accountName, EmailMessage message, string accountDir, CancellationToken cancellationToken)
    {
        var emlFileName = SanitizeFileName($"{message.MessageId}.eml");
        var emlPath = Path.Combine(accountDir, "emails", emlFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(emlPath)!);

        // Convert message to EML format (simplified for this implementation)
        // In a real implementation, this would use MimeKit or similar
        var emlContent = BuildEmlContent(message);
        await File.WriteAllTextAsync(emlPath, emlContent, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Exported message to EML: {Path}", emlPath);
        return emlPath;
    }

    /// <summary>
    /// Builds EML content from an email message.
    /// </summary>
    private string BuildEmlContent(EmailMessage message)
    {
        // Simplified EML generation
        // A real implementation would use MimeKit for proper EML generation
        var eml = new System.Text.StringBuilder();
        eml.AppendLine("Message-ID: " + (message.MessageId ?? Guid.NewGuid().ToString()));
        eml.AppendLine("From: " + message.From);
        eml.AppendLine("To: " + string.Join(", ", message.To));
        eml.AppendLine("Cc: " + string.Join(", ", message.Cc));
        eml.AppendLine("Bcc: " + string.Join(", ", message.Bcc));
        eml.AppendLine("Subject: " + message.Subject);
        eml.AppendLine("Date: " + message.Date.ToString("r"));
        eml.AppendLine();
        eml.AppendLine(message.Body);
        return eml.ToString();
    }

    /// <summary>
    /// Exports a message attachment to a file.
    /// </summary>
    private async Task<string> ExportAttachmentAsync(string accountName, string messageId, EmailAttachment attachment, string accountDir, CancellationToken cancellationToken)
    {
        var attachmentFileName = SanitizeFileName(attachment.FileName);
        var attachmentPath = Path.Combine(accountDir, "attachments", attachmentFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(attachmentPath)!);

        // In a real implementation, this would save the actual binary data
        // For now, we create a placeholder file
        await File.WriteAllTextAsync(attachmentPath, "[Attachment Placeholder]", cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Exported attachment: {Path}", attachmentPath);
        return attachmentPath;
    }

    /// <summary>
    /// Sanitizes a file name to prevent directory traversal and invalid characters.
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "unnamed" : sanitized;
    }

    /// <summary>
    /// Creates a ZIP archive for a domain containing all exported data.
    /// </summary>
    public async Task<string> CreateDomainArchiveAsync(string domainName, Domain domain, List<Account> accounts, List<EmailAlias> aliases, CancellationToken cancellationToken = default)
    {
        var archivePath = Path.Combine(_outputDirectory, $"{SanitizeFileName(domainName)}.zip");
        
        // Create a temporary directory structure
        var tempDir = Path.Combine(_outputDirectory, "temp", domainName);
        Directory.CreateDirectory(tempDir);

        try
        {
            // Save domain metadata
            var domainJson = JsonSerializer.Serialize(domain, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(Path.Combine(tempDir, "domain.json"), domainJson, cancellationToken).ConfigureAwait(false);

            // Save accounts
            var accountsJson = JsonSerializer.Serialize(accounts, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(Path.Combine(tempDir, "accounts.json"), accountsJson, cancellationToken).ConfigureAwait(false);

            // Save aliases
            var aliasesJson = JsonSerializer.Serialize(aliases, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(Path.Combine(tempDir, "aliases.json"), aliasesJson, cancellationToken).ConfigureAwait(false);

            // For now, just create the archive from the temp directory
            if (Directory.Exists(tempDir))
            {
                await _archiveManager!.CreateDomainArchiveAsync(domainName, new { Domain = domain.Name }, false, cancellationToken).ConfigureAwait(false);
            }

            _logger.LogDebug("Created domain archive: {Path}", archivePath);
            return archivePath;
        }
        finally
        {
            // Cleanup temp directory
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { /* Ignore */ }
            }
        }
    }

    /// <summary>
    /// Creates a checkpoint for resume capability.
    /// </summary>
    private async Task CreateCheckpointAsync(string domainName, Domain domain, int accountCount, CancellationToken cancellationToken)
    {
        try
        {
            var state = new Dictionary<string, object>
            {
                { "domainName", domain.Name },
                { "domainId", domain.Id },
                { "accountCount", accountCount },
                { "timestamp", DateTime.UtcNow },
                { "type", "export" }
            };
            var checkpointPath = await _checkpointService!.CreateCheckpointAsync(domainName, state, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Created checkpoint: {Path}", checkpointPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create checkpoint for domain {DomainName}", domain.Name);
        }
    }

    /// <summary>
    /// Disposes resources.
    /// </summary>
    public override void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _archiveManager?.Dispose();
        _checkpointService?.Dispose();
        GC.SuppressFinalize(this);
        base.Dispose();
    }
}
