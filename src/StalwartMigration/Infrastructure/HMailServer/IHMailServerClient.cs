// <copyright file="IHMailServerClient.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using StalwartMigration.Core.Models;

namespace StalwartMigration.Infrastructure.HMailServer;

/// <summary>
/// Interface for the hMailServer client.
/// This allows for mocking and testing of the exporter/importer classes.
/// </summary>
public interface IHMailServerClient : IDisposable
{
    /// <summary>
    /// Gets whether the COM API is available and connected.
    /// </summary>
    bool IsComAvailable { get; }

    /// <summary>
    /// Gets the hMailServer version.
    /// </summary>
    string? Version { get; }

    /// <summary>
    /// Authenticates with hMailServer using administrator credentials.
    /// </summary>
    /// <param name="password">The administrator password.</param>
    void Authenticate(string password);

    /// <summary>
    /// Gets all domains from hMailServer.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of domains.</returns>
    Task<List<Domain>> GetDomainsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific domain by name.
    /// </summary>
    /// <param name="domainName">The domain name to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The domain or null if not found.</returns>
    Task<Domain?> GetDomainByNameAsync(string domainName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all accounts from hMailServer, optionally filtered by domain.
    /// </summary>
    /// <param name="domainId">Optional domain ID to filter by.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of accounts.</returns>
    Task<List<Account>> GetAccountsAsync(string? domainId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all email aliases from hMailServer.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of email aliases.</returns>
    Task<List<EmailAlias>> GetAliasesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all aliases for a specific domain.
    /// </summary>
    /// <param name="domainId">The domain ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of email aliases for the domain.</returns>
    Task<List<EmailAlias>> GetAliasesByDomainAsync(string domainId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all email messages for a specific account.
    /// </summary>
    /// <param name="accountId">The account ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>List of email messages.</returns>
    Task<List<EmailMessage>> GetMessagesAsync(string accountId, CancellationToken cancellationToken = default);
}
