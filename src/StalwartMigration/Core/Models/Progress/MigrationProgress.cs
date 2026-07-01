// <copyright file="MigrationProgress.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

namespace StalwartMigration.Core.Models.Progress;

/// <summary>
/// Represents the progress of a migration operation.
/// </summary>
public class MigrationProgress : IProgress<ProgressReport>
{
    private readonly Action<ProgressReport>? _handler;

    /// <summary>
    /// Gets or sets the total number of operations.
    /// </summary>
    [JsonPropertyName("totalOperations")]
    public int TotalOperations { get; set; }

    /// <summary>
    /// Gets or sets the current operation number.
    /// </summary>
    [JsonPropertyName("currentOperation")]
    public int CurrentOperation { get; set; }

    /// <summary>
    /// Gets or sets the current operation description.
    /// </summary>
    [JsonPropertyName("currentOperationDescription")]
    public string? CurrentOperationDescription { get; set; }

    /// <summary>
    /// Gets or sets the progress percentage (0-100).
    /// </summary>
    [JsonPropertyName("progressPercentage")]
    public int ProgressPercentage
    {
        get
        {
            if (TotalOperations == 0)
            {
                return 0;
            }
            return (int)((double)CurrentOperation / TotalOperations * 100);
        }
    }

    /// <summary>
    /// Gets or sets whether the operation is complete.
    /// </summary>
    [JsonPropertyName("isComplete")]
    public bool IsComplete => CurrentOperation >= TotalOperations;

    /// <summary>
    /// Gets or sets the start time of the operation.
    /// </summary>
    [JsonPropertyName("startedAt")]
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the last update time.
    /// </summary>
    [JsonPropertyName("lastUpdatedAt")]
    public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the elapsed time since the operation started.
    /// </summary>
    [JsonIgnore]
    public TimeSpan Elapsed => DateTimeOffset.UtcNow - StartedAt;

    /// <summary>
    /// Gets the estimated time remaining based on current progress.
    /// </summary>
    [JsonIgnore]
    public TimeSpan EstimatedTimeRemaining
    {
        get
        {
            if (CurrentOperation == 0 || ProgressPercentage >= 100)
            {
                return TimeSpan.Zero;
            }

            double completedFraction = (double)CurrentOperation / TotalOperations;
            double elapsedSeconds = Elapsed.TotalSeconds;
            double totalEstimatedSeconds = elapsedSeconds / completedFraction;
            double remainingSeconds = totalEstimatedSeconds - elapsedSeconds;

            return TimeSpan.FromSeconds(Math.Max(0, remainingSeconds));
        }
    }

    /// <summary>
    /// Gets the estimated completion time.
    /// </summary>
    [JsonIgnore]
    public DateTimeOffset EstimatedCompletionTime => DateTimeOffset.UtcNow.Add(EstimatedTimeRemaining);

    /// <summary>
    /// Initializes a new instance of the MigrationProgress class.
    /// </summary>
    public MigrationProgress()
    { }

    /// <summary>
    /// Initializes a new instance of the MigrationProgress class with the specified total operations.
    /// </summary>
    /// <param name="totalOperations">The total number of operations.</param>
    public MigrationProgress(int totalOperations)
    {
        TotalOperations = totalOperations;
    }

    /// <summary>
    /// Initializes a new instance of the MigrationProgress class with a handler.
    /// </summary>
    /// <param name="handler">The handler to invoke for progress reports.</param>
    public MigrationProgress(Action<ProgressReport>? handler)
    {
        _handler = handler;
    }

    /// <summary>
    /// Initializes a new instance of the MigrationProgress class with the specified total operations and handler.
    /// </summary>
    /// <param name="totalOperations">The total number of operations.</param>
    /// <param name="handler">The handler to invoke for progress reports.</param>
    public MigrationProgress(int totalOperations, Action<ProgressReport>? handler)
        : this(handler)
    {
        TotalOperations = totalOperations;
    }

    /// <summary>
    /// Reports progress for the current operation.
    /// </summary>
    /// <param name="value">The progress report.</param>
    public void Report(ProgressReport value)
    {
        CurrentOperation = value.Current;
        CurrentOperationDescription = value.Description;
        LastUpdatedAt = DateTimeOffset.UtcNow;
        _handler?.Invoke(value);
    }

    /// <summary>
    /// Reports progress with a description.
    /// </summary>
    /// <param name="current">The current operation number.</param>
    /// <param name="description">The description of the current operation.</param>
    public void Report(int current, string? description = null)
    {
        CurrentOperation = current;
        CurrentOperationDescription = description;
        LastUpdatedAt = DateTimeOffset.UtcNow;

        var report = new ProgressReport(current, TotalOperations, description);
        _handler?.Invoke(report);
    }

    /// <summary>
    /// Advances to the next operation.
    /// </summary>
    /// <param name="description">The description of the next operation.</param>
    public void Next(string? description = null)
    {
        Report(CurrentOperation + 1, description);
    }

    /// <summary>
    /// Resets the progress.
    /// </summary>
    public void Reset()
    {
        CurrentOperation = 0;
        CurrentOperationDescription = null;
        StartedAt = DateTimeOffset.UtcNow;
        LastUpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Completes the progress.
    /// </summary>
    public void Complete()
    {
        CurrentOperation = TotalOperations;
        LastUpdatedAt = DateTimeOffset.UtcNow;

        var report = new ProgressReport(TotalOperations, TotalOperations, "Migration complete");
        _handler?.Invoke(report);
    }
}
