// <copyright file="EmailAlias.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using StalwartMigration.Utilities.Helpers;

namespace StalwartMigration.Core.Models;

/// <summary>
/// Represents an email alias that forwards mail from one address to another.
/// </summary>
public class EmailAlias : IEquatable<EmailAlias>, IValidatableObject
{
    /// <summary>
    /// Gets or sets the unique identifier for the alias.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the source email address (the alias address).
    /// </summary>
    [JsonPropertyName("source")]
    [Required(ErrorMessage = "Source email address is required.")]
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the destination email address (where mail is forwarded).
    /// </summary>
    [JsonPropertyName("destination")]
    [Required(ErrorMessage = "Destination email address is required.")]
    public string Destination { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the domain ID this alias belongs to.
    /// </summary>
    [JsonPropertyName("domainId")]
    public string? DomainId { get; set; }

    /// <summary>
    /// Gets or sets the account ID this alias belongs to.
    /// </summary>
    [JsonPropertyName("accountId")]
    public string? AccountId { get; set; }

    /// <summary>
    /// Gets or sets whether the alias is enabled.
    /// </summary>
    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the date and time when the alias was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the alias was last updated.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Gets the source domain from the alias.
    /// </summary>
    [JsonIgnore]
    public string SourceDomain => EmailValidator.ExtractDomain(Source);

    /// <summary>
    /// Gets the source local part from the alias.
    /// </summary>
    [JsonIgnore]
    public string SourceLocalPart => EmailValidator.ExtractLocalPart(Source);

    /// <summary>
    /// Gets the destination domain from the alias.
    /// </summary>
    [JsonIgnore]
    public string DestinationDomain => EmailValidator.ExtractDomain(Destination);

    /// <summary>
    /// Gets the destination local part from the alias.
    /// </summary>
    [JsonIgnore]
    public string DestinationLocalPart => EmailValidator.ExtractLocalPart(Destination);

    /// <summary>
    /// Initializes a new instance of the EmailAlias class.
    /// </summary>
    public EmailAlias()
    { }

    /// <summary>
    /// Initializes a new instance of the EmailAlias class with the specified source and destination.
    /// </summary>
    /// <param name="source">The source email address.</param>
    /// <param name="destination">The destination email address.</param>
    public EmailAlias(string source, string destination)
    {
        Source = EmailValidator.Normalize(source);
        Destination = EmailValidator.Normalize(destination);
    }

    /// <summary>
    /// Validates the email alias model.
    /// </summary>
    /// <param name="validationContext">The validation context.</param>
    /// <returns>A collection of validation results.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Validate source
        if (!EmailValidator.IsValid(Source))
        {
            yield return new ValidationResult(
                "Invalid source email address format.",
                [nameof(Source)]);
        }

        // Validate destination
        if (!EmailValidator.IsValid(Destination))
        {
            yield return new ValidationResult(
                "Invalid destination email address format.",
                [nameof(Destination)]);
        }

        // Validate that source and destination are different
        if (string.Equals(Source, Destination, StringComparison.OrdinalIgnoreCase))
        {
            yield return new ValidationResult(
                "Source and destination cannot be the same.",
                [nameof(Destination)]);
        }
    }

    /// <summary>
    /// Validates the email alias and throws an exception if invalid.
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
    /// Creates a reverse alias (from destination to source).
    /// </summary>
    /// <returns>A new EmailAlias with source and destination reversed.</returns>
    public EmailAlias Reverse()
    {
        return new EmailAlias(Destination, Source)
        {
            Id = Id,
            DomainId = DomainId,
            AccountId = AccountId,
            IsEnabled = IsEnabled,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt
        };
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    public bool Equals(EmailAlias? other)
    {
        if (other == null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return string.Equals(Source, other.Source, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(Destination, other.Destination, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return Equals(obj as EmailAlias);
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + Source.ToLowerInvariant().GetHashCode();
            hash = hash * 23 + Destination.ToLowerInvariant().GetHashCode();
            return hash;
        }
    }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    public override string ToString()
    {
        return $"{Source} -> {Destination}";
    }
}
