// <copyright file="SensitiveDataFilter.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace StalwartMigration.Utilities.Logging;

/// <summary>
/// Provides utilities for filtering sensitive data from log messages.
/// </summary>
public static class SensitiveDataFilter
{
    private static readonly Regex[] SensitivePatterns = new Regex[]
    {
        // Passwords and secrets in key=value format
        new Regex("(password|passwd|pwd|secret|token|apikey|api_key|access_key|auth|credential)=[^\\s]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex("(password|passwd|pwd|secret|token|apikey|api_key|access_key|auth|credential):[^\\s]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        
        // Connection strings
        new Regex("(connectionstring|connection_string|connstr)=[^\\s]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        
        // Bearer tokens
        new Regex("Bearer [a-zA-Z0-9\\-_]+\\.[a-zA-Z0-9\\-_]+\\.[a-zA-Z0-9\\-_]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        
        // Private keys
        new Regex("-----BEGIN[^ ]+(RSA[^ ]+)?PRIVATE[^ ]+KEY-----", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        
        // AWS access keys
        new Regex("AKIA[0-9A-Z]{16}", RegexOptions.Compiled),
        
        // Generic secrets
        new Regex("(secret|private)=[^\\s]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    };

    private const string RedactedValue = "[REDACTED]";

    /// <summary>
    /// Filters sensitive data from a log message.
    /// </summary>
    /// <param name="message">The original message.</param>
    /// <returns>The filtered message with sensitive data redacted.</returns>
    public static string FilterSensitiveData(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return message;
        }

        string filtered = message;
        
        foreach (var pattern in SensitivePatterns)
        {
            filtered = pattern.Replace(filtered, match =>
            {
                // If the match contains an equals sign, keep the key but redact the value
                int equalsIndex = match.Value.IndexOf('=');
                if (equalsIndex >= 0)
                {
                    string key = match.Value.Substring(0, equalsIndex + 1);
                    return key + RedactedValue;
                }
                
                // For colon-separated, keep the key
                int colonIndex = match.Value.IndexOf(':');
                if (colonIndex >= 0)
                {
                    string key = match.Value.Substring(0, colonIndex + 1);
                    return key + RedactedValue;
                }
                
                // Otherwise, just redact the entire match
                return RedactedValue;
            });
        }

        return filtered;
    }

}
