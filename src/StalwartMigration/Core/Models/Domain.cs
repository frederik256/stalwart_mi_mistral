// <copyright file="Domain.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using StalwartMigration.Utilities.Helpers;

namespace StalwartMigration.Core.Models;

/// <summary>
/// Represents a domain in the mail server.
/// </summary>
public class Domain : IEquatable<Domain>, IValidatableObject
{
    /// <summary>
    /// Gets or sets the unique identifier for the domain.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the domain name (e.g., "example.com").
    /// </summary>
    [JsonPropertyName("name")]
    [Required(ErrorMessage = "Domain name is required.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name for the domain.
    /// </summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the description for the domain.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of accounts for this domain.
    /// Null means unlimited.
    /// </summary>
    [JsonPropertyName("maxAccounts")]
    [Range(0, int.MaxValue, ErrorMessage = "Max accounts must be a positive number.")]
    public int? MaxAccounts { get; set; }

    /// <summary>
    /// Gets or sets the total storage quota for this domain in bytes.
    /// Null means unlimited.
    /// </summary>
    [JsonPropertyName("quota")]
    [Range(0, long.MaxValue, ErrorMessage = "Quota must be a positive number.")]
    public long? Quota { get; set; }

    /// <summary>
    /// Gets or sets the used storage for this domain in bytes.
    /// </summary>
    [JsonPropertyName("usedQuota")]
    [Range(0, long.MaxValue, ErrorMessage = "Used quota must be a positive number.")]
    public long UsedQuota { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the domain was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the domain was last updated.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets whether the domain is enabled.
    /// </summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the DNS MX record priority.
    /// </summary>
    [JsonPropertyName("mxPriority")]
    [Range(0, 65535, ErrorMessage = "MX priority must be between 0 and 65535.")]
    public int MxPriority { get; set; } = 10;

    /// <summary>
    /// Gets or sets whether DKIM signing is enabled for this domain.
    /// </summary>
    [JsonPropertyName("dkimEnabled")]
    public bool DkimEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether SPF checking is enabled for this domain.
    /// </summary>
    [JsonPropertyName("spfEnabled")]
    public bool SpfEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether DMARC checking is enabled for this domain.
    /// </summary>
    [JsonPropertyName("dmarcEnabled")]
    public bool DmarcEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether catch-all email is enabled for this domain.
    /// </summary>
    [JsonPropertyName("catchAllEnabled")]
    public bool CatchAllEnabled { get; set; }

    /// <summary>
    /// Gets or sets the catch-all email address.
    /// Only used if CatchAllEnabled is true.
    /// </summary>
    [JsonPropertyName("catchAllEmail")]
    public string? CatchAllEmail { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this domain is the default domain.
    /// </summary>
    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets the quota usage percentage (0-100).
    /// </summary>
    [JsonIgnore]
    public double QuotaUsagePercentage
    {
        get
        {
            if (!Quota.HasValue || Quota.Value == 0)
            {
                return 0;
            }
            return (double)UsedQuota / Quota.Value * 100;
        }
    }

    /// <summary>
    /// Gets the available quota in bytes.
    /// </summary>
    [JsonIgnore]
    public long AvailableQuota
    {
        get
        {
            if (!Quota.HasValue)
            {
                return long.MaxValue;
            }
            return Math.Max(0, Quota.Value - UsedQuota);
        }
    }

    /// <summary>
    /// Initializes a new instance of the Domain class.
    /// </summary>
    public Domain()
    { }

    /// <summary>
    /// Initializes a new instance of the Domain class with the specified name.
    /// </summary>
    /// <param name="name">The domain name.</param>
    public Domain(string name)
    {
        Name = DomainValidator.Normalize(name);
    }

    /// <summary>
    /// Validates the domain model.
    /// </summary>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>A collection of validation results.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Validate domain name
        if (!DomainValidator.IsValid(Name))
        {
            yield return new ValidationResult(
                "Invalid domain name format.",
                [nameof(Name)]);
        }

        // Validate catch-all email if enabled
        if (CatchAllEnabled && !string.IsNullOrWhiteSpace(CatchAllEmail))
        {
            if (!EmailValidator.IsValid(CatchAllEmail))
            {
                yield return new ValidationResult(
                    "Invalid catch-all email address.",
                    [nameof(CatchAllEmail)]);
            }
        }

        // Validate quota
        if (Quota.HasValue && Quota.Value < 0)
        {
            yield return new ValidationResult(
                "Quota cannot be negative.",
                [nameof(Quota)]);
        }

        // Validate used quota
        if (UsedQuota < 0)
        {
            yield return new ValidationResult(
                "Used quota cannot be negative.",
                [nameof(UsedQuota)]);
        }

        // Validate used quota doesn't exceed quota
        if (Quota.HasValue && UsedQuota > Quota.Value)
        {
            yield return new ValidationResult(
                "Used quota cannot exceed total quota.",
                [nameof(UsedQuota)]);
        }
    }

    /// <summary>
    /// Validates the domain and throws an exception if invalid.
    /// </summary>
    public void ValidateAndThrow()
    {
        var validationResults = Validate(new ValidationContext(this)).ToList();

        if (validationResults.Count > 0)
        {
            throw new ValidationException(string.Join("\n", validationResults.Select(r => r.ErrorMessage)));
        }
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    public bool Equals(Domain? other)
    {
        if (other == null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return Equals(obj as Domain);
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    public override int GetHashCode()
    {
        return Name.ToLowerInvariant().GetHashCode();
    }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    public override string ToString()
    {
        return Name;
    }
}
