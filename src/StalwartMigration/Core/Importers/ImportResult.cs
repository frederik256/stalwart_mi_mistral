// <copyright file="ImportResult.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using StalwartMigration.Core.Models;

namespace StalwartMigration.Core.Importers;

/// <summary>
/// Represents the result of an import operation.
/// </summary>
public class ImportResult
{
    /// <summary>
    /// Gets a value indicating whether the import was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the domain data that was imported.
    /// </summary>
    public Domain? DomainData { get; }

    /// <summary>
    /// Gets the error message if the import failed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the list of imported files.
    /// </summary>
    public List<string>? ImportedFiles { get; }

    /// <summary>
    /// Gets the number of accounts imported.
    /// </summary>
    public int AccountsImported { get; }

    /// <summary>
    /// Gets the number of messages imported.
    /// </summary>
    public int MessagesImported { get; }

    /// <summary>
    /// Gets the number of aliases imported.
    /// </summary>
    public int AliasesImported { get; }

    /// <summary>
    /// Gets the number of conflicts resolved.
    /// </summary>
    public int ConflictsResolved { get; }

    /// <summary>
    /// Initializes a new instance of the ImportResult class.
    /// </summary>
    public ImportResult(
        bool isSuccess,
        Domain? domainData = null,
        List<string>? importedFiles = null,
        string? errorMessage = null,
        int accountsImported = 0,
        int messagesImported = 0,
        int aliasesImported = 0,
        int conflictsResolved = 0)
    {
        IsSuccess = isSuccess;
        DomainData = domainData;
        ImportedFiles = importedFiles;
        ErrorMessage = errorMessage;
        AccountsImported = accountsImported;
        MessagesImported = messagesImported;
        AliasesImported = aliasesImported;
        ConflictsResolved = conflictsResolved;
    }

    /// <summary>
    /// Creates a successful import result.
    /// </summary>
    public static ImportResult Success(
        Domain domain,
        List<string>? importedFiles = null,
        int accountsImported = 0,
        int messagesImported = 0,
        int aliasesImported = 0,
        int conflictsResolved = 0)
        => new(true, domain, importedFiles, null, accountsImported, messagesImported, aliasesImported, conflictsResolved);

    /// <summary>
    /// Creates a failed import result.
    /// </summary>
    public static ImportResult Fail(string errorMessage)
        => new(false, null, null, errorMessage);
}
