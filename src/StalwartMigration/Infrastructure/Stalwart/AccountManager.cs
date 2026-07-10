// <copyright file="AccountManager.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StalwartMigration.Core.Models;

namespace StalwartMigration.Infrastructure.Stalwart;

/// <summary>
/// Manages domain, account, and alias operations for the Stalwart Mail Server.
/// Provides a higher-level abstraction over the StalwartClient for creating, updating,
/// and managing mail server infrastructure.
/// </summary>
public class AccountManager : IDisposable
{
    private readonly StalwartClient _client;
    private readonly ILogger<AccountManager> _logger;
    private bool _disposed;

    /// <summary>
    /// Gets the underlying Stalwart client.
    /// </summary>
    public StalwartClient Client => _client;

    /// <summary>
    /// Initializes a new instance of the AccountManager class.
    /// </summary>
    /// <param name="client">The Stalwart client to use for API operations.</param>
    /// <param name="logger">The logger instance.</param>
    public AccountManager(StalwartClient client, ILogger<AccountManager>? logger = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? NullLogger<AccountManager>.Instance;
    }

    /// <summary>
    /// Initializes a new instance of the AccountManager class with a base URL and credentials.
    /// </summary>
    /// <param name="baseUrl">The base URL for the Stalwart API.</param>
    /// <param name="credentials">The API credentials.</param>
    /// <param name="options">The client configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    public AccountManager(string baseUrl, ApiCredentials credentials, StalwartClientOptions? options = null, ILogger<AccountManager>? logger = null)
        : this(new StalwartClient(baseUrl, credentials, options), logger)
    {
    }

    // ========================================================================
    // Domain Management
    // ========================================================================

    /// <summary>
    /// Creates a new domain from the specified Domain model.
    /// </summary>
    /// <param name="domain">The domain to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created domain response.</returns>
    public async Task<DomainResponse> CreateDomainAsync(Domain domain, CancellationToken cancellationToken = default)
    {
        if (domain == null)
            throw new ArgumentNullException(nameof(domain));

        domain.ValidateAndThrow();

        var request = new DomainRequest
        {
            Name = domain.Name,
            DisplayName = domain.DisplayName,
            Description = domain.Description,
            MaxAccounts = domain.MaxAccounts,
            Quota = domain.Quota,

            DkimEnabled = domain.DkimEnabled,
            SpfEnabled = domain.SpfEnabled,
            DmarcEnabled = domain.DmarcEnabled
        };

        _logger.LogInformation("Creating domain: {DomainName}", domain.Name);
        var response = await _client.CreateDomainAsync(request, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Successfully created domain: {DomainName} (ID: {DomainId})", domain.Name, response.Id);
        return response;
    }

    /// <summary>
    /// Updates an existing domain with values from the Domain model.
    /// </summary>
    /// <param name="domainId">The domain ID to update.</param>
    /// <param name="domain">The domain with updated values.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated domain response.</returns>
    public async Task<DomainResponse> UpdateDomainAsync(string domainId, Domain domain, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domainId))
            throw new ArgumentException("Domain ID cannot be null or empty.", nameof(domainId));
        if (domain == null)
            throw new ArgumentNullException(nameof(domain));

        domain.ValidateAndThrow();

        var request = new DomainRequest
        {
            Name = domain.Name,
            DisplayName = domain.DisplayName,
            Description = domain.Description,
            MaxAccounts = domain.MaxAccounts,
            Quota = domain.Quota,

            DkimEnabled = domain.DkimEnabled,
            SpfEnabled = domain.SpfEnabled,
            DmarcEnabled = domain.DmarcEnabled
        };

        _logger.LogInformation("Updating domain: {DomainId}", domainId);
        var response = await _client.UpdateDomainAsync(domainId, request, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Successfully updated domain: {DomainId}", domainId);
        return response;
    }

