// <copyright file="IMigrationOrchestrator.cs" company="Stalwart Labs">
// Copyright (c) Stalwart Labs 2024. All rights reserved.
// </copyright>

using StalwartMigration.Core.Models;
using StalwartMigration.Core.Models.Progress;

namespace StalwartMigration.Core;

/// <summary>
/// Interface for the migration orchestrator.
/// </summary>
public interface IMigrationOrchestrator : IDisposable
{
    /// <summary>
    /// Gets the progress reporter.
    /// </summary>
    IProgress<ProgressReport> Progress { get; }

    /// <summary>
    /// Runs the setup phase to create domains, accounts, and aliases.
    /// </summary>
    Task<MigrationResult> SetupAsync(MigrationOptions options, CancellationToken ct = default);

    /// <summary>
    /// Runs the full migration workflow.
    /// </summary>
    Task<MigrationResult> MigrateAsync(MigrationOptions options, CancellationToken ct = default);

    /// <summary>
    /// Validates the migration.
    /// </summary>
    Task<MigrationResult> ValidateAsync(MigrationOptions options, CancellationToken ct = default);
}
