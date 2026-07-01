// <copyright file="MigrationState.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace StalwartMigration.Core.Models;

/// <summary>
/// Represents the state of a migration for checkpoint/resume capability.
/// </summary>
public class MigrationState
{
    /// <summary>
    /// Gets or sets the unique identifier for the migration state.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the version of the migration state format.
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets the date and time when the migration was started.
    /// </summary>
    [JsonPropertyName("startedAt")]
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the migration was last updated.
    /// </summary>
    [JsonPropertyName("lastUpdatedAt")]
    public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the overall migration status.
    /// </summary>
    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MigrationStatus Status { get; set; } = MigrationStatus.NotStarted;

    /// <summary>
    /// Gets or sets the current phase of the migration.
    /// </summary>
    [JsonPropertyName("currentPhase")]
    public string? CurrentPhase { get; set; }

    /// <summary>
    /// Gets or sets the current task within the phase.
    /// </summary>
    [JsonPropertyName("currentTask")]
    public string? CurrentTask { get; set; }

    /// <summary>
    /// Gets or sets the progress percentage (0-100).
    /// </summary>
    [JsonPropertyName("progressPercentage")]
    [Range(0, 100, ErrorMessage = "Progress percentage must be between 0 and 100.")]
    public int ProgressPercentage { get; set; }

    /// <summary>
    /// Gets or sets the total number of domains to migrate.
    /// </summary>
    [JsonPropertyName("totalDomains")]
    public int TotalDomains { get; set; }

    /// <summary>
    /// Gets or sets the number of domains successfully migrated.
    /// </summary>
    [JsonPropertyName("migratedDomains")]
    public int MigratedDomains { get; set; }

    /// <summary>
    /// Gets or sets the number of domains that failed to migrate.
    /// </summary>
    [JsonPropertyName("failedDomains")]
    public int FailedDomains { get; set; }

    /// <summary>
    /// Gets or sets the total number of accounts to migrate.
    /// </summary>
    [JsonPropertyName("totalAccounts")]
    public int TotalAccounts { get; set; }

    /// <summary>
    /// Gets or sets the number of accounts successfully migrated.
    /// </summary>
    [JsonPropertyName("migratedAccounts")]
    public int MigratedAccounts { get; set; }

    /// <summary>
    /// Gets or sets the number of accounts that failed to migrate.
    /// </summary>
    [JsonPropertyName("failedAccounts")]
    public int FailedAccounts { get; set; }

    /// <summary>
    /// Gets or sets the total number of emails to migrate.
    /// </summary>
    [JsonPropertyName("totalEmails")]
    public int TotalEmails { get; set; }

    /// <summary>
    /// Gets or sets the number of emails successfully migrated.
    /// </summary>
    [JsonPropertyName("migratedEmails")]
    public int MigratedEmails { get; set; }

    /// <summary>
    /// Gets or sets the number of emails that failed to migrate.
    /// </summary>
    [JsonPropertyName("failedEmails")]
    public int FailedEmails { get; set; }

    /// <summary>
    /// Gets or sets the total size of data to migrate in bytes.
    /// </summary>
    [JsonPropertyName("totalBytes")]
    public long TotalBytes { get; set; }

    /// <summary>
    /// Gets or sets the number of bytes successfully migrated.
    /// </summary>
    [JsonPropertyName("migratedBytes")]
    public long MigratedBytes { get; set; }

