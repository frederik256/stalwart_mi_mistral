// <copyright file="EmailValidator.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.Text.RegularExpressions;

namespace StalwartMigration.Utilities.Helpers;

/// <summary>
/// Provides validation for email addresses.
/// </summary>
public static class EmailValidator
{
    // RFC 5322 compliant email regex (simplified but practical)
    private static readonly Regex EmailRegex = new Regex(
        @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Maximum length for an email address (RFC 5321).
    /// </summary>
    public const int MaxEmailLength = 254;

    /// <summary>
    /// Maximum length for the local part of an email address.
    /// </summary>
    public const int MaxLocalPartLength = 64;

    /// <summary>
    /// Validates an email address.
    /// </summary>
    /// <param name="email">The email address to validate.</param>
    /// <returns>True if the email address is valid; otherwise, false.</returns>
    public static bool IsValid(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        // Check length constraints
        if (email.Length > MaxEmailLength)
        {
            return false;
        }

        // Split into local and domain parts
        var parts = email.Split('@');
        if (parts.Length != 2)
        {
            return false;
        }

        var localPart = parts[0];
        var domainPart = parts[1];

        // Check local part length
        if (localPart.Length > MaxLocalPartLength)
        {
            return false;
        }

        // Check domain part is not empty
        if (string.IsNullOrEmpty(domainPart))
        {
            return false;
        }

        // Validate using regex
        return EmailRegex.IsMatch(email);
    }

    /// <summary>
    /// Validates an email address and throws an exception if invalid.
    /// </summary>
    /// <param name="email">The email address to validate.</param>
    /// <param name="paramName">The name of the parameter.</param>
    /// <exception cref="ArgumentException">Thrown when the email address is invalid.</exception>
    public static void Validate(string? email, string paramName)
    {
        if (!IsValid(email))
        {
            throw new ArgumentException("Invalid email address format.", paramName);
        }
    }

    /// <summary>
    /// Normalizes an email address by trimming whitespace and converting to lowercase.
    /// </summary>
    /// <param name="email">The email address to normalize.</param>
    /// <returns>The normalized email address.</returns>
    public static string Normalize(string email)
    {
        if (email == null)
        {
            return string.Empty;
        }

        return email.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Extracts the domain from an email address.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <returns>The domain part of the email address, or empty string if invalid.</returns>
    public static string ExtractDomain(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return string.Empty;
        }

        var atIndex = email.IndexOf('@');
        if (atIndex < 0 || atIndex == email.Length - 1)
        {
            return string.Empty;
        }

        return email.Substring(atIndex + 1);
    }

    /// <summary>
    /// Extracts the local part from an email address.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <returns>The local part of the email address, or empty string if invalid.</returns>
    public static string ExtractLocalPart(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return string.Empty;
        }

        var atIndex = email.IndexOf('@');
        if (atIndex < 0)
        {
            return string.Empty;
        }

        return email.Substring(0, atIndex);
    }
}
