// <copyright file="ConnectionException.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

namespace StalwartMigration.Core.Exceptions;

/// <summary>
/// Exception thrown when there is a connection failure.
/// </summary>
public class ConnectionException : MigrationException
{
    /// <summary>
    /// Initializes a new instance of the ConnectionException class.
    /// </summary>
    public ConnectionException()
    { }

    /// <summary>
    /// Initializes a new instance of the ConnectionException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ConnectionException(string message)
        : base(message)
    { }

    /// <summary>
    /// Initializes a new instance of the ConnectionException class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ConnectionException(string message, Exception innerException)
        : base(message, innerException)
    { }

    /// <summary>
    /// Creates a ConnectionException with context and remediation information.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="context">Additional context about the connection error.</param>
    /// <param name="remediation">Suggestions for how to fix the connection issue.</param>
    public ConnectionException(string message, string context, string remediation)
        : base(message)
    {
        Context = context;
        Remediation = remediation;
    }

    /// <summary>
    /// Gets the target URL or endpoint that failed to connect.
    /// </summary>
    public string? TargetEndpoint { get; private set; }

    /// <summary>
    /// Gets the timeout duration if the connection timed out.
    /// </summary>
    public TimeSpan? Timeout { get; private set; }

    /// <summary>
    /// Creates a ConnectionException for a connection timeout.
    /// </summary>
    /// <param name="endpoint">The endpoint that timed out.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>A ConnectionException for the timeout.</returns>
    public static ConnectionException ForTimeout(string endpoint, TimeSpan timeout)
    {
        var exception = new ConnectionException(
            $"Connection to '{endpoint}' timed out after {timeout.TotalSeconds} seconds.",
            "Connection Timeout",
            $"Check your network connection and verify the endpoint '{endpoint}' is accessible. Consider increasing the timeout value.");
        
        exception.TargetEndpoint = endpoint;
        exception.Timeout = timeout;
        return exception;
    }

    /// <summary>
    /// Creates a ConnectionException for an unreachable endpoint.
    /// </summary>
    /// <param name="endpoint">The endpoint that is unreachable.</param>
    /// <returns>A ConnectionException for the unreachable endpoint.</returns>
    public static ConnectionException ForUnreachable(string endpoint)
    {
        var exception = new ConnectionException(
            $"Could not connect to '{endpoint}'. The endpoint may be down or unreachable.",
            "Connection Failed",
            $"Verify that the endpoint '{endpoint}' is running and accessible from your network. Check firewall settings and network connectivity.");
        
        exception.TargetEndpoint = endpoint;
        return exception;
    }

    /// <summary>
    /// Creates a ConnectionException for a refused connection.
    /// </summary>
    /// <param name="endpoint">The endpoint that refused the connection.</param>
    /// <returns>A ConnectionException for the refused connection.</returns>
    public static ConnectionException ForConnectionRefused(string endpoint)
    {
        var exception = new ConnectionException(
            $"Connection to '{endpoint}' was refused. The server may not be accepting connections.",
            "Connection Refused",
            $"Verify that the service at '{endpoint}' is running and configured to accept connections. Check if the server has reached its connection limit.");
        
        exception.TargetEndpoint = endpoint;
        return exception;
    }
}
