// <copyright file="ICheckpointService.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

namespace StalwartMigration.Core.Services;

/// <summary>
/// Interface for checkpoint/resume functionality.
/// </summary>
public interface ICheckpointService : IDisposable
{
    /// <summary>
    /// The base directory for storing checkpoints.
    /// </summary>
    string BaseDirectory { get; }

    /// <summary>
    /// Gets the directory where checkpoints are stored.
    /// </summary>
    string CheckpointDirectory { get; }

    /// <summary>
    /// Creates a checkpoint with the given state.
    /// </summary>
    /// <param name="checkpointName">The name of the checkpoint.</param>
    /// <param name="state">The state to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The path to the checkpoint file.</returns>
    Task<string> CreateCheckpointAsync(string checkpointName, Dictionary<string, object> state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a checkpoint from the given file path.
    /// </summary>
    /// <param name="checkpointPath">The path to the checkpoint file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The loaded state.</returns>
    Task<Dictionary<string, object>> LoadCheckpointAsync(string checkpointPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a checkpoint exists for the given name.
    /// </summary>
    /// <param name="checkpointName">The name of the checkpoint.</param>
    /// <returns>True if the checkpoint exists.</returns>
    Task<bool> CheckpointExistsAsync(string checkpointName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the path to a checkpoint file.
    /// </summary>
    /// <param name="checkpointName">The name of the checkpoint.</param>
    /// <returns>The full path to the checkpoint file.</returns>
    string GetCheckpointPath(string checkpointName);

    /// <summary>
    /// Deletes a checkpoint file.
    /// </summary>
    /// <param name="checkpointName">The name of the checkpoint to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task DeleteCheckpointAsync(string checkpointName, CancellationToken cancellationToken = default);
}
