// <copyright file="ConfigurationException.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.Runtime.Serialization;

namespace StalwartMigration.Core.Exceptions;

/// <summary>
/// Exception thrown when there is a configuration error.
/// </summary>
public class ConfigurationException : MigrationException
{
    /// <summary>
    /// Initializes a new instance of the ConfigurationException class.
    /// </summary>
    public ConfigurationException()
    { }

    /// <summary>
    /// Initializes a new instance of the ConfigurationException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ConfigurationException(string message)
        : base(message)
    { }

    /// <summary>
    /// Initializes a new instance of the ConfigurationException class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    { }

    /// <summary>
    /// Initializes a new instance of the ConfigurationException class with serialized data.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    protected ConfigurationException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    { }

    /// <summary>
    /// Creates a ConfigurationException with context and remediation information.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="context">Additional context about the configuration error.</param>
    /// <param name="remediation">Suggestions for how to fix the configuration issue.</param>
    public ConfigurationException(string message, string context, string remediation)
        : base(message)
    {
        Context = context;
        Remediation = remediation;
    }

    /// <summary>
    /// Creates a ConfigurationException with context and remediation information, and an inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="context">Additional context about the configuration error.</param>
    /// <param name="remediation">Suggestions for how to fix the configuration issue.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ConfigurationException(string message, string context, string remediation, Exception innerException)
        : base(message, innerException)
    {
        Context = context;
        Remediation = remediation;
    }

    /// <summary>
    /// Gets the configuration key that caused the error, if applicable.
    /// </summary>
    public string? ConfigurationKey { get; private set; }

    /// <summary>
    /// Creates a ConfigurationException for a missing configuration value.
    /// </summary>
    /// <param name="key">The missing configuration key.</param>
    /// <returns>A ConfigurationException for the missing key.</returns>
    public static ConfigurationException ForMissingKey(string key)
    {
        return new ConfigurationException(
            $"Required configuration key '{key}' is missing or empty.",
            "Configuration Validation",
            $"Please add the '{key}' setting to your configuration file.");
    }

    /// <summary>
    /// Creates a ConfigurationException for an invalid configuration value.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The invalid value.</param>
    /// <param name="expectedFormat">Description of the expected format.</param>
    /// <returns>A ConfigurationException for the invalid value.</returns>
    public static ConfigurationException ForInvalidValue(string key, string value, string expectedFormat)
    {
        return new ConfigurationException(
            $"Invalid value '{value}' for configuration key '{key}'. Expected: {expectedFormat}",
            "Configuration Validation",
            $"Please provide a valid value for '{key}' matching the format: {expectedFormat}");
    }
}
