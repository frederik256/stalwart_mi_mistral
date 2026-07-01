// <copyright file="StalwartApiModels.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

namespace StalwartMigration.Infrastructure.Stalwart;

/// <summary>
/// API models for Stalwart REST API v1.
/// Based on: https://github.com/stalwartlabs/stalwart/blob/main/api/v1/openapi.yml
/// </summary>

#region Authentication

/// <summary>
/// Authentication credentials for the Stalwart API.
/// </summary>
public class ApiCredentials
{
    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    [JsonPropertyName("password")]
    public string? Password { get; set; }

    /// <summary>
    /// Initializes a new instance of the ApiCredentials class.
    /// </summary>
    public ApiCredentials()
    { }

    /// <summary>
    /// Initializes a new instance of the ApiCredentials class with the specified username and password.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    public ApiCredentials(string username, string password)
    {
        Username = username;
        Password = password;
    }
}

/// <summary>
/// Authentication token response from the Stalwart API.
/// </summary>
public class AuthTokenResponse
{
    /// <summary>
    /// Gets or sets the access token.
    /// </summary>
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the token type (e.g., "Bearer").
    /// </summary>
    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    /// <summary>
    /// Gets or sets the number of seconds until the token expires.
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Gets or sets the refresh token.
    /// </summary>
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Gets the expiration date and time of the token.
    /// </summary>
    [JsonIgnore]
    public DateTimeOffset ExpiresAt => DateTimeOffset.UtcNow.AddSeconds(ExpiresIn);

    /// <summary>
    /// Gets whether the token is expired.
    /// </summary>
    [JsonIgnore]
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
}

#endregion

#region Health Check

/// <summary>
/// Health check response from the Stalwart API.
/// </summary>
public class HealthCheckResponse
{
    /// <summary>
    /// Gets or sets whether the service is healthy.
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Gets or sets the version of the service.
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the health check.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the uptime of the service in seconds.
    /// </summary>
    [JsonPropertyName("uptime")]
    public long Uptime { get; set; }

    /// <summary>
    /// Gets or sets the uptime as a TimeSpan.
    /// </summary>
    [JsonIgnore]
    public TimeSpan UptimeTimeSpan => TimeSpan.FromSeconds(Uptime);

    /// <summary>
    /// Gets whether the service is healthy.
    /// </summary>
    [JsonIgnore]
    public bool IsHealthy => string.Equals(Status, "ok", StringComparison.OrdinalIgnoreCase);
}

#endregion

#region Domains

/// <summary>
/// Domain request DTO for creating or updating a domain.
/// </summary>
public class DomainRequest
{
    /// <summary>
    /// Gets or sets the domain name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of accounts.
    /// </summary>
    [JsonPropertyName("max_accounts")]
    public int? MaxAccounts { get; set; }

    /// <summary>
    /// Gets or sets the storage quota in bytes.
    /// </summary>
    [JsonPropertyName("quota")]
    public long? Quota { get; set; }

    /// <summary>
    /// Gets or sets the message quota.
    /// </summary>
    [JsonPropertyName("max_messages")]
    public int? MaxMessages { get; set; }

    /// <summary>
    /// Gets or sets whether DKIM signing is enabled.
    /// </summary>
    [JsonPropertyName("dkim_enabled")]
    public bool? DkimEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether SPF checking is enabled.
    /// </summary>
    [JsonPropertyName("spf_enabled")]
    public bool? SpfEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether DMARC checking is enabled.
    /// </summary>
    [JsonPropertyName("dmarc_enabled")]
    public bool? DmarcEnabled { get; set; }
}

/// <summary>
/// Domain response DTO from the Stalwart API.
/// </summary>
public class DomainResponse
{
    /// <summary>
    /// Gets or sets the domain ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the domain name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets whether the domain is enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of accounts.
    /// </summary>
    [JsonPropertyName("max_accounts")]
    public int? MaxAccounts { get; set; }