    /// <summary>
    /// Gets or sets the list of domains that have been migrated.
    /// </summary>
    [JsonPropertyName("migratedDomainsList")]
    public List<string> MigratedDomainsList { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of domains that failed to migrate.
    /// </summary>
    [JsonPropertyName("failedDomainsList")]
    public List<string> FailedDomainsList { get; set; } = new();

    /// <summary>
    /// Gets or sets the checkpoint data for resumable migrations.
    /// </summary>
    [JsonPropertyName("checkpoint")]
    public MigrationCheckpoint? Checkpoint { get; set; }

    /// <summary>
    /// Gets or sets error information for failed migrations.
    /// </summary>
    [JsonPropertyName("errors")]
    public List<MigrationError> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets custom metadata for the migration.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets whether the migration has been started.
    /// </summary>
    [JsonIgnore]
    public bool IsStarted => Status != MigrationStatus.NotStarted;

    /// <summary>
    /// Gets whether the migration is currently in progress.
    /// </summary>
    [JsonIgnore]
    public bool IsInProgress => Status == MigrationStatus.InProgress;

    /// <summary>
    /// Gets whether the migration has been completed successfully.
    /// </summary>
    [JsonIgnore]
    public bool IsCompleted => Status == MigrationStatus.Completed;

    /// <summary>
    /// Gets whether the migration has failed.
    /// </summary>
    [JsonIgnore]
    public bool IsFailed => Status == MigrationStatus.Failed;

    /// <summary>
    /// Gets whether the migration can be resumed.
    /// </summary>
    [JsonIgnore]
    public bool CanResume => Status == MigrationStatus.Paused || Status == MigrationStatus.InProgress;

    /// <summary>
    /// Gets the total number of items to migrate (domains + accounts + emails).
    /// </summary>
    [JsonIgnore]
    public int TotalItems => TotalDomains + TotalAccounts + TotalEmails;

    /// <summary>
    /// Gets the number of successfully migrated items.
    /// </summary>
    [JsonIgnore]
    public int MigratedItems => MigratedDomains + MigratedAccounts + MigratedEmails;

    /// <summary>
    /// Gets the number of failed items.
    /// </summary>
    [JsonIgnore]
    public int FailedItems => FailedDomains + FailedAccounts + FailedEmails;

    /// <summary>
    /// Gets the overall success rate as a percentage.
    /// </summary>
    [JsonIgnore]
    public double SuccessRate
    {
        get
        {
            if (TotalItems == 0)
            {
                return 0;
            }
            return (double)MigratedItems / TotalItems * 100;
        }
    }

    /// <summary>
    /// Initializes a new instance of the MigrationState class.
    /// </summary>
    public MigrationState()
    { }

    /// <summary>
    /// Updates the last updated timestamp.
    /// </summary>
    public void Touch()
    {
        LastUpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the state with the current domain being processed.
    /// </summary>
    /// <param name="domainName">The domain name.</param>
    /// <param name="phase">The current phase.</param>
    /// <param name="task">The current task.</param>
    public void UpdateCurrent(string domainName, string phase, string task)
    {
        CurrentPhase = phase;
        CurrentTask = task;
        Touch();
    }

    /// <summary>
    /// Marks a domain as successfully migrated.
    /// </summary>
    /// <param name="domainName">The domain name.</param>
    public void MarkDomainSuccess(string domainName)
    {
        if (!MigratedDomainsList.Contains(domainName))
        {
            MigratedDomainsList.Add(domainName);
            MigratedDomains++;
        }
        FailedDomainsList.Remove(domainName);
        Touch();
    }

    /// <summary>
    /// Marks a domain as failed.
    /// </summary>
    /// <param name="domainName">The domain name.</param>
    /// <param name="error">The error information.</param>
    public void MarkDomainFailed(string domainName, MigrationError error)
    {
        if (!FailedDomainsList.Contains(domainName))
        {
            FailedDomainsList.Add(domainName);
            FailedDomains++;
        }
        MigratedDomainsList.Remove(domainName);
        Errors.Add(error);
        Touch();
    }

    /// <summary>
    /// Marks an account as successfully migrated.
    /// </summary>
    public void MarkAccountSuccess()
    {
        MigratedAccounts++;
        Touch();
    }

    /// <summary>
    /// Marks an account as failed.
    /// </summary>
    /// <param name="error">The error information.</param>
    public void MarkAccountFailed(MigrationError error)
    {
        FailedAccounts++;
        Errors.Add(error);
        Touch();
    }

    /// <summary>
    /// Marks an email as successfully migrated.
    /// </summary>
    /// <param name="size">The size of the email in bytes.</param>
    public void MarkEmailSuccess(long size)
    {
        MigratedEmails++;
        MigratedBytes += size;
        Touch();
    }

    /// <summary>
    /// Marks an email as failed.
    /// </summary>
    /// <param name="error">The error information.</param>
    public void MarkEmailFailed(MigrationError error)
    {
        FailedEmails++;
        Errors.Add(error);
        Touch();
    }

    /// <summary>
    /// Updates the progress percentage.
    /// </summary>
    public void UpdateProgress()
    {
        if (TotalItems > 0)
        {
            ProgressPercentage = (int)((double)MigratedItems / TotalItems * 100);
        }
        else
        {
            ProgressPercentage = 0;
        }
        Touch();
    }

    /// <summary>
    /// Pauses the migration.
    /// </summary>
    public void Pause()
    {
        Status = MigrationStatus.Paused;
        Touch();
    }

    /// <summary>
    /// Resumes the migration.
    /// </summary>
    public void Resume()
    {
        Status = MigrationStatus.InProgress;
        Touch();
    }

    /// <summary>
    /// Marks the migration as completed.
    /// </summary>
    public void Complete()
    {
        Status = MigrationStatus.Completed;
        ProgressPercentage = 100;
        LastUpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Marks the migration as failed.
    /// </summary>
    /// <param name="error">The error that caused the failure.</param>
    public void Fail(MigrationError error)
    {
        Status = MigrationStatus.Failed;
        Errors.Add(error);
        LastUpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Adds a custom metadata entry.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    public void AddMetadata(string key, object value)
    {
        Metadata[key] = value;
        Touch();
    }

    /// <summary>
    /// Gets a metadata value by key.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The metadata key.</param>
    /// <returns>The value if found; otherwise, default.</returns>
    public T? GetMetadata<T>(string key)
    {
        if (Metadata.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }
}

/// <summary>
/// Represents the status of a migration.
/// </summary>
public enum MigrationStatus
{
    /// <summary>
    /// The migration has not started.
    /// </summary>
    NotStarted = 0,

    /// <summary>
    /// The migration is currently in progress.
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// The migration has been paused.
    /// </summary>
    Paused = 2,

    /// <summary>
    /// The migration has been completed successfully.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// The migration has failed.
    /// </summary>
    Failed = 4,

    /// <summary>
    /// The migration has been cancelled.
    /// </summary>
    Cancelled = 5
}

/// <summary>
/// Represents a checkpoint for resumable migrations.
/// </summary>
public class MigrationCheckpoint
{
    /// <summary>
    /// Gets or sets the identifier for the checkpoint.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the checkpoint was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the last processed domain.
    /// </summary>
    [JsonPropertyName("lastProcessedDomain")]
    public string? LastProcessedDomain { get; set; }

    /// <summary>
    /// Gets or sets the last processed account within the domain.
    /// </summary>
    [JsonPropertyName("lastProcessedAccount")]
    public string? LastProcessedAccount { get; set; }

    /// <summary>
    /// Gets or sets the last processed message ID.
    /// </summary>
    [JsonPropertyName("lastProcessedMessageId")]
    public string? LastProcessedMessageId { get; set; }

    /// <summary>
    /// Gets or sets the offset for batch processing.
    /// </summary>
    [JsonPropertyName("batchOffset")]
    public int BatchOffset { get; set; }

    /// <summary>
    /// Gets or sets the size of each batch.
    /// </summary>
    [JsonPropertyName("batchSize")]
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets custom checkpoint data.
    /// </summary>
    [JsonPropertyName("data")]
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>
    /// Initializes a new instance of the MigrationCheckpoint class.
    /// </summary>
    public MigrationCheckpoint()
    { }

    /// <summary>
    /// Updates the checkpoint with the current position.
    /// </summary>
    /// <param name="domain">The last processed domain.</param>
    /// <param name="account">The last processed account.</param>
    /// <param name="messageId">The last processed message ID.</param>
    public void UpdatePosition(string? domain, string? account, string? messageId)
    {
        LastProcessedDomain = domain;
        LastProcessedAccount = account;
        LastProcessedMessageId = messageId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Advances the batch offset.
    /// </summary>
    public void AdvanceBatch()
    {
        BatchOffset += BatchSize;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Resets the batch offset.
    /// </summary>
    public void ResetBatch()
    {
        BatchOffset = 0;
    }
}

/// <summary>
/// Represents an error that occurred during migration.
/// </summary>
public class MigrationError
{
    /// <summary>
    /// Gets or sets the unique identifier for the error.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the error occurred.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// Gets or sets the severity of the error.
    /// </summary>
    [JsonPropertyName("severity")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ErrorSeverity Severity { get; set; } = ErrorSeverity.Error;

    /// <summary>
    /// Gets or sets the context in which the error occurred.
    /// </summary>
    [JsonPropertyName("context")]
    public Dictionary<string, object> Context { get; set; } = new();

    /// <summary>
    /// Gets or sets the stack trace.
    /// </summary>
    [JsonPropertyName("stackTrace")]
    public string? StackTrace { get; set; }

    /// <summary>
    /// Gets or sets the inner error.
    /// </summary>
    [JsonPropertyName("innerError")]
    public MigrationError? InnerError { get; set; }

    /// <summary>
    /// Initializes a new instance of the MigrationError class.
    /// </summary>
    public MigrationError()
    { }

    /// <summary>
    /// Initializes a new instance of the MigrationError class with the specified message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public MigrationError(string message)
    {
        Message = message;
    }

    /// <summary>
    /// Initializes a new instance of the MigrationError class with the specified message and exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="exception">The exception that caused the error.</param>
    public MigrationError(string message, Exception exception)
        : this(message)
    {
        Code = exception.GetType().Name;
        StackTrace = exception.ToString();

        if (exception.InnerException != null)
        {
            InnerError = new MigrationError(exception.InnerException.Message, exception.InnerException);
        }
    }

    /// <summary>
    /// Creates a MigrationError from an exception.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>A MigrationError representing the exception.</returns>
    public static MigrationError FromException(Exception exception)
    {
        return new MigrationError(exception.Message, exception);
    }

    /// <summary>
    /// Adds context information to the error.
    /// </summary>
    /// <param name="key">The context key.</param>
    /// <param name="value">The context value.</param>
    public void AddContext(string key, object value)
    {
        Context[key] = value;
    }
}

/// <summary>
/// Represents the severity of an error.
/// </summary>
public enum ErrorSeverity
{
    /// <summary>
    /// Information level.
    /// </summary>
    Info = 0,

    /// <summary>
    /// Warning level.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Error level.
    /// </summary>
    Error = 2,

    /// <summary>
    /// Critical error level.
    /// </summary>
    Critical = 3
}
