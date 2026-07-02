// <copyright file="IImporter.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using StalwartMigration.Core.Models;
using StalwartMigration.Core.Models.Progress;
using StalwartMigration.Infrastructure.Stalwart;

namespace StalwartMigration.Core.Importers;

/// <summary>
/// Interface for data importers.
/// </summary>
public interface IImporter : IDisposable
{
    /// <summary>
    /// Gets the Stalwart client used for data import.
    /// </summary>
    IStalwartClient Client { get; }

    /// <summary>
    /// Gets the progress reporter.
    /// </summary>
    IProgress<ProgressReport> Progress { get; }

    /// <summary>
    /// Imports a single domain.
    /// </summary>
    /// <param name="domain">The domain to import.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The import result.</returns>
    Task<ImportResult> ImportDomainAsync(Domain domain, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports all domains from the archive directory.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of import results for each domain.</returns>
    Task<List<ImportResult>> ImportAllDomainsAsync(CancellationToken cancellationToken = default);
}
