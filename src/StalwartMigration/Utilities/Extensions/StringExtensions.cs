// <copyright file="StringExtensions.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.Text.RegularExpressions;

namespace StalwartMigration.Utilities.Extensions;

/// <summary>
/// Extension methods for string operations.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Checks if a string is null, empty, or consists only of whitespace.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <returns>True if the string is null, empty, or whitespace; otherwise, false.</returns>
    public static bool IsNullOrWhiteSpace(this string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Checks if a string is null or empty.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <returns>True if the string is null or empty; otherwise, false.</returns>
    public static bool IsNullOrEmpty(this string? value)
    {
        return string.IsNullOrEmpty(value);
    }

    /// <summary>
    /// Throws an ArgumentException if the string is null, empty, or whitespace.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="paramName">The name of the parameter.</param>
    /// <param name="message">Optional error message.</param>
    /// <exception cref="ArgumentException">Thrown when the string is null, empty, or whitespace.</exception>
    public static void ThrowIfNullOrWhiteSpace(this string? value, string paramName, string? message = null)
    {
        if (value.IsNullOrWhiteSpace())
        {
            throw new ArgumentException(message ?? "Value cannot be null or whitespace.", paramName);
        }
    }

    /// <summary>
    /// Throws an ArgumentNullException if the string is null.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="paramName">The name of the parameter.</param>
    /// <exception cref="ArgumentNullException">Thrown when the string is null.</exception>
    public static void ThrowIfNull(this string? value, string paramName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(paramName);
        }
    }

    /// <summary>
    /// Truncates a string to a maximum length.
    /// </summary>
    /// <param name="value">The string to truncate.</param>
    /// <param name="maxLength">The maximum length of the resulting string.</param>
    /// <returns>The truncated string, or the original string if it's shorter than maxLength.</returns>
    public static string Truncate(this string value, int maxLength)
    {
        if (value == null)
        {
            return string.Empty;
        }

        if (value.Length <= maxLength)
        {
            return value;
        }

        return value.Substring(0, maxLength);
    }

    /// <summary>
    /// Truncates a string and adds an ellipsis if it was truncated.
    /// </summary>
    /// <param name="value">The string to truncate.</param>
    /// <param name="maxLength">The maximum length of the resulting string (including ellipsis).</param>
    /// <returns>The truncated string with ellipsis, or the original string if it's shorter than maxLength.</returns>
    public static string TruncateWithEllipsis(this string value, int maxLength)
    {
        if (value == null)
        {
            return string.Empty;
        }

        if (value.Length <= maxLength)
        {
            return value;
        }

        if (maxLength <= 3)
        {
            return value.Substring(0, maxLength);
        }

        return value.Substring(0, maxLength - 3) + "...";
    }

    /// <summary>
    /// Converts a string to PascalCase.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>The PascalCase string.</returns>
    public static string ToPascalCase(this string value)
    {
        if (value.IsNullOrEmpty())
        {
            return value ?? string.Empty;
        }

        var parts = value.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var result = string.Join(string.Empty, parts.Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1).ToLowerInvariant()));
        return result;
    }

    /// <summary>
    /// Converts a string to camelCase.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>The camelCase string.</returns>
    public static string ToCamelCase(this string value)
    {
        if (value.IsNullOrEmpty())
        {
            return value ?? string.Empty;
        }

        var pascalCase = value.ToPascalCase();
        return char.ToLowerInvariant(pascalCase[0]) + pascalCase.Substring(1);
    }

    /// <summary>
    /// Checks if the string contains any of the specified values.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="values">The values to search for.</param>
    /// <param name="comparison">The string comparison type.</param>
    /// <returns>True if the string contains any of the values; otherwise, false.</returns>
    public static bool ContainsAny(this string value, IEnumerable<string> values, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        if (value.IsNullOrEmpty() || values == null)
        {
            return false;
        }

        return values.Any(v => value.IndexOf(v, comparison) >= 0);
    }

    /// <summary>
    /// Checks if the string starts with any of the specified values.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="values">The values to search for.</param>
    /// <param name="comparison">The string comparison type.</param>
    /// <returns>True if the string starts with any of the values; otherwise, false.</returns>
    public static bool StartsWithAny(this string value, IEnumerable<string> values, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        if (value.IsNullOrEmpty() || values == null)
        {
            return false;
        }

        return values.Any(v => value.StartsWith(v, comparison));
    }

    /// <summary>
    /// Checks if the string ends with any of the specified values.
    /// </summary>
    /// <param name="value">The string to check.</param>
    /// <param name="values">The values to search for.</param>
    /// <param name="comparison">The string comparison type.</param>
    /// <returns>True if the string ends with any of the values; otherwise, false.</returns>
    public static bool EndsWithAny(this string value, IEnumerable<string> values, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        if (value.IsNullOrEmpty() || values == null)
        {
            return false;
        }

        return values.Any(v => value.EndsWith(v, comparison));
    }
}
