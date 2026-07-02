// <copyright file="StalwartImporter.cs" company="Stalwart Labs">
// Copyright (c) Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StalwartMigration.Core.Models;
using StalwartMigration.Core.Models.Progress;
using StalwartMigration.Core.Services;
using StalwartMigration.Infrastructure.FileSystem;
using StalwartMigration.Infrastructure.Stalwart;

namespace StalwartMigration.Core.Importers;

/// <summary>
/// Imports data into Stalwart mail server.
/// This is the fallback path when Vandelay is unavailable.
/// </summary>
public class StalwartImporter : ImporterBase
{
    private readonly string _inputDirectory;
    private readonly ILogger<StalwartImporter> _logger;
    private readonly ArchiveManager? _archiveManager;
    private readonly CheckpointService? _checkpointService;
    private bool _disposed;

    /// <summary>
    /// Gets the input directory for import files.
    /// </summary>
    public string InputDirectory => _inputDirectory;

    /// <summary>
    /// Initializes a new instance of the StalwartImporter class.
    /// </summary>
    public StalwartImporter(
        IStalwartClient client,
        string inputDirectory,
        ArchiveManager? archiveManager = null,
        CheckpointService? checkpointService = null,
        ILogger<StalwartImporter>? logger = null)
        : base(client, logger)
    {
        if (string.IsNullOrWhiteSpace(inputDirectory))
            throw new ArgumentException("Input directory cannot be null or empty.", nameof(inputDirectory));

        _inputDirectory = Path.GetFullPath(inputDirectory.TrimEnd(Path.DirectorySeparatorChar));
        _logger = logger ?? NullLogger<StalwartImporter>.Instance;
        _archiveManager = archiveManager ?? new ArchiveManager(_inputDirectory);
        _checkpointService = checkpointService ?? new CheckpointService(_inputDirectory);

        if (!Directory.Exists(_inputDirectory))
        {
            throw new DirectoryNotFoundException("Input directory does not exist.");
        }
    }

