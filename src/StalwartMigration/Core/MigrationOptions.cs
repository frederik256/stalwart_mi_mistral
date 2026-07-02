// <copyright file="MigrationOptions.cs" company="Stalwart Labs">
// Copyright (c) Stalwart Labs 2024. All rights reserved.
// </copyright>

namespace StalwartMigration.Core;

/// <summary>
/// Options for the migration process.
/// </summary>
public class MigrationOptions
{
    /// <summary>
    /// Gets or sets the hMailServer connection string.
    /// </summary>
    public string? HMailServerConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the Stalwart API base URL.
    /// </summary>
    public string? StalwartBaseUrl { get; set; }

    /// <summary>
    /// Gets or sets the Stalwart API credentials.
    /// </summary>
    public string? StalwartUsername { get; set; }

    /// <summary>
    /// Gets or sets the Stalwart API password.
    /// </summary>
    public string? StalwartPassword { get; set; }

    /// <summary>
    /// Gets or sets the output directory for export files.
    /// </summary>
    public string? OutputDirectory { get; set; }

    /// <summary>
    /// Gets or sets the input directory for import files.
    /// </summary>
    public string? InputDirectory { get; set; }

    /// <summary>
    /// Gets or sets the batch size for processing.
    /// </summary>
    public int BatchSize { get; set; } = 10;

    /// <summary>
    /// Gets or sets a value indicating whether to use Vandelay for message migration.
    /// </summary>
    public bool UseVandelay { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to run in dry-run mode.
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// Gets or sets the checkpoint interval in seconds.
    /// </summary>
    public int CheckpointIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets a value indicating whether to resume from the last checkpoint.
    /// </summary>
    public bool ResumeFromCheckpoint { get; set; }

    /// <summary>
    /// Gets or sets the domain names to migrate (empty means all domains).
    /// </summary>
    public List<string> DomainNames { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to skip message migration.
    /// </summary>
    public bool SkipMessages { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to skip validation.
    /// </summary>
    public bool SkipValidation { get; set; }
}
