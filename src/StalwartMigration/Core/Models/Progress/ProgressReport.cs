// <copyright file="ProgressReport.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

namespace StalwartMigration.Core.Models.Progress;

/// <summary>
/// Represents a progress report for a single operation.
/// </summary>
public class ProgressReport
{
    /// <summary>
    /// Gets or sets the current operation number.
    /// </summary>
    [JsonPropertyName("current")]
    public int Current { get; set; }

    /// <summary>
    /// Gets or sets the total number of operations.
    /// </summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>
    /// Gets or sets the description of the current operation.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the progress percentage (0-100).
    /// </summary>
    [JsonPropertyName("progressPercentage")]
    public int ProgressPercentage
    {
        get
        {
            if (Total == 0)
            {
                return 0;
            }
            return (int)((double)Current / Total * 100);
        }
    }

    /// <summary>
    /// Gets or sets the timestamp of the progress report.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the elapsed time for this operation.
    /// </summary>
    [JsonPropertyName("elapsed")]
    public TimeSpan Elapsed { get; set; }

    /// <summary>
    /// Gets or sets whether this is the final report.
    /// </summary>
    [JsonPropertyName("isFinal")]
    public bool IsFinal { get; set; }

    /// <summary>
    /// Gets or sets additional data for the progress report.
    /// </summary>
    [JsonPropertyName("data")]
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>
    /// Gets whether the operation is complete.
    /// </summary>
    [JsonIgnore]
    public bool IsComplete => Current >= Total;

    /// <summary>
    /// Initializes a new instance of the ProgressReport class.
    /// </summary>
    public ProgressReport()
    { }

    /// <summary>
    /// Initializes a new instance of the ProgressReport class with the specified current and total.
    /// </summary>
    /// <param name="current">The current operation number.</param>
    /// <param name="total">The total number of operations.</param>
    public ProgressReport(int current, int total)
    {
        Current = current;
        Total = total;
    }

    /// <summary>
    /// Initializes a new instance of the ProgressReport class with the specified current, total, and description.
    /// </summary>
    /// <param name="current">The current operation number.</param>
    /// <param name="total">The total number of operations.</param>
    /// <param name="description">The description of the current operation.</param>
    public ProgressReport(int current, int total, string? description)
        : this(current, total)
    {
        Description = description;
    }

    /// <summary>
    /// Creates a final progress report.
    /// </summary>
    /// <param name="total">The total number of operations.</param>
    /// <param name="description">The description.</param>
    /// <returns>A final progress report.</returns>
    public static ProgressReport Final(int total, string? description = null)
    {
        return new ProgressReport(total, total, description ?? "Complete")
        {
            IsFinal = true
        };
    }

    /// <summary>
    /// Adds data to the progress report.
    /// </summary>
    /// <param name="key">The data key.</param>
    /// <param name="value">The data value.</param>
    public void AddData(string key, object value)
    {
        Data[key] = value;
    }

    /// <summary>
    /// Gets a data value by key.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The data key.</param>
    /// <returns>The value if found; otherwise, default.</returns>
    public T? GetData<T>(string key)
    {
        if (Data.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    public override string ToString()
    {
        return $"[{ProgressPercentage}%] {Current}/{Total}: {Description ?? "Processing"}";
    }
}