    /// <summary>
    /// Gets or sets the storage quota in bytes.
    /// </summary>
    [JsonPropertyName("quota")]
    public long? Quota { get; set; }

    /// <summary>
    /// Gets or sets the used storage in bytes.
    /// </summary>
    [JsonPropertyName("used_quota")]
    public long UsedQuota { get; set; }

    /// <summary>
    /// Gets or sets the message quota.
    /// </summary>
    [JsonPropertyName("max_messages")]
    public int? MaxMessages { get; set; }

    /// <summary>
    /// Gets or sets whether DKIM signing is enabled.
    /// </summary>
    [JsonPropertyName("dkim_enabled")]
    public bool DkimEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether SPF checking is enabled.
    /// </summary>
    [JsonPropertyName("spf_enabled")]
    public bool SpfEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether DMARC checking is enabled.
    /// </summary>
    [JsonPropertyName("dmarc_enabled")]
    public bool DmarcEnabled { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the domain was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the domain was last updated.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Gets the quota usage percentage.
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
}

/// <summary>
/// List response for domains.
/// </summary>
public class DomainListResponse
{
    /// <summary>
    /// Gets or sets the list of domains.
    /// </summary>
    [JsonPropertyName("domains")]
    public List<DomainResponse> Domains { get; set; } = new();

    /// <summary>
    /// Gets or sets the total number of domains.
    /// </summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>
    /// Gets or sets the offset for pagination.
    /// </summary>
    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    /// <summary>
    /// Gets or sets the limit for pagination.
    /// </summary>
    [JsonPropertyName("limit")]
    public int Limit { get; set; }
}

#endregion

#region Accounts

/// <summary>
/// Account request DTO for creating or updating an account.
/// </summary>
public class AccountRequest
{
    /// <summary>
    /// Gets or sets the domain ID.
    /// </summary>
    [JsonPropertyName("domain_id")]
    public string? DomainId { get; set; }

    /// <summary>
    /// Gets or sets the account name (local part).
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    [JsonPropertyName("password")]
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the storage quota in bytes.
    /// </summary>
    [JsonPropertyName("quota")]
    public long? Quota { get; set; }

    /// <summary>
    /// Gets or sets the message quota.
    /// </summary>
    [JsonPropertyName("max_messages")]
    public int? MaxMessages { get; set; }

    /// <summary>
    /// Gets or sets whether the account is enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; set; }

    /// <summary>
    /// Gets or sets whether the account is an administrator.
    /// </summary>
    [JsonPropertyName("is_admin")]
    public bool? IsAdmin { get; set; }

    /// <summary>
    /// Gets or sets the forwarding addresses.
    /// </summary>
    [JsonPropertyName("forwarding")]
    public List<string>? Forwarding { get; set; }

    /// <summary>
    /// Gets or sets whether to keep a copy of forwarded messages.
    /// </summary>
    [JsonPropertyName("keep_forwarded_copy")]
    public bool? KeepForwardedCopy { get; set; }
}

/// <summary>
/// Account response DTO from the Stalwart API.
/// </summary>
public class AccountResponse
{
    /// <summary>
    /// Gets or sets the account ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the domain ID.
    /// </summary>
    [JsonPropertyName("domain_id")]
    public string? DomainId { get; set; }

    /// <summary>
    /// Gets or sets the account name (local part).
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the full email address.
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets whether the account is enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets whether the account is an administrator.
    /// </summary>
    [JsonPropertyName("is_admin")]
    public bool IsAdmin { get; set; }

    /// <summary>
    /// Gets or sets the storage quota in bytes.
    /// </summary>
    [JsonPropertyName("quota")]
    public long? Quota { get; set; }

    /// <summary>
    /// Gets or sets the used storage in bytes.
    /// </summary>
    [JsonPropertyName("used_quota")]
    public long UsedQuota { get; set; }