    /// <summary>
    /// Imports a single domain into Stalwart.
    /// </summary>
    public override async Task<ImportResult> ImportDomainAsync(Domain domain, CancellationToken ct = default)
    {
        if (domain == null)
            throw new ArgumentNullException(nameof(domain));

        _logger.LogInformation("Starting import of domain: {DomainName} ({DomainId})", domain.Name, domain.Id);

        try
        {
            // Import domain
            var stalwartDomain = await ImportDomainInternalAsync(domain, ct).ConfigureAwait(false);
            _logger.LogDebug("Imported domain {DomainName}", domain.Name);

            // Import accounts
            var domainDir = Path.Combine(_inputDirectory, domain.Name);
            var accountsJsonPath = Path.Combine(domainDir, $"{domain.Name}_accounts.json");
            
            if (File.Exists(accountsJsonPath))
            {
                var accountsJson = await File.ReadAllTextAsync(accountsJsonPath, ct).ConfigureAwait(false);
                var accounts = JsonSerializer.Deserialize<List<Account>>(accountsJson) ?? new List<Account>();
                
                foreach (var account in accounts)
                {
                    ct.ThrowIfCancellationRequested();
                    await ImportAccountInternalAsync(stalwartDomain, account, ct).ConfigureAwait(false);
                    _logger.LogDebug("Imported account {AccountName}", account.Name);
                }
                
                Progress?.Report(new ProgressReport(accounts.Count, accounts.Count, "Accounts imported"));
            }

            // Import aliases
            var aliasesJsonPath = Path.Combine(domainDir, $"{domain.Name}_aliases.json");
            if (File.Exists(aliasesJsonPath))
            {
                var aliasesJson = await File.ReadAllTextAsync(aliasesJsonPath, ct).ConfigureAwait(false);
                var aliases = JsonSerializer.Deserialize<List<EmailAlias>>(aliasesJson) ?? new List<EmailAlias>();
                
                foreach (var alias in aliases)
                {
                    ct.ThrowIfCancellationRequested();
                    await ImportAliasInternalAsync(alias, ct).ConfigureAwait(false);
                    _logger.LogDebug("Imported alias {AliasSource}", alias.Source);
                }
                
                Progress?.Report(new ProgressReport(1, 1, "Aliases imported"));
            }

            // Create checkpoint
            await CreateCheckpointAsync(domain.Name, domain, ct).ConfigureAwait(false);

            _logger.LogInformation("Successfully imported domain: {DomainName}", domain.Name);
            return ImportResult.Success(domain, new List<string> { domain.Name });
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Import of domain {DomainName} was cancelled", domain.Name);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import domain {DomainName}", domain.Name);
            return ImportResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// Imports all domains into Stalwart.
    /// </summary>
    public override async Task<List<ImportResult>> ImportAllDomainsAsync(CancellationToken ct = default)
    {
        var results = new List<ImportResult>();
        
        // Get all domain directories
        var domainDirs = Directory.GetDirectories(_inputDirectory);
        
        foreach (var domainDir in domainDirs)
        {
            ct.ThrowIfCancellationRequested();
            
            var domainName = Path.GetFileName(domainDir);
            var domainJsonPath = Path.Combine(domainDir, $"{domainName}_metadata.json");
            
            if (File.Exists(domainJsonPath))
            {
                var domainJson = await File.ReadAllTextAsync(domainJsonPath, ct).ConfigureAwait(false);
                var domain = JsonSerializer.Deserialize<Domain>(domainJson);
                
                if (domain != null)
                {
                    var result = await ImportDomainAsync(domain, ct).ConfigureAwait(false);
                    results.Add(result);
                }
            }
        }
        
        return results;
    }

    /// <summary>
    /// Imports a domain into Stalwart.
    /// </summary>
    private async Task<Domain> ImportDomainInternalAsync(Domain domain, CancellationToken ct)
    {
        var stalwartDomain = new Domain
        {
            Name = domain.Name,
            Description = domain.Description,
            Quota = domain.Quota,
            MaxAccounts = domain.MaxAccounts,
            IsEnabled = domain.IsEnabled
        };
        
        var createdDomain = await Client.CreateDomainAsync(stalwartDomain, ct).ConfigureAwait(false);
        return createdDomain;
    }

    /// <summary>
    /// Imports an account into Stalwart.
    /// </summary>
    private async Task ImportAccountInternalAsync(Domain domain, Account account, CancellationToken ct)
    {
        var stalwartAccount = new Account
        {
            Name = account.Name,
            DisplayName = account.DisplayName ?? account.Name,
            Password = account.Password,
            Email = account.Email,
            Quota = account.Quota,
            IsEnabled = account.IsEnabled,
            ForwardingAddresses = account.ForwardingAddresses,
            ForwardingEnabled = account.ForwardingEnabled,
            KeepForwardedCopy = account.KeepForwardedCopy
        };
        
        await Client.CreateAccountAsync(stalwartAccount, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Imports an alias into Stalwart.
    /// </summary>
    private async Task ImportAliasInternalAsync(EmailAlias alias, CancellationToken ct)
    {
        var stalwartAlias = new EmailAlias
        {
            Source = alias.Source,
            Destination = alias.Destination,
            IsEnabled = alias.IsEnabled
        };
        
        await Client.CreateAliasAsync(stalwartAlias, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a checkpoint for resume capability.
    /// </summary>
    private async Task CreateCheckpointAsync(string domainName, Domain domain, CancellationToken ct)
    {
        try
        {
            var state = new Dictionary<string, object>
            {
                { "domainName", domain.Name },
                { "domainId", domain.Id },
                { "timestamp", DateTime.UtcNow },
                { "type", "import" }
            };
            var checkpointPath = await _checkpointService!.CreateCheckpointAsync(domainName, state, ct).ConfigureAwait(false);
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
