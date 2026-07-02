// <copyright file="ExporterBase.cs" company="Stalwart Labs">
// Copyright (c) Stalwart Labs 2024. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StalwartMigration.Core.Models;
using StalwartMigration.Core.Models.Progress;
using StalwartMigration.Infrastructure.HMailServer;

namespace StalwartMigration.Core.Exporters;

/// <summary>
/// Abstract base class for data exporters.
/// </summary>
public abstract class ExporterBase : IExporter
{
    private readonly ILogger _logger;
    private bool _disposed;

    /// <summary>Gets the hMailServer client.</summary>
    public IHMailServerClient Client { get; }

    /// <summary>Gets the progress reporter.</summary>
    public IProgress<ProgressReport> Progress { get; }

    /// <summary>Initializes a new instance.</summary>
    protected ExporterBase(IHMailServerClient client, ILogger? logger = null)
    {
        Client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? NullLogger<ExporterBase>.Instance;
        Progress = new Progress<ProgressReport>(report =>
            _logger.LogInformation("Progress: {Current}/{Total} ({ProgressPercentage}%) - {Description}",
                report.Current, report.Total, report.ProgressPercentage, report.Description));
    }

    /// <summary>Exports a single domain.</summary>
    public abstract Task<ExportResult> ExportDomainAsync(Domain domain, CancellationToken ct = default);

    /// <summary>Exports all domains.</summary>
    public async Task<List<ExportResult>> ExportAllDomainsAsync(CancellationToken ct = default)
    {
        var results = new List<ExportResult>();
        try
        {
            var domains = await Client.GetDomainsAsync(ct).ConfigureAwait(false);
            foreach (var domain in domains)
            {
                ct.ThrowIfCancellationRequested();
                results.Add(await ExportDomainAsync(domain, ct).ConfigureAwait(false));
            }
        }
        catch (Exception ex)
        {
            results.Add(ExportResult.Fail(ex.Message));
        }
        return results;
    }

    /// <summary>Disposes resources.</summary>
    public virtual void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