    /// <summary>
    /// Gets a domain by its ID.
    /// </summary>
    public async Task<DomainResponse> GetDomainAsync(string domainId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting domain: {DomainId}", domainId);
        return await _client.GetDomainAsync(domainId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a domain by its name.
    /// </summary>
    public async Task<DomainResponse> GetDomainByNameAsync(string domainName, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting domain by name: {DomainName}", domainName);
        return await _client.GetDomainByNameAsync(domainName, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Lists all domains.
    /// </summary>
    public async Task<DomainListResponse> ListDomainsAsync(int offset = 0, int limit = 100, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing domains (offset={Offset}, limit={Limit})", offset, limit);
        return await _client.ListDomainsAsync(offset, limit, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes a domain by its ID.
    /// </summary>
    public async Task DeleteDomainAsync(string domainId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting domain: {DomainId}", domainId);
        await _client.DeleteDomainAsync(domainId, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Successfully deleted domain: {DomainId}", domainId);
    }

    /// <summary>
    /// Checks if a domain exists by name.
    /// </summary>
    public async Task<bool> DomainExistsAsync(string domainName, CancellationToken cancellationToken = default)
    {
        try
        {
            var domain = await GetDomainByNameAsync(domainName, cancellationToken).ConfigureAwait(false);
            return domain != null;
        }
        catch (StalwartClientException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    // ========================================================================
    // Account Management
    // ========================================================================

    /// <summary>
    /// Creates a new account from the specified Account model.
    /// </summary>
    /// <param name="account">The account to create.</param>
    /// <param name="domainId">The domain ID to associate with the account.</param>
    /// <param name="password">The account password (optional, can be set in the Account model).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created account response.</returns>
    public async Task<AccountResponse> CreateAccountAsync(Account account, string domainId, string? password = null, CancellationToken cancellationToken = default)
    {
        if (account == null)
            throw new ArgumentNullException(nameof(account));
        if (string.IsNullOrWhiteSpace(domainId))
            throw new ArgumentException("Domain ID cannot be null or empty.", nameof(domainId));

        account.ValidateAndThrow();

        // If password is provided, use it; otherwise use the account's password
        var accountPassword = password ?? account.Password ?? account.PasswordHash;

        var request = new AccountRequest
        {
            DomainId = domainId,
            Name = account.Name,
            DisplayName = account.DisplayName,
            Password = accountPassword,
            Quota = account.Quota,
            MaxMessages = account.MaxMessages,
            Enabled = account.IsEnabled,
            IsAdmin = account.IsAdmin,
            Forwarding = account.ForwardingAddresses,
            KeepForwardedCopy = account.KeepForwardedCopy
        };

        _logger.LogInformation("Creating account: {AccountName} in domain: {DomainId}", account.Name, domainId);
        var response = await _client.CreateAccountAsync(request, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Successfully created account: {AccountName} (ID: {AccountId})", account.Name, response.Id);
        return response;
    }

    /// <summary>
    /// Updates an existing account with values from the Account model.
    /// </summary>
    /// <param name="accountId">The account ID to update.</param>
    /// <param name="account">The account with updated values.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated account response.</returns>
    public async Task<AccountResponse> UpdateAccountAsync(string accountId, Account account, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accountId))
            throw new ArgumentException("Account ID cannot be null or empty.", nameof(accountId));
        if (account == null)
            throw new ArgumentNullException(nameof(account));

        account.ValidateAndThrow();

        var request = new AccountRequest
        {
            DomainId = account.DomainId,
            Name = account.Name,
            DisplayName = account.DisplayName,
            Password = account.Password ?? account.PasswordHash,
            Quota = account.Quota,
            MaxMessages = account.MaxMessages,
            Enabled = account.IsEnabled,
            IsAdmin = account.IsAdmin,
            Forwarding = account.ForwardingAddresses,
            KeepForwardedCopy = account.KeepForwardedCopy
        };

        _logger.LogInformation("Updating account: {AccountId}", accountId);
        var response = await _client.UpdateAccountAsync(accountId, request, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Successfully updated account: {AccountId}", accountId);
        return response;
    }

    /// <summary>
    /// Gets an account by its ID.
    /// </summary>
    public async Task<AccountResponse> GetAccountAsync(string accountId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting account: {AccountId}", accountId);
        return await _client.GetAccountAsync(accountId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets an account by its email address.
    /// </summary>
    public async Task<AccountResponse> GetAccountByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting account by email: {Email}", email);
        return await _client.GetAccountByEmailAsync(email, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Lists all accounts, optionally filtered by domain.
    /// </summary>
    public async Task<AccountListResponse> ListAccountsAsync(string? domainId = null, int offset = 0, int limit = 100, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing accounts (domainId={DomainId}, offset={Offset}, limit={Limit})",
            domainId ?? "all", offset, limit);
        return await _client.ListAccountsAsync(domainId, offset, limit, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes an account by its ID.
    /// </summary>
    public async Task DeleteAccountAsync(string accountId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting account: {AccountId}", accountId);
        await _client.DeleteAccountAsync(accountId, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Successfully deleted account: {AccountId}", accountId);
    }

    /// <summary>
    /// Checks if an account exists by email address.
    /// </summary>
    public async Task<bool> AccountExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var account = await GetAccountByEmailAsync(email, cancellationToken).ConfigureAwait(false);
            return account != null;
        }
        catch (StalwartClientException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets all accounts for a specific domain.
    /// </summary>
    public async Task<List<AccountResponse>> GetAccountsByDomainAsync(string domainId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domainId))
            throw new ArgumentException("Domain ID cannot be null or empty.", nameof(domainId));

        var result = new List<AccountResponse>();
        int offset = 0;
        const int limit = 100;

        while (true)
        {
            var response = await ListAccountsAsync(domainId, offset, limit, cancellationToken).ConfigureAwait(false);
            if (response.Accounts == null || response.Accounts.Count == 0)
                break;

            result.AddRange(response.Accounts);
            offset += limit;

            if (offset >= response.Total)
                break;
        }

        return result;
    }

    // ========================================================================
    // Alias Management
    // ========================================================================

    /// <summary>
    /// Creates a new alias from the specified EmailAlias model.
    /// </summary>
    /// <param name="alias">The alias to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created alias response.</returns>
    public async Task<AliasResponse> CreateAliasAsync(EmailAlias alias, CancellationToken cancellationToken = default)
    {
        if (alias == null)
            throw new ArgumentNullException(nameof(alias));

        alias.ValidateAndThrow();

        var request = new AliasRequest
        {
            Source = alias.Source,
            Destination = alias.Destination
        };

        _logger.LogInformation("Creating alias: {Source} -> {Destination}", alias.Source, alias.Destination);
        var response = await _client.CreateAliasAsync(request, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Successfully created alias: {Source} -> {Destination} (ID: {AliasId})",
            alias.Source, alias.Destination, response.Id);
        return response;
    }

    /// <summary>
    /// Updates an existing alias with values from the EmailAlias model.
    /// </summary>
    /// <param name="aliasId">The alias ID to update.</param>
    /// <param name="alias">The alias with updated values.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated alias response.</returns>
    public async Task<AliasResponse> UpdateAliasAsync(string aliasId, EmailAlias alias, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(aliasId))
            throw new ArgumentException("Alias ID cannot be null or empty.", nameof(aliasId));
        if (alias == null)
            throw new ArgumentNullException(nameof(alias));

        alias.ValidateAndThrow();

        var request = new AliasRequest
        {
            Source = alias.Source,
            Destination = alias.Destination
        };

        _logger.LogInformation("Updating alias: {AliasId}", aliasId);
        var response = await _client.UpdateAliasAsync(aliasId, request, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Successfully updated alias: {AliasId}", aliasId);
        return response;
    }

    /// <summary>
    /// Gets an alias by its ID.
    /// </summary>
    public async Task<AliasResponse> GetAliasAsync(string aliasId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting alias: {AliasId}", aliasId);
        return await _client.GetAliasAsync(aliasId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Lists all aliases, optionally filtered by domain or account.
    /// </summary>
    public async Task<AliasListResponse> ListAliasesAsync(string? domainId = null, string? accountId = null, int offset = 0, int limit = 100, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing aliases (domainId={DomainId}, accountId={AccountId}, offset={Offset}, limit={Limit})",
            domainId ?? "all", accountId ?? "all", offset, limit);
        return await _client.ListAliasesAsync(domainId, accountId, offset, limit, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes an alias by its ID.
    /// </summary>
    public async Task DeleteAliasAsync(string aliasId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting alias: {AliasId}", aliasId);
        await _client.DeleteAliasAsync(aliasId, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Successfully deleted alias: {AliasId}", aliasId);
    }

    /// <summary>
    /// Gets all aliases for a specific domain.
    /// </summary>
    public async Task<List<AliasResponse>> GetAliasesByDomainAsync(string domainId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domainId))
            throw new ArgumentException("Domain ID cannot be null or empty.", nameof(domainId));

        var result = new List<AliasResponse>();
        int offset = 0;
        const int limit = 100;

        while (true)
        {
            var response = await ListAliasesAsync(domainId, null, offset, limit, cancellationToken).ConfigureAwait(false);
            if (response.Aliases == null || response.Aliases.Count == 0)
                break;

            result.AddRange(response.Aliases);
            offset += limit;

            if (offset >= response.Total)
                break;
        }

        return result;
    }

    /// <summary>
    /// Gets all aliases for a specific account.
    /// </summary>
    public async Task<List<AliasResponse>> GetAliasesByAccountAsync(string accountId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accountId))
            throw new ArgumentException("Account ID cannot be null or empty.", nameof(accountId));

        var result = new List<AliasResponse>();
        int offset = 0;
        const int limit = 100;

        while (true)
        {
            var response = await ListAliasesAsync(null, accountId, offset, limit, cancellationToken).ConfigureAwait(false);
            if (response.Aliases == null || response.Aliases.Count == 0)
                break;

            result.AddRange(response.Aliases);
            offset += limit;

            if (offset >= response.Total)
                break;
        }

        return result;
    }

    // ========================================================================
    // Health Check
    // ========================================================================

    /// <summary>
    /// Checks the health of the Stalwart API.
    /// </summary>
    public async Task<HealthCheckResponse> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Performing health check");
        return await _client.HealthCheckAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Checks if the Stalwart API is available and healthy.
    /// </summary>
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await HealthCheckAsync(cancellationToken).ConfigureAwait(false);
            return response.IsHealthy;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the authenticated account's permissions, edition and locale.
    /// Uses the Management API /api/account endpoint.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Account information.</returns>
    public async Task<AccountInfoResponse> GetAccountInfoAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting account information");
        return await _client.GetAccountInfoAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Discovers the OpenID Connect provider for an email address.
    /// </summary>
    /// <param name="email">The email address or account name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>OIDC discovery document.</returns>
    public async Task<Dictionary<string, object>> DiscoverOidcProviderAsync(string email, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Discovering OIDC provider for {Email}", email);
        return await _client.DiscoverOidcProviderAsync(email, cancellationToken).ConfigureAwait(false);
    }

    // ========================================================================
    // Bulk Operations
    // ========================================================================

    /// <summary>
    /// Creates multiple domains in a batch.
    /// </summary>
    /// <param name="domains">The list of domains to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of created domain responses.</returns>
    public async Task<List<DomainResponse>> CreateDomainsAsync(IEnumerable<Domain> domains, CancellationToken cancellationToken = default)
    {
        if (domains == null)
            throw new ArgumentNullException(nameof(domains));

        var results = new List<DomainResponse>();
        foreach (var domain in domains)
        {
            var response = await CreateDomainAsync(domain, cancellationToken).ConfigureAwait(false);
            results.Add(response);
        }
        return results;
    }

    /// <summary>
    /// Creates multiple accounts in a batch for a specific domain.
    /// </summary>
    /// <param name="domainId">The domain ID to associate with the accounts.</param>
    /// <param name="accounts">The list of accounts to create.</param>
    /// <param name="defaultPassword">The default password to use for all accounts (can be overridden per account).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of created account responses.</returns>
    public async Task<List<AccountResponse>> CreateAccountsAsync(
        string domainId, IEnumerable<Account> accounts, string? defaultPassword = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domainId))
            throw new ArgumentException("Domain ID cannot be null or empty.", nameof(domainId));
        if (accounts == null)
            throw new ArgumentNullException(nameof(accounts));

        var results = new List<AccountResponse>();
        foreach (var account in accounts)
        {
            var response = await CreateAccountAsync(account, domainId, defaultPassword, cancellationToken).ConfigureAwait(false);
            results.Add(response);
        }
        return results;
    }

    /// <summary>
    /// Creates multiple aliases in a batch.
    /// </summary>
    /// <param name="aliases">The list of aliases to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of created alias responses.</returns>
    public async Task<List<AliasResponse>> CreateAliasesAsync(IEnumerable<EmailAlias> aliases, CancellationToken cancellationToken = default)
    {
        if (aliases == null)
            throw new ArgumentNullException(nameof(aliases));

        var results = new List<AliasResponse>();
        foreach (var alias in aliases)
        {
            var response = await CreateAliasAsync(alias, cancellationToken).ConfigureAwait(false);
            results.Add(response);
        }
        return results;
    }

    /// <summary>
    /// Disposes the underlying client.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _client.Dispose();
            _disposed = true;
        }
    }
}
