// <copyright file="ValidationService.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StalwartMigration.Utilities.Helpers;

namespace StalwartMigration.Core.Services;

/// <summary>
/// Service for validating data integrity and business rules.
/// </summary>
public class ValidationService : IValidationService
{
    private readonly ILogger<ValidationService> _logger;

    /// <summary>
    /// Initializes a new instance of the ValidationService class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ValidationService(ILogger<ValidationService>? logger = null)
    {
        _logger = logger ?? NullLogger<ValidationService>.Instance;
    }

    /// <summary>
    /// Validates that an object is not null.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The name of the parameter.</param>
    /// <returns>The validation result.</returns>
    public ValidationResult ValidateNotNull(object? value, string paramName)
    {
        if (value != null)
        {
            _logger.LogDebug("{ParamName} validation passed (not null)", paramName);
            return ValidationResult.Success();
        }

        _logger.LogWarning("{ParamName} validation failed: value is null", paramName);
        return ValidationResult.Fail($"{paramName} cannot be null.");
    }

    /// <summary>
    /// Validates a string value.
    /// </summary>
    public ValidationResult ValidateString(string? value, string paramName, int minLength = 0, int maxLength = int.MaxValue, bool allowNullOrEmpty = false)
    {
        if (allowNullOrEmpty && string.IsNullOrEmpty(value))
        {
            _logger.LogDebug("{ParamName} validation passed (null/empty allowed)", paramName);
            return ValidationResult.Success();
        }

        if (value == null)
        {
            _logger.LogWarning("{ParamName} validation failed: value is null", paramName);
            return ValidationResult.Fail($"{paramName} cannot be null.");
        }

        if (value.Length < minLength)
        {
            _logger.LogWarning("{ParamName} validation failed: too short ({Length} < {Min})", paramName, value.Length, minLength);
            return ValidationResult.Fail($"{paramName} is too short. Minimum length: {minLength}, actual length: {value.Length}.");
        }

        if (value.Length > maxLength)
        {
            _logger.LogWarning("{ParamName} validation failed: too long ({Length} > {Max})", paramName, value.Length, maxLength);
            return ValidationResult.Fail($"{paramName} is too long. Maximum length: {maxLength}, actual length: {value.Length}.");
        }

        _logger.LogDebug("{ParamName} validation passed (length: {Length})", paramName, value.Length);
        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates an email address.
    /// </summary>
    public ValidationResult ValidateEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return ValidationResult.Fail("Email address cannot be null or empty.");

        try
        {
            var isValid = EmailValidator.IsValid(email);
            if (!isValid)
                return ValidationResult.Fail($"Invalid email address format: {email}");
            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            return ValidationResult.Fail($"Email validation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates a domain name.
    /// </summary>
    public ValidationResult ValidateDomain(string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return ValidationResult.Fail("Domain cannot be null or empty.");

        try
        {
            var isValid = DomainValidator.IsValid(domain);
            if (!isValid)
                return ValidationResult.Fail($"Invalid domain name format: {domain}");
            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            return ValidationResult.Fail($"Domain validation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates a collection.
    /// </summary>
    public ValidationResult ValidateCollection(System.Collections.IEnumerable? collection, string paramName, bool allowEmpty = false)
    {
        if (collection == null)
            return ValidationResult.Fail($"{paramName} cannot be null.");
        
        var count = 0;
        foreach (var _ in collection)
        {
            count++;
            break; // Just check if it has at least one element
        }
        
        if (!allowEmpty && count == 0)
            return ValidationResult.Fail($"{paramName} cannot be empty.");
        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates that a value is within the specified range.
    /// </summary>
    public ValidationResult ValidateRange<T>(T value, string paramName, T minValue, T maxValue) where T : IComparable<T>
    {
        if (value == null)
            return ValidationResult.Fail($"{paramName} cannot be null.");
        if (value.CompareTo(minValue) < 0)
            return ValidationResult.Fail($"{paramName} must be at least {minValue}.");
        if (value.CompareTo(maxValue) > 0)
            return ValidationResult.Fail($"{paramName} must be at most {maxValue}.");
        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates a file path.
    /// </summary>
    public ValidationResult ValidateFilePath(string? path, string paramName, bool checkExists = false)
    {
        if (string.IsNullOrWhiteSpace(path))
            return ValidationResult.Fail($"{paramName} cannot be null or empty.");

        try
        {
            var sanitized = PathSanitizer.SanitizePath(path);
            if (sanitized != path)
                return ValidationResult.Fail($"{paramName} contains invalid characters or path traversal attempt.");
            if (checkExists && !File.Exists(path))
                return ValidationResult.Fail($"{paramName} file does not exist: {path}");
            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            return ValidationResult.Fail($"{paramName} is invalid: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates a directory path.
    /// </summary>
    public ValidationResult ValidateDirectoryPath(string? path, string paramName, bool checkExists = false)
    {
        if (string.IsNullOrWhiteSpace(path))
            return ValidationResult.Fail($"{paramName} cannot be null or empty.");

        try
        {
            var sanitized = PathSanitizer.SanitizePath(path);
            if (sanitized != path)
                return ValidationResult.Fail($"{paramName} contains invalid characters or path traversal attempt.");
            if (checkExists && !Directory.Exists(path))
                return ValidationResult.Fail($"{paramName} directory does not exist: {path}");
            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            return ValidationResult.Fail($"{paramName} is invalid: {ex.Message}");
        }
    }
}
