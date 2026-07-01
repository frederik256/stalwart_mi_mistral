// <copyright file="AuthenticationException.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

namespace StalwartMigration.Core.Exceptions;

/// <summary>
/// Exception thrown when authentication fails.
/// </summary>
public class AuthenticationException : MigrationException
{
    /// <summary>
    /// Initializes a new instance of the AuthenticationException class.
    /// </summary>
    public AuthenticationException()
    { }

    /// <summary>
    /// Initializes a new instance of the AuthenticationException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public AuthenticationException(string message)
        : base(message)
    { }

    /// <summary>
    /// Initializes a new instance of the AuthenticationException class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public AuthenticationException(string message, Exception innerException)
        : base(message, innerException)
    { }

    /// <summary>
    /// Creates an AuthenticationException with context and remediation information.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="context">Additional context about the authentication error.</param>
    /// <param name="remediation">Suggestions for how to fix the authentication issue.</param>
    public AuthenticationException(string message, string context, string remediation)
        : base(message)
    {
        Context = context;
        Remediation = remediation;
    }

    /// <summary>
    /// Gets the username or identifier used for authentication, if applicable.
    /// </summary>
    public string? Username { get; private set; }

    /// <summary>
    /// Gets the authentication method that failed, if applicable.
    /// </summary>
    public string? AuthenticationMethod { get; private set; }

    /// <summary>
    /// Creates an AuthenticationException for invalid credentials.
    /// </summary>
    /// <param name="username">The username that failed authentication.</param>
    /// <returns>An AuthenticationException for invalid credentials.</returns>
    public static AuthenticationException ForInvalidCredentials(string? username = null)
    {
        var message = username != null
            ? $"Authentication failed for user '{username}'. Invalid username or password."
            : "Authentication failed. Invalid username or password.";
        
        var exception = new AuthenticationException(
            message,
            "Authentication Failed",
            "Please verify your username and password and try again. If you've forgotten your password, you may need to reset it.");
        
        exception.Username = username;
        exception.AuthenticationMethod = "Password";
        return exception;
    }

    /// <summary>
    /// Creates an AuthenticationException for an expired token.
    /// </summary>
    /// <param name="tokenType">The type of token that expired.</param>
    /// <returns>An AuthenticationException for the expired token.</returns>
    public static AuthenticationException ForExpiredToken(string tokenType = "session")
    {
        var exception = new AuthenticationException(
            $"Authentication failed: {tokenType} token has expired.",
            "Token Expired",
            $"Please obtain a new {tokenType} token and try again.");
        
        exception.AuthenticationMethod = tokenType + " Token";
        return exception;
    }

    /// <summary>
    /// Creates an AuthenticationException for insufficient permissions.
    /// </summary>
    /// <param name="username">The username that lacks permissions.</param>
    /// <param name="requiredPermission">The required permission that is missing.</param>
    /// <returns>An AuthenticationException for insufficient permissions.</returns>
    public static AuthenticationException ForInsufficientPermissions(string? username, string requiredPermission)
    {
        var message = username != null
            ? $"User '{username}' does not have permission: {requiredPermission}."
            : $"Current user does not have permission: {requiredPermission}.";
        
        var exception = new AuthenticationException(
            message,
            "Permission Denied",
            $"Please ensure the user has the '{requiredPermission}' permission. You may need to contact an administrator.");
        
        exception.Username = username;
        exception.AuthenticationMethod = "Permission Check";
        return exception;
    }

    /// <summary>
    /// Creates an AuthenticationException for an unsupported authentication method.
    /// </summary>
    /// <param name="method">The unsupported authentication method.</param>
    /// <returns>An AuthenticationException for the unsupported method.</returns>
    public static AuthenticationException ForUnsupportedMethod(string method)
    {
        var exception = new AuthenticationException(
            $"Authentication method '{method}' is not supported.",
            "Unsupported Authentication Method",
            $"Please use one of the supported authentication methods. Refer to the documentation for available options.");
        
        exception.AuthenticationMethod = method;
        return exception;
    }
}
