// <copyright file="Account.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using StalwartMigration.Utilities.Helpers;

namespace StalwartMigration.Core.Models;

/// <summary>
/// Represents a user account in the mail server.
/// </summary>
public class Account : IEquatable<Account>, IValidatableObject
{
    /// <summary>
    /// Gets or sets the unique identifier for the account.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the account name (e.g., "john.doe" or "john.doe@example.com").
    /// </summary>
    [JsonPropertyName("name")]
    [Required(ErrorMessage = "Account name is required.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full email address for the account.
    /// </summary>
    [JsonPropertyName("email")]
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name for the account.
    /// </summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the account password (for hMailServer source only; not stored for Stalwart).
    /// </summary>
    [JsonPropertyName("password")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the password hash (for Stalwart target).
    /// </summary>
    [JsonPropertyName("passwordHash")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PasswordHash { get; set; }

    /// <summary>
    /// Gets or sets the storage quota for this account in bytes.
    /// Null means use domain quota.
    /// </summary>
    [JsonPropertyName("quota")]
    [Range(0, long.MaxValue, ErrorMessage = "Quota must be a positive number.")]
    public long? Quota { get; set; }

    /// <summary>
    /// Gets or sets the used storage for this account in bytes.
    /// </summary>
    [JsonPropertyName("usedQuota")]
    [Range(0, long.MaxValue, ErrorMessage = "Used quota must be a positive number.")]
    public long UsedQuota { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of messages for this account.
    /// Null means unlimited.
    /// </summary>
    [JsonPropertyName("maxMessages")]
    [Range(0, int.MaxValue, ErrorMessage = "Max messages must be a positive number.")]
    public int? MaxMessages { get; set; }

    /// <summary>
    /// Gets or sets the current number of messages for this account.
    /// </summary>
    [JsonPropertyName("messageCount")]
    [Range(0, int.MaxValue, ErrorMessage = "Message count must be a positive number.")]
    public int MessageCount { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the account was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the account was last updated.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the account was last logged in.
    /// </summary>
    [JsonPropertyName("lastLoginAt")]
    public DateTimeOffset? LastLoginAt { get; set; }

    /// <summary>
    /// Gets or sets whether the account is enabled.
    /// </summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the account is an administrator.
    /// </summary>
    [JsonPropertyName("isAdmin")]
    public bool IsAdmin { get; set; }

    /// <summary>
    /// Gets or sets the account's forwarding addresses.
    /// </summary>
    [JsonPropertyName("forwardingAddresses")]
    public List<string> ForwardingAddresses { get; set; } = new();

    /// <summary>
    /// Gets or sets whether forwarding is enabled for this account.
    /// </summary>
    [JsonPropertyName("forwardingEnabled")]
    public bool ForwardingEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether to keep a copy of forwarded messages.
    /// </summary>
    [JsonPropertyName("keepForwardedCopy")]
    public bool KeepForwardedCopy { get; set; } = true;

    /// <summary>
    /// Gets or sets the account's email aliases.
    /// </summary>
    [JsonPropertyName("aliases")]
    public List<EmailAlias> Aliases { get; set; } = new();

    /// <summary>
    /// Gets or sets the domain this account belongs to.
    /// </summary>
    [JsonPropertyName("domain")]
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the domain ID this account belongs to.
    /// </summary>
    [JsonPropertyName("domainId")]
    public string? DomainId { get; set; }

    /// <summary>
    /// Gets or sets custom properties for the account.
    /// </summary>
    [JsonPropertyName("properties")]
    public Dictionary<string, string> Properties { get; set; } = new();

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
    /// Gets the username part of the email address (local part).
    /// </summary>
    [JsonIgnore]
    public string Username
    {
        get => EmailValidator.ExtractLocalPart(Email);
    }

    /// <summary>
    /// Initializes a new instance of the Account class.
    /// </summary>
    public Account()
    { }

    /// <summary>
    /// Initializes a new instance of the Account class with the specified email.
    /// </summary>
    /// <param name="email">The email address.</param>
    public Account(string email)
    {
        Email = EmailValidator.Normalize(email);
        Name = EmailValidator.ExtractLocalPart(email);
        Domain = EmailValidator.ExtractDomain(email);
    }

    /// <summary>
    /// Validates the account model.
    /// </summary>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>A collection of validation results.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Validate email
        if (!EmailValidator.IsValid(Email))
        {
            yield return new ValidationResult(
                "Invalid email address format.",
                [nameof(Email)]);
        }

        // Validate domain
        if (!DomainValidator.IsValid(Domain))
        {
            yield return new ValidationResult(
                "Invalid domain name.",
                [nameof(Domain)]);
        }

        // Validate forwarding addresses
        foreach (var forwardingAddress in ForwardingAddresses)
        {
            if (!EmailValidator.IsValid(forwardingAddress))
            {
                yield return new ValidationResult(
                    $"Invalid forwarding address: {forwardingAddress}",
                    [nameof(ForwardingAddresses)]);
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

        // Validate message count
        if (MessageCount < 0)
        {
            yield return new ValidationResult(
                "Message count cannot be negative.",
                [nameof(MessageCount)]);
        }
    }

    /// <summary>
    /// Validates the account and throws an exception if invalid.
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
    public bool Equals(Account? other)
    {
        if (other == null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return string.Equals(Email, other.Email, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return Equals(obj as Account);
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    public override int GetHashCode()
    {
        return Email.ToLowerInvariant().GetHashCode();
    }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    public override string ToString()
    {
        return Email;
    }
}
