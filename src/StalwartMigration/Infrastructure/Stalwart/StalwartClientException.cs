// <copyright file="StalwartClientException.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.Net;
using System.Text.Json.Serialization;

namespace StalwartMigration.Infrastructure.Stalwart;

/// <summary>
/// Exception thrown when there is an error communicating with the Stalwart REST API.
/// </summary>
public class StalwartClientException : Exception
{
    /// <summary>
    /// Initializes a new instance of the StalwartClientException class.
    /// </summary>
    public StalwartClientException()
    { }

    /// <summary>
    /// Initializes a new instance of the StalwartClientException class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public StalwartClientException(string message)
        : base(message)
    { }

    /// <summary>
    /// Initializes a new instance of the StalwartClientException class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public StalwartClientException(string message, Exception innerException)
        : base(message, innerException)
    { }

    /// <summary>
    /// Gets or sets the HTTP status code from the API response.
    /// </summary>
    public HttpStatusCode? StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the request URL that failed.
    /// </summary>
    public string? RequestUrl { get; set; }

    /// <summary>
    /// Gets or sets the HTTP method that failed.
    /// </summary>
    public string? HttpMethod { get; set; }

    /// <summary>
    /// Gets or sets the request body that was sent.
    /// </summary>
    public string? RequestBody { get; set; }

    /// <summary>
    /// Gets or sets the response body from the API.
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// Gets or sets the error code from the API response.
    /// </summary>
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the error details from the API response.
    /// </summary>
    [JsonPropertyName("errorDetails")]
    public string? ErrorDetails { get; set; }

    /// <summary>
    /// Creates a StalwartClientException for a failed API request.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="requestUrl">The request URL.</param>
    /// <param name="httpMethod">The HTTP method.</param>
    /// <param name="requestBody">The request body.</param>
    /// <param name="responseBody">The response body.</param>
    /// <returns>A StalwartClientException for the failed request.</returns>
    public static StalwartClientException ForApiError(
        string message,
        HttpStatusCode statusCode,
        string? requestUrl = null,
        string? httpMethod = null,
        string? requestBody = null,
        string? responseBody = null)
    {
        return new StalwartClientException(message)
        {
            StatusCode = statusCode,
            RequestUrl = requestUrl,
            HttpMethod = httpMethod,
            RequestBody = requestBody,
            ResponseBody = responseBody
        };
    }

    /// <summary>
    /// Creates a StalwartClientException for a not found error.
    /// </summary>
    /// <param name="resourceType">The type of resource that was not found.</param>
    /// <param name="resourceId">The ID of the resource that was not found.</param>
    /// <returns>A StalwartClientException for the not found error.</returns>
    public static StalwartClientException ForNotFound(string resourceType, string resourceId)
    {
        return new StalwartClientException(
            $"Resource of type '{resourceType}' with ID '{resourceId}' was not found.")
        {
            StatusCode = HttpStatusCode.NotFound,
            ErrorCode = "NOT_FOUND"
        };
    }

    /// <summary>
    /// Creates a StalwartClientException for an unauthorized error.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A StalwartClientException for the unauthorized error.</returns>
    public static StalwartClientException ForUnauthorized(string? message = null)
    {
        return new StalwartClientException(message ?? "Authentication failed. Invalid or missing credentials.")
        {
            StatusCode = HttpStatusCode.Unauthorized,
            ErrorCode = "UNAUTHORIZED"
        };
    }

    /// <summary>
    /// Creates a StalwartClientException for a forbidden error.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A StalwartClientException for the forbidden error.</returns>
    public static StalwartClientException ForForbidden(string? message = null)
    {
        return new StalwartClientException(message ?? "Access denied. Insufficient permissions.")
        {
            StatusCode = HttpStatusCode.Forbidden,
            ErrorCode = "FORBIDDEN"
        };
    }

    /// <summary>
    /// Creates a StalwartClientException for a conflict error.
    /// </summary>
    /// <param name="resourceType">The type of resource that caused the conflict.</param>
    /// <param name="resourceId">The ID of the resource that caused the conflict.</param>
    /// <param name="message">The error message.</param>
    /// <returns>A StalwartClientException for the conflict error.</returns>
    public static StalwartClientException ForConflict(string resourceType, string resourceId, string? message = null)
    {
        return new StalwartClientException(
            message ?? $"Resource of type '{resourceType}' with ID '{resourceId}' already exists.")
        {
            StatusCode = HttpStatusCode.Conflict,
            ErrorCode = "CONFLICT"
        };
    }

    /// <summary>
    /// Creates a StalwartClientException for a rate limit error.
    /// </summary>
    /// <param name="retryAfter">The number of seconds to wait before retrying.</param>
    /// <returns>A StalwartClientException for the rate limit error.</returns>
    public static StalwartClientException ForRateLimit(int retryAfter)
    {
        return new StalwartClientException(
            $"Rate limit exceeded. Please retry after {retryAfter} seconds.")
        {
            StatusCode = HttpStatusCode.TooManyRequests,
            ErrorCode = "RATE_LIMITED",
            ErrorDetails = retryAfter.ToString()
        };
    }

    /// <summary>
    /// Creates a StalwartClientException for a connection error.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <returns>A StalwartClientException for the connection error.</returns>
    public static StalwartClientException ForConnectionError(string message, Exception innerException)
    {
        return new StalwartClientException(message, innerException)
        {
            ErrorCode = "CONNECTION_ERROR"
        };
    }

    /// <summary>
    /// Creates a StalwartClientException from an HTTP response.
    /// </summary>
    /// <param name="response">The HTTP response.</param>
    /// <returns>A StalwartClientException for the HTTP response.</returns>
    public static async Task<StalwartClientException> FromResponseAsync(HttpResponseMessage response)
    {
        var statusCode = response.StatusCode;
        var requestUrl = response.RequestMessage?.RequestUri?.ToString();
        var httpMethod = response.RequestMessage?.Method.ToString();
        var responseBody = await response.Content.ReadAsStringAsync();

        // Try to parse error details from response
        string message;
        string? errorCode = null;
        string? errorDetails = null;

        try
        {
            // Try to parse as JSON
            var responseObj = System.Text.Json.JsonDocument.Parse(responseBody);
            message = responseObj.RootElement.GetProperty("message").GetString() ?? response.ReasonPhrase ?? "Unknown error";
            errorCode = responseObj.RootElement.GetProperty("code").GetString();
            errorDetails = responseObj.RootElement.GetProperty("details").GetString();
        }
        catch
        {
            message = response.ReasonPhrase ?? "Unknown error";
        }

        return new StalwartClientException(message)
        {
            StatusCode = statusCode,
            RequestUrl = requestUrl,
            HttpMethod = httpMethod,
            ResponseBody = responseBody,
            ErrorCode = errorCode,
            ErrorDetails = errorDetails
        };
    }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    public override string ToString()
    {
        var baseString = base.ToString();
        if (StatusCode.HasValue)
        {
            baseString += $"\nStatus Code: {(int)StatusCode.Value} ({StatusCode.Value})";
        }
        if (!string.IsNullOrEmpty(RequestUrl))
        {
            baseString += $"\nRequest URL: {RequestUrl}";
        }
        if (!string.IsNullOrEmpty(HttpMethod))
        {
            baseString += $"\nHTTP Method: {HttpMethod}";
        }
        if (!string.IsNullOrEmpty(ErrorCode))
        {
            baseString += $"\nError Code: {ErrorCode}";
        }
        if (!string.IsNullOrEmpty(ResponseBody))
        {
            baseString += $"\nResponse: {ResponseBody}";
        }
        return baseString;
    }
}
