// <copyright file="DataValidationException.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.Runtime.Serialization;

namespace StalwartMigration.Core.Exceptions;

/// <summary>
/// Exception thrown when there is a data validation error.
/// </summary>
public class DataValidationException : MigrationException
{
    /// <summary>
    /// Initializes a new instance of the DataValidationException class.
    /// </summary>
    public DataValidationException()
    { }

    /// <summary>
    /// Initializes a new instance of the DataValidationException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public DataValidationException(string message)
        : base(message)
    { }

    /// <summary>
    /// Initializes a new instance of the DataValidationException class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public DataValidationException(string message, Exception innerException)
        : base(message, innerException)
    { }

    /// <summary>
    /// Initializes a new instance of the DataValidationException class with serialized data.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    protected DataValidationException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    { }

    /// <summary>
    /// Creates a DataValidationException with context and remediation information.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="context">Additional context about the data validation error.</param>
    /// <param name="remediation">Suggestions for how to fix the data validation issue.</param>
    public DataValidationException(string message, string context, string remediation)
        : base(message)
    {
        Context = context;
        Remediation = remediation;
    }

    /// <summary>
    /// Gets the field or property name that failed validation, if applicable.
    /// </summary>
    public string? FieldName { get; private set; }

    /// <summary>
    /// Gets the invalid value that caused the validation error, if applicable.
    /// </summary>
    public object? InvalidValue { get; private set; }

    /// <summary>
    /// Gets the expected format or constraints for the field.
    /// </summary>
    public string? ExpectedFormat { get; private set; }

    /// <summary>
    /// Creates a DataValidationException for a required field that is missing or empty.
    /// </summary>
    /// <param name="fieldName">The name of the required field.</param>
    /// <returns>A DataValidationException for the missing required field.</returns>
    public static DataValidationException ForMissingRequiredField(string fieldName)
    {
        var exception = new DataValidationException(
            $"Required field '{fieldName}' is missing or empty.",
            "Data Validation",
            $"Please provide a value for the '{fieldName}' field. This field is required and cannot be null or empty.");
        
        exception.FieldName = fieldName;
        return exception;
    }

    /// <summary>
    /// Creates a DataValidationException for an invalid format.
    /// </summary>
    /// <param name="fieldName">The name of the field with invalid format.</param>
    /// <param name="value">The invalid value.</param>
    /// <param name="expectedFormat">Description of the expected format.</param>
    /// <returns>A DataValidationException for the invalid format.</returns>
    public static DataValidationException ForInvalidFormat(string fieldName, object value, string expectedFormat)
    {
        var exception = new DataValidationException(
            $"Invalid format for field '{fieldName}'. Value '{value}' does not match expected format: {expectedFormat}.",
            "Data Validation",
            $"Please provide a value for '{fieldName}' that matches the format: {expectedFormat}.");
        
        exception.FieldName = fieldName;
        exception.InvalidValue = value;
        exception.ExpectedFormat = expectedFormat;
        return exception;
    }

    /// <summary>
    /// Creates a DataValidationException for a value that is out of range.
    /// </summary>
    /// <param name="fieldName">The name of the field with out-of-range value.</param>
    /// <param name="value">The out-of-range value.</param>
    /// <param name="minValue">The minimum allowed value.</param>
    /// <param name="maxValue">The maximum allowed value.</param>
    /// <returns>A DataValidationException for the out-of-range value.</returns>
    public static DataValidationException ForOutOfRange(string fieldName, object value, object minValue, object maxValue)
    {
        var exception = new DataValidationException(
            $"Value '{value}' for field '{fieldName}' is out of range. Must be between {minValue} and {maxValue}.",
            "Data Validation",
            $"Please provide a value for '{fieldName}' between {minValue} and {maxValue}.");
        
        exception.FieldName = fieldName;
        exception.InvalidValue = value;
        exception.ExpectedFormat = $"Range: {minValue} to {maxValue}";
        return exception;
    }

    /// <summary>
    /// Creates a DataValidationException for a duplicate value.
    /// </summary>
    /// <param name="fieldName">The name of the field with duplicate value.</param>
    /// <param name="value">The duplicate value.</param>
    /// <returns>A DataValidationException for the duplicate value.</returns>
    public static DataValidationException ForDuplicateValue(string fieldName, object value)
    {
        var exception = new DataValidationException(
            $"Duplicate value '{value}' found for field '{fieldName}'. Values must be unique.",
            "Data Validation",
            $"Please ensure the value for '{fieldName}' is unique. The value '{value}' already exists.");
        
        exception.FieldName = fieldName;
        exception.InvalidValue = value;
        return exception;
    }

    /// <summary>
    /// Creates a DataValidationException for an invalid reference.
    /// </summary>
    /// <param name="fieldName">The name of the field with invalid reference.</param>
    /// <param name="value">The invalid reference value.</param>
    /// <param name="validValues">Description of valid values or reference.</param>
    /// <returns>A DataValidationException for the invalid reference.</returns>
    public static DataValidationException ForInvalidReference(string fieldName, object value, string validValues)
    {
        var exception = new DataValidationException(
            $"Invalid reference '{value}' for field '{fieldName}'. {validValues}.",
            "Data Validation",
            $"Please provide a valid reference for '{fieldName}'. {validValues}.");
        
        exception.FieldName = fieldName;
        exception.InvalidValue = value;
        exception.ExpectedFormat = validValues;
        return exception;
    }
}
