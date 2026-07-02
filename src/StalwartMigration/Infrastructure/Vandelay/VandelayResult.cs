// <copyright file="VandelayResult.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

namespace StalwartMigration.Infrastructure.Vandelay;

/// <summary>
/// Represents the result of a Vandelay operation.
/// </summary>
public class VandelayResult
{
    /// <summary>
    /// Gets or sets whether the operation succeeded.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the exit code from the Vandelay process.
    /// </summary>
    [JsonPropertyName("exitCode")]
    public int ExitCode { get; set; }

    /// <summary>
    /// Gets or sets the standard output from the Vandelay process.
    /// </summary>
    [JsonPropertyName("stdout")]
    public string? StandardOutput { get; set; }

    /// <summary>
    /// Gets or sets the standard error from the Vandelay process.
    /// </summary>
    [JsonPropertyName("stderr")]
    public string? StandardError { get; set; }

    /// <summary>
    /// Gets or sets the command that was executed.
    /// </summary>
    [JsonPropertyName("command")]
    public string? Command { get; set; }

    /// <summary>
    /// Gets or sets the arguments passed to the command.
    /// </summary>
    [JsonPropertyName("arguments")]
    public List<string> Arguments { get; set; } = new();

    /// <summary>
    /// Gets or sets the working directory where the command was executed.
    /// </summary>
    [JsonPropertyName("workingDirectory")]
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Gets or sets the duration of the operation.
    /// </summary>
    [JsonPropertyName("duration")]
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the start time of the operation.
    /// </summary>
    [JsonPropertyName("startTime")]
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time of the operation.
    /// </summary>
    [JsonPropertyName("endTime")]
    public DateTimeOffset EndTime { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the number of items processed (messages, folders, etc.).
    /// </summary>
    [JsonPropertyName("itemsProcessed")]
    public int ItemsProcessed { get; set; }

    /// <summary>
    /// Gets or sets the number of items that succeeded.
    /// </summary>
    [JsonPropertyName("itemsSucceeded")]
    public int ItemsSucceeded { get; set; }

    /// <summary>
    /// Gets or sets the number of items that failed.
    /// </summary>
    [JsonPropertyName("itemsFailed")]
    public int ItemsFailed { get; set; }

    /// <summary>
    /// Gets or sets the number of items that were skipped.
    /// </summary>
    [JsonPropertyName("itemsSkipped")]
    public int ItemsSkipped { get; set; }

    /// <summary>
    /// Gets or sets the Vandelay version that was used.
    /// </summary>
    [JsonPropertyName("vandelayVersion")]
    public string? VandelayVersion { get; set; }

    /// <summary>
    /// Gets or sets additional metadata from the operation.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Initializes a new instance of the VandelayResult class.
    /// </summary>
    public VandelayResult()
    {
        StartTime = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Marks the result as completed and calculates the duration.
    /// </summary>
    public void Complete()
    {
        EndTime = DateTimeOffset.UtcNow;
        Duration = EndTime - StartTime;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="output">The standard output.</param>
    /// <param name="itemsProcessed">The number of items processed.</param>
    /// <returns>A successful VandelayResult.</returns>
    public static VandelayResult ForSuccess(string? output = null, int itemsProcessed = 0)
    {
        return new VandelayResult
        {
            Success = true,
            ExitCode = 0,
            StandardOutput = output,
            ItemsProcessed = itemsProcessed,
            ItemsSucceeded = itemsProcessed
        };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="exitCode">The exit code.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="standardError">The standard error output.</param>
    /// <returns>A failed VandelayResult.</returns>
    public static VandelayResult ForFailure(int exitCode, string? errorMessage = null, string? standardError = null)
    {
        return new VandelayResult
        {
            Success = false,
            ExitCode = exitCode,
            ErrorMessage = errorMessage,
            StandardError = standardError
        };
    }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    public override string ToString()
    {
        var status = Success ? "SUCCESS" : "FAILED";
        return $"Vandelay {status} (Exit Code: {ExitCode}, Duration: {Duration.TotalSeconds:F2}s, Items: {ItemsProcessed})";
    }
}
