// <copyright file="DomainValidator.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.Text.RegularExpressions;

namespace StalwartMigration.Utilities.Helpers;

/// <summary>
/// Provides validation for domain names.
/// </summary>
public static class DomainValidator
{
    // Domain regex: allows letters, digits, hyphens, and dots
    // Each label must be 1-63 characters, and the entire domain must be 1-253 characters
    private static readonly Regex DomainRegex = new Regex(
        @"^(?!:\/\/)(?:(?:[a-zA-Z0-9][a-zA-Z0-9-]{0,61}[a-zA-Z0-9])\.)+[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Maximum length for a domain name.
    /// </summary>
    public const int MaxDomainLength = 253;

    /// <summary>
    /// Maximum length for a single label in a domain name.
    /// </summary>
    public const int MaxLabelLength = 63;

    /// <summary>
    /// Minimum length for a TLD (top-level domain).
    /// </summary>
    public const int MinTldLength = 2;

    /// <summary>
    /// Validates a domain name.
    /// </summary>
    /// <param name="domain">The domain name to validate.</param>
    /// <returns>True if the domain name is valid; otherwise, false.</returns>
    public static bool IsValid(string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return false;
        }

        // Remove any leading or trailing whitespace
        domain = domain.Trim();

        // Check length constraints
        if (domain.Length > MaxDomainLength || domain.Length < 3)
        {
            return false;
        }

        // Check for invalid characters
        if (domain.Contains("..") || domain.StartsWith("-") || domain.EndsWith("-") ||
            domain.StartsWith(".") || domain.EndsWith("."))
        {
            return false;
        }

        // Validate using regex
        return DomainRegex.IsMatch(domain);
    }

    /// <summary>
    /// Validates a domain name and throws an exception if invalid.
    /// </summary>
    /// <param name="domain">The domain name to validate.</param>
    /// <param name="paramName">The name of the parameter.</param>
    /// <exception cref="ArgumentException">Thrown when the domain name is invalid.</exception>
    public static void Validate(string? domain, string paramName)
    {
        if (!IsValid(domain))
        {
            throw new ArgumentException("Invalid domain name format.", paramName);
        }
    }

    /// <summary>
    /// Normalizes a domain name by trimming whitespace and converting to lowercase.
    /// </summary>
    /// <param name="domain">The domain name to normalize.</param>
    /// <returns>The normalized domain name.</returns>
    public static string Normalize(string domain)
    {
        if (domain == null)
        {
            return string.Empty;
        }

        return domain.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Extracts the TLD (top-level domain) from a domain name.
    /// </summary>
    /// <param name="domain">The domain name.</param>
    /// <returns>The TLD, or empty string if the domain is invalid.</returns>
    public static string ExtractTld(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return string.Empty;
        }

        var parts = domain.Split('.');
        if (parts.Length < 2)
        {
            return string.Empty;
        }

        return parts[^1];
    }

    /// <summary>
    /// Extracts the subdomain from a domain name.
    /// </summary>
    /// <param name="domain">The domain name.</param>
    /// <returns>The subdomain (everything except the TLD), or empty string if invalid.</returns>
    public static string ExtractSubdomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return string.Empty;
        }

        var parts = domain.Split('.');
        if (parts.Length < 2)
        {
            return string.Empty;
        }

        return string.Join(".", parts.Take(parts.Length - 1));
    }

    /// <summary>
    /// Checks if a domain is a subdomain of another domain.
    /// </summary>
    /// <param name="subdomain">The potential subdomain.</param>
    /// <param name="parentDomain">The parent domain to check against.</param>
    /// <returns>True if subdomain is a subdomain of parentDomain; otherwise, false.</returns>
    public static bool IsSubdomainOf(string subdomain, string parentDomain)
    {
        if (string.IsNullOrWhiteSpace(subdomain) || string.IsNullOrWhiteSpace(parentDomain))
        {
            return false;
        }

        subdomain = subdomain.ToLowerInvariant();
        parentDomain = parentDomain.ToLowerInvariant();

        // Exact match is not a subdomain
        if (subdomain == parentDomain)
        {
            return false;
        }

        // Check if subdomain ends with parentDomain
        if (!subdomain.EndsWith("." + parentDomain))
        {
            return false;
        }

        // Ensure we have at least one label before the parent domain
        var subdomainParts = subdomain.Split('.');
        var parentParts = parentDomain.Split('.');

        if (subdomainParts.Length <= parentParts.Length)
        {
            return false;
        }

        // Check that the last N parts match the parent domain
        for (int i = 0; i < parentParts.Length; i++)
        {
            if (subdomainParts[^(i + 1)] != parentParts[^(i + 1)])
            {
                return false;
            }
        }

        return true;
    }
}
