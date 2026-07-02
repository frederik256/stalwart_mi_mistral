// <copyright file="CheckpointService.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace StalwartMigration.Core.Services;

/// <summary>
/// Service for managing checkpoints to enable resumable migrations.
/// Checkpoints are created every 30 seconds during migration.
/// </summary>
public class CheckpointService : ICheckpointService
{
    private readonly ILogger<CheckpointService> _logger;
    private readonly string _checkpointDirectory;
    private bool _disposed;

    /// <summary>
    /// Gets the base directory for storing checkpoints.
    /// </summary>
    public string BaseDirectory { get; }

    /// <summary>
    /// Gets the directory where checkpoints are stored.
    /// </summary>
    public string CheckpointDirectory => _checkpointDirectory;

    /// <summary>
    /// Initializes a new instance of the CheckpointService class.
    /// </summary>
    /// <param name="baseDirectory">The base directory for storing checkpoints.</param>
    /// <param name="logger">The logger instance.</param>
    public CheckpointService(string baseDirectory, ILogger<CheckpointService>? logger = null)
    {
        if (string.IsNullOrWhiteSpace(baseDirectory))
            throw new ArgumentException("Base directory cannot be null or empty.", nameof(baseDirectory));

        BaseDirectory = System.IO.Path.GetFullPath(baseDirectory.TrimEnd(System.IO.Path.DirectorySeparatorChar));
        _checkpointDirectory = System.IO.Path.Combine(BaseDirectory, "checkpoints");
        _logger = logger ?? NullLogger<CheckpointService>.Instance;

        // Ensure checkpoint directory exists
        if (!Directory.Exists(_checkpointDirectory))
        {
            Directory.CreateDirectory(_checkpointDirectory);
            _logger.LogInformation("Created checkpoint directory: {Directory}", _checkpointDirectory);
        }
    }

    /// <summary>
    /// Gets the path to a checkpoint file.
    /// </summary>
    /// <param name="checkpointName">The name of the checkpoint.</param>
    /// <returns>The full path to the checkpoint file.</returns>
    public string GetCheckpointPath(string checkpointName)
    {
        if (string.IsNullOrWhiteSpace(checkpointName))
            throw new ArgumentException("Checkpoint name cannot be null or empty.", nameof(checkpointName));

        // Sanitize the checkpoint name to prevent directory traversal
        var safeName = System.IO.Path.GetFileNameWithoutExtension(checkpointName);
        if (string.IsNullOrWhiteSpace(safeName))
            throw new ArgumentException("Invalid checkpoint name.", nameof(checkpointName));

        return System.IO.Path.Combine(_checkpointDirectory, $"{safeName}.json");
    }

    /// <summary>
    /// Creates a checkpoint with the given state.
    /// </summary>
    /// <param name="checkpointName">The name of the checkpoint.</param>
    /// <param name="state">The state to save.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The path to the checkpoint file.</returns>
    /// <exception cref="ArgumentException">Thrown when checkpointName is invalid.</exception>
    public async Task<string> CreateCheckpointAsync(string checkpointName, Dictionary<string, object> state, CancellationToken cancellationToken = default)
    {
        var checkpointPath = GetCheckpointPath(checkpointName);

        _logger.LogDebug("Creating checkpoint: {CheckpointName} at {CheckpointPath}", checkpointName, checkpointPath);

        // Add timestamp to state
        state["checkpointTimestamp"] = DateTime.UtcNow;
        state["checkpointName"] = checkpointName;

        var json = JsonSerializer.Serialize(state, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        cancellationToken.ThrowIfCancellationRequested();

        await System.IO.File.WriteAllTextAsync(checkpointPath, json, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Checkpoint created: {CheckpointPath}", checkpointPath);

        return checkpointPath;
    }

    /// <summary>
    /// Loads a checkpoint from the given file path.
    /// </summary>
    /// <param name="checkpointPath">The path to the checkpoint file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The loaded state.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the checkpoint file doesn't exist.</exception>
    public async Task<Dictionary<string, object>> LoadCheckpointAsync(string checkpointPath, CancellationToken cancellationToken = default)
    {
        if (!System.IO.File.Exists(checkpointPath))
            throw new FileNotFoundException("Checkpoint file not found.", checkpointPath);

        _logger.LogDebug("Loading checkpoint: {CheckpointPath}", checkpointPath);

        cancellationToken.ThrowIfCancellationRequested();

        var json = await System.IO.File.ReadAllTextAsync(checkpointPath, cancellationToken).ConfigureAwait(false);

        try
        {
            var state = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (state == null)
                return new Dictionary<string, object>();

            // Convert JsonElement to object
            var result = new Dictionary<string, object>();
            foreach (var kvp in state)
            {
                result[kvp.Key] = ConvertJsonElement(kvp.Value);
            }

            _logger.LogInformation("Checkpoint loaded: {CheckpointPath}", checkpointPath);

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize checkpoint: {CheckpointPath}", checkpointPath);
            throw;
        }
    }

    /// <summary>
    /// Converts a JsonElement to a .NET object.
    /// </summary>
    private static object ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null!,
            JsonValueKind.Number =>
                element.TryGetInt32(out var int32) ? int32 :
                element.TryGetInt64(out var int64) ? int64 :
                element.TryGetDouble(out var dbl) ? dbl :
                element.GetRawText(),
            JsonValueKind.String => element.GetString()!,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(k => k.Name, v => ConvertJsonElement(v.Value)),
            _ => element.GetRawText()!
        };
    }

    /// <summary>
    /// Checks if a checkpoint exists for the given name.
    /// </summary>
    /// <param name="checkpointName">The name of the checkpoint.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the checkpoint exists.</returns>
    public async Task<bool> CheckpointExistsAsync(string checkpointName, CancellationToken cancellationToken = default)
    {
        var checkpointPath = GetCheckpointPath(checkpointName);
        cancellationToken.ThrowIfCancellationRequested();
        return await Task.FromResult(System.IO.File.Exists(checkpointPath)).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes a checkpoint file.
    /// </summary>
    /// <param name="checkpointName">The name of the checkpoint to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task DeleteCheckpointAsync(string checkpointName, CancellationToken cancellationToken = default)
    {
        var checkpointPath = GetCheckpointPath(checkpointName);

        if (System.IO.File.Exists(checkpointPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            System.IO.File.Delete(checkpointPath);
            _logger.LogDebug("Deleted checkpoint: {CheckpointPath}", checkpointPath);
        }
        else
        {
            _logger.LogWarning("Checkpoint not found for deletion: {CheckpointPath}", checkpointPath);
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Disposes the service.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
