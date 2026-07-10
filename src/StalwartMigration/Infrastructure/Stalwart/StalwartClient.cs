// <copyright file="StalwartClient.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StalwartMigration.Core.Models;

namespace StalwartMigration.Infrastructure.Stalwart;

/// <summary>
/// Client for communicating with the Stalwart REST API v1.
/// Provides HTTP client configuration, authentication, retry logic, and CRUD operations
/// for domains, accounts, and aliases.
/// </summary>
public class StalwartClient : IStalwartClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StalwartClient> _logger;
    private readonly StalwartClientOptions _options;
    private AuthTokenResponse? _currentToken;
    private bool _disposed;

    /// <summary>
    /// The base URL for the Stalwart API.
    /// </summary>
    public string BaseUrl { get; }

    /// <summary>
    /// Gets whether the client is currently authenticated.
    /// </summary>
    public bool IsAuthenticated => _currentToken != null && !_currentToken.IsExpired;

    /// <summary>
    /// Gets or sets the API credentials for authentication.
    /// </summary>
    public ApiCredentials? Credentials { get; set; }

    /// <summary>
    /// Initializes a new instance of the StalwartClient class.
    /// </summary>
    public StalwartClient(string baseUrl, StalwartClientOptions? options = null, ILogger<StalwartClient>? logger = null)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL cannot be null or empty.", nameof(baseUrl));

        BaseUrl = baseUrl.TrimEnd('/');
        _options = options ?? new StalwartClientOptions();
        _logger = logger ?? NullLogger<StalwartClient>.Instance;

        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };

        _httpClient = new HttpClient(handler, true)
        {
            Timeout = _options.Timeout ?? TimeSpan.FromSeconds(30)
        };

        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("StalwartMigration/1.0");
    }

    /// <summary>
    /// Initializes a new instance of the StalwartClient class with credentials.
    /// </summary>
    public StalwartClient(string baseUrl, ApiCredentials credentials, StalwartClientOptions? options = null, ILogger<StalwartClient>? logger = null)
        : this(baseUrl, options, logger)
    {
        Credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
    }

    /// <summary>
    /// Authenticates with the Stalwart API using the configured credentials.
    /// </summary>
    public async Task<AuthTokenResponse> AuthenticateAsync(ApiCredentials? credentials = null, CancellationToken cancellationToken = default)
    {
        var creds = credentials ?? Credentials;
        if (creds == null)
            throw StalwartClientException.ForUnauthorized("No credentials configured for authentication.");

        if (string.IsNullOrEmpty(creds.Username) || string.IsNullOrEmpty(creds.Password))
            throw StalwartClientException.ForUnauthorized("Username and password are required for authentication.");

        var requestUrl = $"{BaseUrl}/api/auth";
        var requestBody = new { username = creds.Username, password = creds.Password };

        var response = await SendRequestInternalAsync<AuthTokenResponse>(
            HttpMethod.Post, requestUrl, requestBody, false, cancellationToken).ConfigureAwait(false);

        _currentToken = response.Data ?? throw StalwartClientException.ForUnauthorized("Empty token response from API.");
        _logger.LogInformation("Authenticated with Stalwart API. Token expires at {ExpiresAt}", _currentToken.ExpiresAt);
        return _currentToken;
    }

    /// <summary>
    /// Refreshes the authentication token.
    /// </summary>
    public async Task<AuthTokenResponse> RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_currentToken?.RefreshToken == null)
            throw StalwartClientException.ForUnauthorized("No refresh token available.");

        var requestUrl = $"{BaseUrl}/api/auth/token";
        var requestBody = new { refresh_token = _currentToken.RefreshToken };

        var response = await SendRequestInternalAsync<AuthTokenResponse>(
            HttpMethod.Post, requestUrl, requestBody, false, cancellationToken).ConfigureAwait(false);

        _currentToken = response.Data ?? throw StalwartClientException.ForUnauthorized("Empty token response from API.");
        _logger.LogInformation("Refreshed authentication token. New token expires at {ExpiresAt}", _currentToken.ExpiresAt);
        return _currentToken;
    }

    /// <summary>
    /// Ensures the client is authenticated, refreshing the token if necessary.
    /// </summary>
    public async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        if (!IsAuthenticated)
            await AuthenticateAsync(null, cancellationToken).ConfigureAwait(false);
        else if (_currentToken!.IsExpired)
            await RefreshTokenAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Checks the health of the Stalwart API.
    /// </summary>
    public async Task<HealthCheckResponse> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/api/health";
        var response = await SendRequestInternalAsync<HealthCheckResponse>(
            HttpMethod.Get, url, null, false, cancellationToken).ConfigureAwait(false);
        return response.Data ?? new HealthCheckResponse { Status = "unknown" };
    }

    // ========================================================================
    // Domain Operations
    // ========================================================================

    /// <summary>Gets a domain by its ID.</summary>
    public async Task<DomainResponse> GetDomainAsync(string domainId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domainId))
            throw new ArgumentException("Domain ID cannot be null or empty.", nameof(domainId));
        var url = $"{BaseUrl}/api/domains/{Uri.EscapeDataString(domainId)}";
        var response = await SendRequestAsync<DomainResponse>(HttpMethod.Get, url, null, true, cancellationToken).ConfigureAwait(false);
        return response.Data ?? throw StalwartClientException.ForNotFound("Domain", domainId);
    }

    /// <summary>Gets a domain by its name.</summary>
    public async Task<DomainResponse> GetDomainByNameAsync(string domainName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domainName))
            throw new ArgumentException("Domain name cannot be null or empty.", nameof(domainName));
        var url = $"{BaseUrl}/api/domains/name/{Uri.EscapeDataString(domainName)}";
        var response = await SendRequestAsync<DomainResponse>(HttpMethod.Get, url, null, true, cancellationToken).ConfigureAwait(false);
        return response.Data ?? throw StalwartClientException.ForNotFound("Domain", domainName);
    }

    /// <summary>Lists all domains with optional pagination.</summary>
    public async Task<DomainListResponse> ListDomainsAsync(int offset = 0, int limit = 100, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/api/domains?offset={offset}&limit={limit}";
        var response = await SendRequestAsync<DomainListResponse>(HttpMethod.Get, url, null, true, cancellationToken).ConfigureAwait(false);
        return response.Data ?? new DomainListResponse();
    }

    /// <summary>Creates a new domain.</summary>
    public async Task<DomainResponse> CreateDomainAsync(DomainRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Domain name is required.", nameof(request));
        var url = $"{BaseUrl}/api/domains";
        var response = await SendRequestAsync<DomainResponse>(HttpMethod.Post, url, request, true, cancellationToken).ConfigureAwait(false);
        return response.Data ?? throw new StalwartClientException("Failed to create domain: empty response from API.");
    }

    /// <summary>Updates an existing domain.</summary>
    public async Task<DomainResponse> UpdateDomainAsync(string domainId, DomainRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domainId))
            throw new ArgumentException("Domain ID cannot be null or empty.", nameof(domainId));
        if (request == null) throw new ArgumentNullException(nameof(request));
        var url = $"{BaseUrl}/api/domains/{Uri.EscapeDataString(domainId)}";
        var response = await SendRequestAsync<DomainResponse>(HttpMethod.Put, url, request, true, cancellationToken).ConfigureAwait(false);
        return response.Data ?? throw StalwartClientException.ForNotFound("Domain", domainId);
    }

    /// <summary>Deletes a domain.</summary>
    public async Task DeleteDomainAsync(string domainId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domainId))
            throw new ArgumentException("Domain ID cannot be null or empty.", nameof(domainId));
        var url = $"{BaseUrl}/api/domains/{Uri.EscapeDataString(domainId)}";
        await SendRequestAsync<object>(HttpMethod.Delete, url, null, true, cancellationToken).ConfigureAwait(false);
    }

    // ========================================================================
    // Account Operations
    // ========================================================================

    /// <summary>Gets an account by its ID.</summary>
    public async Task<AccountResponse> GetAccountAsync(string accountId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accountId))
            throw new ArgumentException("Account ID cannot be null or empty.", nameof(accountId));
        var url = $"{BaseUrl}/api/accounts/{Uri.EscapeDataString(accountId)}";
        var response = await SendRequestAsync<AccountResponse>(HttpMethod.Get, url, null, true, cancellationToken).ConfigureAwait(false);
        return response.Data ?? throw StalwartClientException.ForNotFound("Account", accountId);
    }

    /// <summary>Gets an account by its email address.</summary>
    public async Task<AccountResponse> GetAccountByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be null or empty.", nameof(email));
        var url = $"{BaseUrl}/api/accounts/email/{Uri.EscapeDataString(email)}";
        var response = await SendRequestAsync<AccountResponse>(HttpMethod.Get, url, null, true, cancellationToken).ConfigureAwait(false);
        return response.Data ?? throw StalwartClientException.ForNotFound("Account", email);
    }

    /// <summary>Lists all accounts with optional pagination.</summary>
    public async Task<AccountListResponse> ListAccountsAsync(string? domainId = null, int offset = 0, int limit = 100, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/api/accounts?offset={offset}&limit={limit}";
        if (!string.IsNullOrEmpty(domainId))
            url += $"&domain_id={Uri.EscapeDataString(domainId)}";
        var response = await SendRequestAsync<AccountListResponse>(HttpMethod.Get, url, null, true, cancellationToken).ConfigureAwait(false);
        return response.Data ?? new AccountListResponse();
    }

    /// <summary>Creates a new account.</summary>
    public async Task<AccountResponse> CreateAccountAsync(AccountRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Account name is required.", nameof(request));
        var url = $"{BaseUrl}/api/accounts";
        var response = await SendRequestAsync<AccountResponse>(HttpMethod.Post, url, request, true, cancellationToken).ConfigureAwait(false);
        return response.Data ?? throw new StalwartClientException("Failed to create account: empty response from API.");
    }

    /// <summary>Updates an existing account.</summary>
    public async Task<AccountResponse> UpdateAccountAsync(string accountId, AccountRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accountId))
            throw new ArgumentException("Account ID cannot be null or empty.", nameof(accountId));
        if (request == null) throw new ArgumentNullException(nameof(request));
        var url = $"{BaseUrl}/api/accounts/{Uri.EscapeDataString(accountId)}";
        var response = await SendRequestAsync<AccountResponse>(HttpMethod.Put, url, request, true, cancellationToken).ConfigureAwait(false);
        return response.Data ?? throw StalwartClientException.ForNotFound("Account", accountId);
    }

    /// <summary>Deletes an account.</summary>
    public async Task DeleteAccountAsync(string accountId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accountId))
            throw new ArgumentException("Account ID cannot be null or empty.", nameof(accountId));
        var url = $"{BaseUrl}/api/accounts/{Uri.EscapeDataString(accountId)}";
        await SendRequestAsync<object>(HttpMethod.Delete, url, null, true, cancellationToken).ConfigureAwait(false);
    }

    // ========================================================================
    // Alias Operations
    // ========================================================================

    /// <summary>Gets an alias by its ID.</summary>
    public async Task<AliasResponse> GetAliasAsync(string aliasId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(aliasId))
            throw new ArgumentException("Alias ID cannot be null or empty.", nameof(aliasId));
        var url = $"{BaseUrl}/api/aliases/{Uri.EscapeDataString(aliasId)}";
        var response = await SendRequestAsync<AliasResponse>(HttpMethod.Get, url, null, true, cancellationToken).ConfigureAwait(false);
        return response.Data ?? throw StalwartClientException.ForNotFound("Alias", aliasId);
    }

    /// <summary>Lists all aliases with optional pagination.</summary>
    public async Task<AliasListResponse> ListAliasesAsync(string? domainId = null, string? accountId = null, int offset = 0, int limit = 100, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/api/aliases?offset={offset}&limit={limit}";
        if (!string.IsNullOrEmpty(domainId))
            url += $"&domain_id={Uri.EscapeDataString(domainId)}";
        if (!string.IsNullOrEmpty(accountId))
            url += $"&account_id={Uri.EscapeDataString(accountId)}";
        var response = await SendRequestAsync<AliasListResponse>(HttpMethod.Get, url, null, true, cancellationToken).ConfigureAwait(false);
        return response.Data ?? new AliasListResponse();
    }

    /// <summary>Creates a new alias.</summary>
    public async Task<AliasResponse> CreateAliasAsync(AliasRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.Source))
            throw new ArgumentException("Alias source is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Destination))
            throw new ArgumentException("Alias destination is required.", nameof(request));
        var url = $"{BaseUrl}/api/aliases";
        var response = await SendRequestAsync<AliasResponse>(HttpMethod.Post, url, request, true, cancellationToken).ConfigureAwait(false);
        return response.Data ?? throw new StalwartClientException("Failed to create alias: empty response from API.");
    }

    /// <summary>Updates an existing alias.</summary>
    public async Task<AliasResponse> UpdateAliasAsync(string aliasId, AliasRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(aliasId))
            throw new ArgumentException("Alias ID cannot be null or empty.", nameof(aliasId));
        if (request == null) throw new ArgumentNullException(nameof(request));
        var url = $"{BaseUrl}/api/aliases/{Uri.EscapeDataString(aliasId)}";
        var response = await SendRequestAsync<AliasResponse>(HttpMethod.Put, url, request, true, cancellationToken).ConfigureAwait(false);
        return response.Data ?? throw StalwartClientException.ForNotFound("Alias", aliasId);
    }

    /// <summary>Deletes an alias.</summary>
    public async Task DeleteAliasAsync(string aliasId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(aliasId))
            throw new ArgumentException("Alias ID cannot be null or empty.", nameof(aliasId));
        var url = $"{BaseUrl}/api/aliases/{Uri.EscapeDataString(aliasId)}";
        await SendRequestAsync<object>(HttpMethod.Delete, url, null, true, cancellationToken).ConfigureAwait(false);
    }

    // ========================================================================
    // OpenAPI Management API Endpoints
    // ========================================================================

    /// <summary>
    /// Discovers the OpenID Connect provider for an email address.
    /// GET /api/discover/{email}
    /// </summary>
    /// <param name="email">The email address or account name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>OIDC discovery document.</returns>
    public async Task<Dictionary<string, object>> DiscoverOidcProviderAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be null or empty.", nameof(email));
        var url = $"{BaseUrl}/api/discover/{Uri.EscapeDataString(email)}";
        var response = await SendRequestInternalAsync<Dictionary<string, object>>(
            HttpMethod.Get, url, null, false, cancellationToken).ConfigureAwait(false);
        return response.Data ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// Gets the authenticated account's permissions, edition and locale.
    /// GET /api/account
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Account information.</returns>
    public async Task<AccountInfoResponse> GetAccountInfoAsync(CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/api/account";
        var response = await SendRequestAsync<AccountInfoResponse>(
            HttpMethod.Get, url, null, true, cancellationToken).ConfigureAwait(false);
        return response.Data ?? new AccountInfoResponse();
    }

    /// <summary>
    /// Gets the configuration schema redirect.
    /// GET /api/schema - Redirects to /api/schema/{hash}
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Schema hash redirect URL.</returns>
    public async Task<string> GetSchemaRedirectAsync(CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/api/schema";
        var response = await SendRequestInternalAsync<object>(
            HttpMethod.Get, url, null, true, cancellationToken).ConfigureAwait(false);
        // Follow redirect manually or return the Location header
        if (response.Data != null)
            return response.Data.ToString() ?? string.Empty;
        return string.Empty;
    }

    /// <summary>
    /// Gets the configuration schema at a specific hash.
    /// GET /api/schema/{hash}
    /// </summary>
    /// <param name="hash">SHA-256 hex digest of the configuration schema.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>JSON Schema document (gzipped).</returns>
    public async Task<byte[]> GetSchemaAsync(string hash, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(hash))
            throw new ArgumentException("Hash cannot be null or empty.", nameof(hash));
        var url = $"{BaseUrl}/api/schema/{Uri.EscapeDataString(hash)}";
        var response = await SendRequestInternalAsync<byte[]>(
            HttpMethod.Get, url, null, true, cancellationToken).ConfigureAwait(false);
        return response.Data ?? Array.Empty<byte>();
    }

    /// <summary>
    /// Issues a short-lived token for live delivery diagnostics.
    /// GET /api/token/delivery
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Short-lived delivery token.</returns>
    public async Task<string> IssueDeliveryTokenAsync(CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/api/token/delivery";
        var response = await SendRequestAsync<string>(
            HttpMethod.Get, url, null, true, cancellationToken).ConfigureAwait(false);
        return response.Data ?? string.Empty;
    }

    /// <summary>
    /// Issues a short-lived token for live tracing (Enterprise only).
    /// GET /api/token/tracing
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Short-lived tracing token, or empty if not available in this edition.</returns>
    public async Task<string> IssueTracingTokenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{BaseUrl}/api/token/tracing";
            var response = await SendRequestAsync<string>(
                HttpMethod.Get, url, null, true, cancellationToken).ConfigureAwait(false);
            return response.Data ?? string.Empty;
        }
        catch (StalwartClientException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Enterprise feature not available
            return string.Empty;
        }
    }

    /// <summary>
    /// Issues a short-lived token for live metrics (Enterprise only).
    /// GET /api/token/metrics
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Short-lived metrics token, or empty if not available in this edition.</returns>
    public async Task<string> IssueMetricsTokenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"{BaseUrl}/api/token/metrics";
            var response = await SendRequestAsync<string>(
                HttpMethod.Get, url, null, true, cancellationToken).ConfigureAwait(false);
            return response.Data ?? string.Empty;
        }
        catch (StalwartClientException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Enterprise feature not available
            return string.Empty;
        }
    }

    // ========================================================================
    // Internal Request Handling
    // ========================================================================

    private async Task<ApiResponse<T>> SendRequestAsync<T>(
        HttpMethod method, string url, object? data, bool requiresAuth, CancellationToken cancellationToken)
    {
        if (requiresAuth)
            await EnsureAuthenticatedAsync(cancellationToken).ConfigureAwait(false);
        return await SendRequestInternalAsync<T>(method, url, data, requiresAuth, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ApiResponse<T>> SendRequestInternalAsync<T>(
        HttpMethod method, string url, object? data, bool requiresAuth, CancellationToken cancellationToken)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            using var request = new HttpRequestMessage(method, url);
            if (requiresAuth && _currentToken != null)
                request.Headers.Authorization = new AuthenticationHeaderValue(
                    _currentToken.TokenType ?? "Bearer", _currentToken.AccessToken);
            if (data != null)
            {
                var json = JsonSerializer.Serialize(data, _options.JsonSerializerOptions);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }
            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (typeof(T) == typeof(string))
                    return new ApiResponse<T> { Success = true, Data = (T)(object)responseBody };
                var result = JsonSerializer.Deserialize<ApiResponse<T>>(responseBody, _options.JsonSerializerOptions);
                return result ?? new ApiResponse<T> { Success = true, Data = default };
            }
            else
            {
                var ex = await StalwartClientException.FromResponseAsync(response).ConfigureAwait(false);
                _logger.LogError(ex, "API request failed");
                throw ex;
            }
        }).ConfigureAwait(false);
    }

    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action)
    {
        int retryCount = 0;
        while (true)
        {
            try
            {
                return await action().ConfigureAwait(false);
            }
            catch (Exception ex) when (ShouldRetry(ex, retryCount))
            {
                retryCount++;
                var delay = CalculateRetryDelay(retryCount);
                _logger.LogWarning(ex, "Request failed (attempt {Attempt}/{MaxAttempts}). Retrying in {Delay}ms...",
                    retryCount, _options.MaxRetries, delay.TotalMilliseconds);
                await Task.Delay(delay).ConfigureAwait(false);
            }
        }
    }

    private bool ShouldRetry(Exception ex, int retryCount)
    {
        if (retryCount >= _options.MaxRetries) return false;
        if (ex is StalwartClientException clientEx && clientEx.StatusCode == HttpStatusCode.TooManyRequests)
            return true;
        if (ex is HttpRequestException || ex is TaskCanceledException || ex is TimeoutException ||
            ex.InnerException is HttpRequestException)
            return true;
        if (ex is StalwartClientException clientEx2 && clientEx2.StatusCode.HasValue &&
            clientEx2.StatusCode.Value >= HttpStatusCode.InternalServerError)
            return true;
        return false;
    }

    private TimeSpan CalculateRetryDelay(int retryCount)
    {
        var baseDelay = _options.RetryBaseDelay ?? TimeSpan.FromMilliseconds(100);
        var maxDelay = _options.RetryMaxDelay ?? TimeSpan.FromSeconds(30);
        var exponentialDelay = baseDelay * Math.Pow(2, retryCount - 1);
        var delay = TimeSpan.FromMilliseconds(Math.Min(exponentialDelay.TotalMilliseconds, maxDelay.TotalMilliseconds));
        var jitter = Random.Shared.NextDouble() * delay.TotalMilliseconds * 0.25;
        return delay + TimeSpan.FromMilliseconds(jitter);
    }

    // ========================================================================
    // IStalwartClient Interface Implementation
    // Using explicit interface implementation to avoid method name conflicts
    // with existing public methods that have different parameter types.
    // ========================================================================

    Task IStalwartClient.AuthenticateAsync(ApiCredentials credentials, CancellationToken cancellationToken)
        => AuthenticateAsync(credentials, cancellationToken);

    async Task<Domain> IStalwartClient.CreateDomainAsync(Domain domain, CancellationToken cancellationToken)
    {
        var request = new DomainRequest
        {
            Name = domain.Name,
            Description = domain.Description,
            Quota = domain.Quota,
            MaxAccounts = domain.MaxAccounts
        };
        var response = await CreateDomainAsync(request, cancellationToken).ConfigureAwait(false);
        return MapToDomain(response);
    }

    async Task<Domain?> IStalwartClient.GetDomainAsync(string domainName, CancellationToken cancellationToken)
    {
        var response = await GetDomainByNameAsync(domainName, cancellationToken).ConfigureAwait(false);
        return response != null ? MapToDomain(response) : null;
    }

    async Task<Account> IStalwartClient.CreateAccountAsync(Account account, CancellationToken cancellationToken)
    {
        var request = new AccountRequest
        {
            Name = account.Name,
            DisplayName = account.DisplayName,
            Password = account.Password,
            Quota = account.Quota,
            Enabled = account.IsEnabled,
            Forwarding = account.ForwardingAddresses
        };
        var response = await CreateAccountAsync(request, cancellationToken).ConfigureAwait(false);
        return MapToAccount(response);
    }

    async Task<Account> IStalwartClient.UpdateAccountAsync(string accountId, Account account, CancellationToken cancellationToken)
    {
        var request = new AccountRequest
        {
            Name = account.Name,
            DisplayName = account.DisplayName,
            Password = account.Password,
            Quota = account.Quota,
            Enabled = account.IsEnabled,
            Forwarding = account.ForwardingAddresses
        };
        var response = await UpdateAccountAsync(accountId, request, cancellationToken).ConfigureAwait(false);
        return MapToAccount(response);
    }

    async Task<EmailAlias> IStalwartClient.CreateAliasAsync(EmailAlias alias, CancellationToken cancellationToken)
    {
        var request = new AliasRequest
        {
            Source = alias.Source,
            Destination = alias.Destination
        };
        var response = await CreateAliasAsync(request, cancellationToken).ConfigureAwait(false);
        return MapToAlias(response);
    }

    Task<bool> IStalwartClient.ImportMessageAsync(string accountId, string emlContent, CancellationToken cancellationToken)
        => throw new NotImplementedException("Message import via API is not yet implemented. Use Vandelay for message migration.");

    Task<bool> IStalwartClient.ImportAttachmentAsync(string accountId, string messageId, byte[] attachmentData, string fileName, CancellationToken cancellationToken)
        => throw new NotImplementedException("Attachment import via API is not yet implemented. Use Vandelay for message migration.");

    async Task<bool> IStalwartClient.CheckHealthAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await HealthCheckAsync(cancellationToken).ConfigureAwait(false);
            return response != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Maps DomainResponse to Domain model.</summary>
    private static Domain MapToDomain(DomainResponse response)
        => new Domain
        {
            Id = response.Id,
            Name = response.Name ?? string.Empty,
            Description = response.Description,
            IsEnabled = response.Enabled,
            Quota = response.Quota,
            MaxAccounts = response.MaxAccounts,
            CreatedAt = response.CreatedAt,
            UpdatedAt = response.UpdatedAt
        };

    /// <summary>Maps AccountResponse to Account model.</summary>
    private static Account MapToAccount(AccountResponse response)
        => new Account
        {
            Id = response.Id,
            Name = response.Name ?? string.Empty,
            Email = response.Email ?? string.Empty,
            DisplayName = response.DisplayName,
            IsEnabled = response.Enabled,
            Quota = response.Quota,
            UsedQuota = response.UsedQuota,
            ForwardingAddresses = response.Forwarding ?? new(),
            ForwardingEnabled = true,
            KeepForwardedCopy = response.KeepForwardedCopy,
            DomainId = response.DomainId,
            CreatedAt = response.CreatedAt,
            UpdatedAt = response.UpdatedAt
        };

    /// <summary>Maps AliasResponse to EmailAlias model.</summary>
    private static EmailAlias MapToAlias(AliasResponse response)
        => new EmailAlias
        {
            Id = response.Id,
            Source = response.Source ?? string.Empty,
            Destination = response.Destination ?? string.Empty,
            IsEnabled = response.Enabled,
            CreatedAt = response.CreatedAt,
            UpdatedAt = response.UpdatedAt
        };

    /// <summary>Disposes the HTTP client.</summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Configuration options for the StalwartClient.
/// </summary>
public class StalwartClientOptions
{
    /// <summary>Gets or sets the request timeout. Default is 30 seconds.</summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>Gets or sets the maximum number of retry attempts. Default is 3.</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Gets or sets the base delay for retry attempts. Default is 100ms.</summary>
    public TimeSpan? RetryBaseDelay { get; set; }

    /// <summary>Gets or sets the maximum delay for retry attempts. Default is 30 seconds.</summary>
    public TimeSpan? RetryMaxDelay { get; set; }

    /// <summary>Gets or sets the JSON serializer options.</summary>
    public JsonSerializerOptions JsonSerializerOptions { get; set; } = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
}
