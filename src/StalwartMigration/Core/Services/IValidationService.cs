// <copyright file="IValidationService.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

namespace StalwartMigration.Core.Services;

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the validation passed.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the error message if validation failed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Initializes a new instance of the ValidationResult class.
    /// </summary>
    /// <param name="isValid">Whether the validation passed.</param>
    /// <param name="errorMessage">The error message if validation failed.</param>
    public ValidationResult(bool isValid, string? errorMessage = null)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Success() => new(true);

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    public static ValidationResult Fail(string errorMessage) => new(false, errorMessage);

    /// <summary>
    /// Implicit conversion to bool (returns IsValid).
    /// </summary>
    public static implicit operator bool(ValidationResult result) => result.IsValid;
}

/// <summary>
/// Interface for data validation services.
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Validates that an object is not null.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The name of the parameter.</param>
    /// <returns>The validation result.</returns>
    ValidationResult ValidateNotNull(object? value, string paramName);

    /// <summary>
    /// Validates a string value.
    /// </summary>
    /// <param name="value">The string value to validate.</param>
    /// <param name="paramName">The name of the parameter.</param>
    /// <param name="minLength">The minimum length.</param>
    /// <param name="maxLength">The maximum length.</param>
    /// <param name="allowNullOrEmpty">Whether to allow null or empty strings.</param>
    /// <returns>The validation result.</returns>
    ValidationResult ValidateString(string? value, string paramName, int minLength = 0, int maxLength = int.MaxValue, bool allowNullOrEmpty = false);

    /// <summary>
    /// Validates an email address.
    /// </summary>
    /// <param name="email">The email address to validate.</param>
    /// <returns>The validation result.</returns>
    ValidationResult ValidateEmail(string? email);

    /// <summary>
    /// Validates a domain name.
    /// </summary>
    /// <param name="domain">The domain name to validate.</param>
    /// <returns>The validation result.</returns>
    ValidationResult ValidateDomain(string? domain);

    /// <summary>
    /// Validates a collection.
    /// </summary>
    /// <param name="collection">The collection to validate.</param>
    /// <param name="paramName">The name of the parameter.</param>
    /// <param name="allowEmpty">Whether to allow empty collections.</param>
    /// <returns>The validation result.</returns>
    ValidationResult ValidateCollection(System.Collections.IEnumerable? collection, string paramName, bool allowEmpty = false);

    /// <summary>
    /// Validates that a value is within the specified range.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The name of the parameter.</param>
    /// <param name="minValue">The minimum value (inclusive).</param>
    /// <param name="maxValue">The maximum value (inclusive).</param>
    /// <returns>The validation result.</returns>
    ValidationResult ValidateRange<T>(T value, string paramName, T minValue, T maxValue) where T : IComparable<T>;

    /// <summary>
    /// Validates a file path.
    /// </summary>
    /// <param name="path">The file path to validate.</param>
    /// <param name="paramName">The name of the parameter.</param>
    /// <param name="checkExists">Whether to check if the file exists.</param>
    /// <returns>The validation result.</returns>
    ValidationResult ValidateFilePath(string? path, string paramName, bool checkExists = false);

    /// <summary>
    /// Validates a directory path.
    /// </summary>
    /// <param name="path">The directory path to validate.</param>
    /// <param name="paramName">The name of the parameter.</param>
    /// <param name="checkExists">Whether to check if the directory exists.</param>
    /// <returns>The validation result.</returns>
    ValidationResult ValidateDirectoryPath(string? path, string paramName, bool checkExists = false);
}
