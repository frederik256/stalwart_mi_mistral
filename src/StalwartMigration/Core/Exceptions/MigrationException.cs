// <copyright file="MigrationException.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.Runtime.Serialization;

namespace StalwartMigration.Core.Exceptions;

/// <summary>
/// Base exception for all migration-related errors.
/// </summary>
public class MigrationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the MigrationException class.
    /// </summary>
    public MigrationException()
    { }

    /// <summary>
    /// Initializes a new instance of the MigrationException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public MigrationException(string message)
        : base(message)
    { }

    /// <summary>
    /// Initializes a new instance of the MigrationException class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public MigrationException(string message, Exception innerException)
        : base(message, innerException)
    { }

    /// <summary>
    /// Initializes a new instance of the MigrationException class with serialized data.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    protected MigrationException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    { }

    /// <summary>
    /// Gets additional context information about the migration error.
    /// </summary>
    public virtual string? Context { get; protected set; }

    /// <summary>
    /// Gets remediation suggestions for the error.
    /// </summary>
    public virtual string? Remediation { get; protected set; }
}
