// <copyright file="ImporterBase.cs" company="Stalwart Labs">
// Copyright (c) Stalwart Labs 2024. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StalwartMigration.Core.Models;
using StalwartMigration.Core.Models.Progress;
using StalwartMigration.Infrastructure.Stalwart;

namespace StalwartMigration.Core.Importers;

/// <summary>
/// Abstract base class for data importers.
/// </summary>
public abstract class ImporterBase : IImporter
{
    private readonly ILogger _logger;
    private bool _disposed;

    /// <summary>Gets the Stalwart client.</summary>
    public IStalwartClient Client { get; }

    /// <summary>Gets the progress reporter.</summary>
    public IProgress<ProgressReport> Progress { get; }

    /// <summary>Initializes a new instance.</summary>
    protected ImporterBase(IStalwartClient client, ILogger? logger = null)
    {
        Client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? NullLogger<ImporterBase>.Instance;
        Progress = new Progress<ProgressReport>(report =>
            _logger.LogInformation("Progress: {Current}/{Total} ({ProgressPercentage}%) - {Description}",
                report.Current, report.Total, report.ProgressPercentage, report.Description));
    }

    /// <summary>Imports a single domain.</summary>
    public abstract Task<ImportResult> ImportDomainAsync(Domain domain, CancellationToken ct = default);

    /// <summary>Imports all domains.</summary>
    public abstract Task<List<ImportResult>> ImportAllDomainsAsync(CancellationToken ct = default);

    /// <summary>Disposes resources.</summary>
    public virtual void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