    /// <summary>
    /// Gets or sets the message quota.
    /// </summary>
    [JsonPropertyName("max_messages")]
    public int? MaxMessages { get; set; }

    /// <summary>
    /// Gets or sets the current message count.
    /// </summary>
    [JsonPropertyName("message_count")]
    public int MessageCount { get; set; }

    /// <summary>
    /// Gets or sets the forwarding addresses.
    /// </summary>
    [JsonPropertyName("forwarding")]
    public List<string> Forwarding { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to keep a copy of forwarded messages.
    /// </summary>
    [JsonPropertyName("keep_forwarded_copy")]
    public bool KeepForwardedCopy { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the account was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the account was last updated.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the account was last logged in.
    /// </summary>
    [JsonPropertyName("last_login_at")]
    public DateTimeOffset? LastLoginAt { get; set; }
}

/// <summary>
/// List response for accounts.
/// </summary>
public class AccountListResponse
{
    /// <summary>
    /// Gets or sets the list of accounts.
    /// </summary>
    [JsonPropertyName("accounts")]
    public List<AccountResponse> Accounts { get; set; } = new();

    /// <summary>
    /// Gets or sets the total number of accounts.
    /// </summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>
    /// Gets or sets the offset for pagination.
    /// </summary>
    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    /// <summary>
    /// Gets or sets the limit for pagination.
    /// </summary>
    [JsonPropertyName("limit")]
    public int Limit { get; set; }
}

#endregion

#region Aliases

/// <summary>
/// Alias request DTO for creating or updating an alias.
/// </summary>
public class AliasRequest
{
    /// <summary>
    /// Gets or sets the source email address.
    /// </summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the destination email address.
    /// </summary>
    [JsonPropertyName("destination")]
    public string? Destination { get; set; }
}

/// <summary>
/// Alias response DTO from the Stalwart API.
/// </summary>
public class AliasResponse
{
    /// <summary>
    /// Gets or sets the alias ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the source email address.
    /// </summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the destination email address.
    /// </summary>
    [JsonPropertyName("destination")]
    public string? Destination { get; set; }

    /// <summary>
    /// Gets or sets whether the alias is enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the alias was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the alias was last updated.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>
/// List response for aliases.
/// </summary>
public class AliasListResponse
{
    /// <summary>
    /// Gets or sets the list of aliases.
    /// </summary>
    [JsonPropertyName("aliases")]
    public List<AliasResponse> Aliases { get; set; } = new();

    /// <summary>
    /// Gets or sets the total number of aliases.
    /// </summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>
    /// Gets or sets the offset for pagination.
    /// </summary>
    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    /// <summary>
    /// Gets or sets the limit for pagination.
    /// </summary>
    [JsonPropertyName("limit")]
    public int Limit { get; set; }
}

#endregion

#region Common

/// <summary>
/// Generic API response wrapper.
/// </summary>
/// <typeparam name="T">The type of the data.</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Gets or sets whether the request was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the data payload.
    /// </summary>
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    [JsonPropertyName("error_code")]
    public string? ErrorCode { get; set; }
}

/// <summary>
/// Generic paginated API response.
/// </summary>
/// <typeparam name="T">The type of the items.</typeparam>
public class PaginatedResponse<T>
{
    /// <summary>
    /// Gets or sets the list of items.
    /// </summary>
    [JsonPropertyName("items")]
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Gets or sets the total number of items.
    /// </summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>
    /// Gets or sets the offset for pagination.
    /// </summary>
    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    /// <summary>
    /// Gets or sets the limit for pagination.
    /// </summary>
    [JsonPropertyName("limit")]
    public int Limit { get; set; }
}

/// <summary>
/// Error response from the Stalwart API.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the error details.
    /// </summary>
    [JsonPropertyName("details")]
    public string? Details { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    [JsonPropertyName("status_code")]
    public int? StatusCode { get; set; }
}

#endregion
