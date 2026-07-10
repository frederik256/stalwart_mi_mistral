// <copyright file="IStalwartClient.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using StalwartMigration.Core.Models;

namespace StalwartMigration.Infrastructure.Stalwart;

/// <summary>
/// Interface for the Stalwart API client.
/// This allows for mocking and testing of the importer classes.
/// </summary>
public interface IStalwartClient : IDisposable
{
    /// <summary>
    /// Gets the base URL for the Stalwart API.
    /// </summary>
    string BaseUrl { get; }

    /// <summary>
    /// Gets whether the client is currently authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Authenticates with the Stalwart API.
    /// </summary>
    /// <param name="credentials">The API credentials.</param>
    Task AuthenticateAsync(ApiCredentials credentials, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a domain in Stalwart.
    /// </summary>
    /// <param name="domain">The domain to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created domain.</returns>
    Task<Domain> CreateDomainAsync(Domain domain, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a domain by name.
    /// </summary>
    /// <param name="domainName">The domain name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The domain or null if not found.</returns>
    Task<Domain?> GetDomainAsync(string domainName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an account in Stalwart.
    /// </summary>
    /// <param name="account">The account to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created account.</returns>
    Task<Account> CreateAccountAsync(Account account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an account in Stalwart.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="account">The account data to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated account.</returns>
    Task<Account> UpdateAccountAsync(string accountId, Account account, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an alias in Stalwart.
    /// </summary>
    /// <param name="alias">The alias to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created alias.</returns>
    Task<EmailAlias> CreateAliasAsync(EmailAlias alias, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports an email message.
    /// </summary>
    /// <param name="accountId">The account ID to import the message to.</param>
    /// <param name="emlContent">The EML content of the message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the import was successful.</returns>
    Task<bool> ImportMessageAsync(string accountId, string emlContent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a binary attachment.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="messageId">The message ID.</param>
    /// <param name="attachmentData">The attachment binary data.</param>
    /// <param name="fileName">The attachment file name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the import was successful.</returns>
    Task<bool> ImportAttachmentAsync(string accountId, string messageId, byte[] attachmentData, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the Stalwart API is healthy.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the API is healthy.</returns>
    Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers the OpenID Connect provider for an email address.
    /// </summary>
    /// <param name="email">The email address or account name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>OIDC discovery document.</returns>
    Task<System.Collections.Generic.Dictionary<string, object>> DiscoverOidcProviderAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the authenticated account's permissions, edition and locale.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Account information.</returns>
    Task<AccountInfoResponse> GetAccountInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the configuration schema redirect.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Schema hash redirect URL.</returns>
    Task<string> GetSchemaRedirectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the configuration schema at a specific hash.
    /// </summary>
    /// <param name="hash">SHA-256 hex digest of the configuration schema.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>JSON Schema document (gzipped).</returns>
    Task<byte[]> GetSchemaAsync(string hash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a short-lived token for live delivery diagnostics.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Short-lived delivery token.</returns>
    Task<string> IssueDeliveryTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a short-lived token for live tracing (Enterprise only).
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Short-lived tracing token, or empty if not available in this edition.</returns>
    Task<string> IssueTracingTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a short-lived token for live metrics (Enterprise only).
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Short-lived metrics token, or empty if not available in this edition.</returns>
    Task<string> IssueMetricsTokenAsync(CancellationToken cancellationToken = default);
}
