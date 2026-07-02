// <copyright file="IExporter.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using StalwartMigration.Core.Models;

namespace StalwartMigration.Core.Exporters;

/// <summary>
/// Interface for data exporters.
/// </summary>
public interface IExporter : IDisposable
{
    /// <summary>
    /// Gets the hMailServer client used for data extraction.
    /// </summary>
    IHMailServerClient Client { get; }

    /// <summary>
    /// Gets the progress reporter.
    /// </summary>
    IProgress<ProgressReport> Progress { get; }

    /// <summary>
    /// Exports a single domain.
    /// </summary>
    /// <param name="domain">The domain to export.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The export result.</returns>
    Task<ExportResult> ExportDomainAsync(Domain domain, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports all domains.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of export results for each domain.</returns>
    Task<List<ExportResult>> ExportAllDomainsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the result of an export operation.
/// </summary>
public class ExportResult
{
    /// <summary>
    /// Gets a value indicating whether the export was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the domain data that was exported.
    /// </summary>
    public Domain? DomainData { get; }

    /// <summary>
    /// Gets the error message if the export failed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the list of exported files.
    /// </summary>
    public List<string>? ExportedFiles { get; }

    /// <summary>
    /// Initializes a new instance of the ExportResult class.
    /// </summary>
    /// <param name="isSuccess">Whether the export was successful.</param>
    /// <param name="domainData">The domain data.</param>
    /// <param name="exportedFiles">The list of exported files.</param>
    /// <param name="errorMessage">The error message.</param>
    public ExportResult(bool isSuccess, Domain? domainData = null, List<string>? exportedFiles = null, string? errorMessage = null)
    {
        IsSuccess = isSuccess;
        DomainData = domainData;
        ExportedFiles = exportedFiles;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Creates a successful export result.
    /// </summary>
    public static ExportResult Success(Domain domain, List<string>? exportedFiles = null)
        => new(true, domain, exportedFiles);

    /// <summary>
    /// Creates a failed export result.
    /// </summary>
    public static ExportResult Fail(string errorMessage)
        => new(false, null, null, errorMessage);
}
