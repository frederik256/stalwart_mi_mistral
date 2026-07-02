// <copyright file="MigrationResult.cs" company="Stalwart Labs">
// Copyright (c) Stalwart Labs 2024. All rights reserved.
// </copyright>

using StalwartMigration.Core.Models;

namespace StalwartMigration.Core;

/// <summary>
/// Result of a migration operation.
/// </summary>
public class MigrationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the migration was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the migration failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the total number of domains processed.
    /// </summary>
    public int DomainsProcessed { get; set; }

    /// <summary>
    /// Gets or sets the total number of accounts processed.
    /// </summary>
    public int AccountsProcessed { get; set; }

    /// <summary>
    /// Gets or sets the total number of aliases processed.
    /// </summary>
    public int AliasesProcessed { get; set; }

    /// <summary>
    /// Gets or sets the total number of messages processed.
    /// </summary>
    public int MessagesProcessed { get; set; }

    /// <summary>
    /// Gets or sets the list of domain results.
    /// </summary>
    public List<DomainResult> DomainResults { get; set; } = new();

    /// <summary>
    /// Gets or sets the start time of the migration.
    /// </summary>
    public DateTimeOffset StartTime { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the end time of the migration.
    /// </summary>
    public DateTimeOffset EndTime { get; set; }

    /// <summary>
    /// Gets the duration of the migration.
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>
    /// Creates a successful migration result.
    /// </summary>
    public static MigrationResult CreateSuccess(int domainsProcessed, int accountsProcessed, int aliasesProcessed, int messagesProcessed)
        => new MigrationResult
        {
            Success = true,
            DomainsProcessed = domainsProcessed,
            AccountsProcessed = accountsProcessed,
            AliasesProcessed = aliasesProcessed,
            MessagesProcessed = messagesProcessed,
            EndTime = DateTimeOffset.UtcNow
        };

    /// <summary>
    /// Creates a failed migration result.
    /// </summary>
    public static MigrationResult CreateFail(string errorMessage)
        => new MigrationResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            EndTime = DateTimeOffset.UtcNow
        };
}

/// <summary>
/// Result for a single domain migration.
/// </summary>
public class DomainResult
{
    /// <summary>
    /// Gets or sets the domain name.
    /// </summary>
    public string? DomainName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the domain migration was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the domain migration failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the number of accounts processed.
    /// </summary>
    public int AccountsProcessed { get; set; }

    /// <summary>
    /// Gets or sets the number of aliases processed.
    /// </summary>
    public int AliasesProcessed { get; set; }

    /// <summary>
    /// Gets or sets the number of messages processed.
    /// </summary>
    public int MessagesProcessed { get; set; }
}
