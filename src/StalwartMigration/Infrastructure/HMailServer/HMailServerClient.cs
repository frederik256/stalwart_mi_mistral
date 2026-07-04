// <copyright file="HMailServerClient.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StalwartMigration.Core.Models;
using StalwartMigration.Utilities.Helpers;

namespace StalwartMigration.Infrastructure.HMailServer;

/// <summary>
/// Client for communicating with hMailServer via its COM API.
/// This is the primary method for extracting data from hMailServer.
/// Falls back to database access if COM API is unavailable.
/// </summary>
public class HMailServerClient : IHMailServerClient
{
    private readonly HMailServerDatabase? _databaseFallback;
    private readonly ILogger<HMailServerClient> _logger;
    private dynamic? _application;
    private dynamic? _server;
    private bool _disposed;
    private string? _version;

    /// <summary>
    /// Gets whether the COM API is available and connected.
    /// </summary>
    public bool IsComAvailable { get; private set; }

    /// <summary>
    /// Gets the hMailServer version.
    /// </summary>
    public string? Version => _version;

    /// <summary>
    /// Gets the fallback database client.
    /// </summary>
    public HMailServerDatabase? DatabaseFallback => _databaseFallback;

    /// <summary>
    /// Initializes a new instance of the HMailServerClient class.
    /// </summary>
    /// <param name="databaseConnectionString">Optional database connection string for fallback.</param>
    /// <param name="databaseType">The database type for fallback.</param>
    /// <param name="logger">The logger instance.</param>
    public HMailServerClient(string? databaseConnectionString = null, DatabaseType databaseType = DatabaseType.SQLite, ILogger<HMailServerClient>? logger = null)
    {
        _logger = logger ?? NullLogger<HMailServerClient>.Instance;

        // Initialize COM API
        try
        {
            InitializeComApi();
            IsComAvailable = true;
            _logger.LogInformation("hMailServer COM API initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize hMailServer COM API.");
            throw new HMailServerException("Failed to initialize hMailServer COM API.", ex)
            {
                FailedOperation = "Initialization",
                Remediation = "Install hMailServer on this machine."
            };
        }

        // Note: Database fallback removed per architecture decision
        // Only COM API is used for hMailServer infrastructure access
    }

    /// <summary>
    /// Initializes the COM API connection to hMailServer.
    /// </summary>
    private void InitializeComApi()
    {
        try
        {
            // Create hMailServer Application COM object
            Type? hmailType = Type.GetTypeFromProgID("hMailServer.Application");
            
            if (hmailType == null)
            {
                throw new COMException("hMailServer.Application COM object not found. hMailServer may not be installed.");
            }

            _application = Activator.CreateInstance(hmailType);
            
            if (_application == null)
            {
                throw new COMException("Failed to create hMailServer.Application instance.");
            }

            // Connect to the local server
            _server = _application.Server;
            
            if (_server == null)
            {
                throw new COMException("Failed to access hMailServer Server object.");
            }

            // Get version
            _version = GetVersionFromCom();
        }
        catch (COMException comEx)
        {
            throw new HMailServerException("COM initialization failed.", comEx)
            {
                FailedOperation = "COM Initialization",
                ComErrorCode = comEx.ErrorCode,
                Remediation = "Ensure hMailServer is installed and COM components are registered. Run hMailServer as administrator at least once."
            };
        }
    }

    /// <summary>
    /// Tests the COM API connection to ensure it's still active.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the connection is healthy, false otherwise.</returns>
    public async Task<bool> TestComConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_server == null || _application == null)
            {
                _logger.LogWarning("COM API not initialized");
                return false;
            }

            // Try to access a simple property to test the connection
            var version = _server.Version?.ToString();
            if (version != null)
            {
                _logger.LogDebug("COM connection health check passed");
                return true;
            }
            
            _logger.LogWarning("COM connection health check failed: unable to access Version property");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "COM connection health check failed");
            return false;
        }
    }

    /// <summary>
    /// Gets the hMailServer version from COM API.
    /// </summary>
    private string? GetVersionFromCom()
    {
        try
        {
            if (_server != null)
            {
                var version = _server.Version ?? string.Empty;
                return version.ToString();
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve hMailServer version from COM");
            return null;
        }
    }

    /// <summary>
    /// Authenticates with hMailServer using administrator credentials.
    /// </summary>
    /// <param name="password">The administrator password.</param>
    public void Authenticate(string password)
    {
        if (!IsComAvailable && _databaseFallback == null)
        {
            throw new HMailServerException("No connection method available.")
            {
                FailedOperation = "Authentication",
                Remediation = "Initialize with COM API or database connection first."
            };
        }

        if (IsComAvailable)
        {
            try
            {
                if (_server != null)
                {
                    // hMailServer COM API uses Authenticate method
                    bool result = (bool)_server.Authenticate("Administrator", password);
                    
                    if (!result)
                    {
                        throw HMailServerException.ForAuthenticationError(
                            "Invalid administrator password for hMailServer.");
                    }

                    _logger.LogInformation("Authenticated with hMailServer via COM API");
                }
            }
            catch (COMException comEx)
            {
                throw HMailServerException.ForAuthenticationError(comEx.Message);
            }
        }
        // Database fallback doesn't require separate authentication
        // as it uses the connection string credentials
    }

    /// <summary>
    /// Gets all domains from hMailServer.
    /// </summary>
    public async Task<List<Domain>> GetDomainsAsync(CancellationToken cancellationToken = default)
    {
        if (IsComAvailable)
        {
            return await GetDomainsFromComAsync(cancellationToken).ConfigureAwait(false);
        }
        else if (_databaseFallback != null)
        {
            return await _databaseFallback.GetDomainsAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            throw new HMailServerException("No connection method available.")
            {
                FailedOperation = "GetDomains",
                Remediation = "Initialize with COM API or database connection first."
            };
        }
    }

    /// <summary>
    /// Gets all domains using COM API.
    /// </summary>
    private async Task<List<Domain>> GetDomainsFromComAsync(CancellationToken cancellationToken)
    {
        var domains = new List<Domain>();

        try
        {
            if (_server == null)
            {
                throw new HMailServerException("COM server not initialized.")
                {
                    FailedOperation = "GetDomains",
                    Remediation = "Reinitialize the client."
                };
            }

            // hMailServer COM: Server.Domains returns a Domains collection
            dynamic domainsCollection = _server.Domains;
            
            if (domainsCollection == null)
            {
                throw new HMailServerException("Failed to access Domains collection.")
                {
                    FailedOperation = "GetDomains",
                    ResourceType = "Domains",
                    Remediation = "Check hMailServer installation and COM API accessibility."
                };
            }

            int domainCount = domainsCollection.Count;
            _logger.LogDebug("Found {Count} domains in hMailServer", domainCount);

            // Iterate through domains (hMailServer COM uses 1-based indexing)
            for (int i = 0; i < domainCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                dynamic domainObj = domainsCollection.Item[i];
                
                if (domainObj == null)
                    continue;

                var domain = new Domain
                {
                    Id = domainObj.ID?.ToString(),
                    Name = domainObj.Name?.ToString() ?? string.Empty,
                    Description = domainObj.Description?.ToString(),
                    IsEnabled = (bool)domainObj.Enabled,
                    MaxAccounts = domainObj.MaxNumberOfAccounts,
                    Quota = ConvertMaxSizeToBytes(domainObj.MaxSize),
                    UsedQuota = ConvertMaxSizeToBytes(domainObj.MaxSizeUsed),
                    DkimEnabled = domainObj.DKIMSigningEnabled,
                    SpfEnabled = domainObj.SPFCheckingEnabled,
                    DmarcEnabled = domainObj.DMARCCheckingEnabled,
                    IsDefault = domainObj.IsDefault,
                    CatchAllEnabled = domainObj.CatchAllEnabled,
                    CatchAllEmail = domainObj.CatchAllEmailAddress?.ToString()
                };

                // Normalize domain name
                domain.Name = DomainValidator.Normalize(domain.Name);
                domains.Add(domain);
            }

            _logger.LogInformation("Retrieved {Count} domains from hMailServer via COM API", domains.Count);
        }
        catch (COMException comEx)
        {
            throw new HMailServerException("COM error while retrieving domains.", comEx)
            {
                FailedOperation = "GetDomains",
                ComErrorCode = comEx.ErrorCode,
                ResourceType = "Domain",
                Remediation = "Check hMailServer COM API accessibility and try again."
            };
        }

        return domains;
    }

    /// <summary>
    /// Gets a specific domain by name.
    /// </summary>
    /// <param name="domainName">The domain name to retrieve.</param>
    public async Task<Domain?> GetDomainByNameAsync(string domainName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domainName))
            throw new ArgumentException("Domain name cannot be null or empty.", nameof(domainName));

        var normalizedName = DomainValidator.Normalize(domainName);

        if (IsComAvailable)
        {
            return await GetDomainByNameFromComAsync(normalizedName, cancellationToken).ConfigureAwait(false);
        }
        else if (_databaseFallback != null)
        {
            return await _databaseFallback.GetDomainByNameAsync(normalizedName, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            throw new HMailServerException("No connection method available.")
            {
                FailedOperation = "GetDomainByName",
                ResourceId = domainName,
                Remediation = "Initialize with COM API or database connection first."
            };
        }
    }

    /// <summary>
    /// Gets a specific domain by name using COM API.
    /// </summary>
    private async Task<Domain?> GetDomainByNameFromComAsync(string domainName, CancellationToken cancellationToken)
    {
        try
        {
            if (_server == null)
            {
                throw new HMailServerException("COM server not initialized.")
                {
                    FailedOperation = "GetDomainByName",
                    ResourceId = domainName,
                    Remediation = "Reinitialize the client."
                };
            }

            dynamic domainsCollection = _server.Domains;
            
            if (domainsCollection == null)
                return null;

            int domainCount = domainsCollection.Count;

            for (int i = 0; i < domainCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                dynamic domainObj = domainsCollection.Item[i];
                
                if (domainObj == null)
                    continue;

                string? name = domainObj.Name?.ToString();
                
                if (string.Equals(name, domainName, StringComparison.OrdinalIgnoreCase))
                {
                    return new Domain
                    {
                        Id = domainObj.ID?.ToString(),
                        Name = name,
                        Description = domainObj.Description?.ToString(),
                        IsEnabled = (bool)domainObj.Enabled,
                        MaxAccounts = domainObj.MaxNumberOfAccounts,
                        Quota = ConvertMaxSizeToBytes(domainObj.MaxSize),
                        UsedQuota = ConvertMaxSizeToBytes(domainObj.MaxSizeUsed),
                        DkimEnabled = domainObj.DKIMSigningEnabled,
                        SpfEnabled = domainObj.SPFCheckingEnabled,
                        DmarcEnabled = domainObj.DMARCCheckingEnabled,
                        IsDefault = domainObj.IsDefault,
                        CatchAllEnabled = domainObj.CatchAllEnabled,
                        CatchAllEmail = domainObj.CatchAllEmailAddress?.ToString()
                    };
                }
            }

            return null;
        }
        catch (COMException comEx)
        {
            throw new HMailServerException("COM error while retrieving domain.", comEx)
            {
                FailedOperation = "GetDomainByName",
                ComErrorCode = comEx.ErrorCode,
                ResourceType = "Domain",
                ResourceId = domainName,
                Remediation = "Check hMailServer COM API accessibility and try again."
            };
        }
    }

    /// <summary>
    /// Gets all accounts from hMailServer, optionally filtered by domain.
    /// </summary>
    /// <param name="domainId">Optional domain ID to filter by.</param>
    public async Task<List<Account>> GetAccountsAsync(string? domainId = null, CancellationToken cancellationToken = default)
    {
        if (IsComAvailable && domainId == null)
        {
            // COM API is better for getting all accounts
            return await GetAccountsFromComAsync(domainId, cancellationToken).ConfigureAwait(false);
        }
        else if (domainId != null && _databaseFallback != null)
        {
            // For domain-specific, use database if we have the domain ID
            return await _databaseFallback.GetAccountsAsync(domainId, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // Fallback to COM for domain-filtered accounts
            return await GetAccountsFromComAsync(domainId, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Gets accounts using COM API.
    /// </summary>
    private async Task<List<Account>> GetAccountsFromComAsync(string? domainId, CancellationToken cancellationToken)
    {
        var accounts = new List<Account>();

        try
        {
            if (_server == null)
            {
                throw new HMailServerException("COM server not initialized.")
                {
                    FailedOperation = "GetAccounts",
                    Remediation = "Reinitialize the client."
                };
            }

            dynamic domainsCollection = _server.Domains;
            
            if (domainsCollection == null)
            {
                throw new HMailServerException("Failed to access Domains collection.")
                {
                    FailedOperation = "GetAccounts",
                    Remediation = "Check hMailServer installation and COM API accessibility."
                };
            }

            int domainCount = domainsCollection.Count;

            for (int d = 0; d < domainCount; d++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                dynamic domainObj = domainsCollection.Item[d];
                
                if (domainObj == null)
                    continue;

                // If domain ID specified and doesn't match, skip
                if (domainId != null)
                {
                    string? currentDomainId = domainObj.ID?.ToString();
                    if (!string.Equals(currentDomainId, domainId, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                // Get accounts for this domain
                dynamic accountsCollection = domainObj.Accounts;
                
                if (accountsCollection == null)
                    continue;

                int accountCount = accountsCollection.Count;

                for (int a = 0; a < accountCount; a++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    dynamic accountObj = accountsCollection.Item[a];
                    
                    if (accountObj == null)
                        continue;

                    var account = new Account
                    {
                        Id = accountObj.ID?.ToString(),
                        DomainId = domainObj.ID?.ToString(),
                        Domain = domainObj.Name?.ToString() ?? string.Empty,
                        Name = accountObj.Name?.ToString() ?? string.Empty,
                        Email = accountObj.Address?.ToString() ?? string.Empty,
                        DisplayName = accountObj.DisplayName?.ToString(),
                        Quota = ConvertMaxSizeToBytes(accountObj.MaxSize),
                        UsedQuota = ConvertMaxSizeToBytes(accountObj.MaxSizeUsed),
                        IsEnabled = accountObj.Enabled,
                        IsAdmin = accountObj.IsAdmin,
                        ForwardingEnabled = accountObj.ForwardingEnabled,
                        KeepForwardedCopy = accountObj.KeepForwardedCopy
                    };

                    // Handle password (may be null for security reasons)
                    try
                    {
                        var password = accountObj.Password?.ToString();
                        if (!string.IsNullOrWhiteSpace(password))
                        {
                            account.Password = password;
                        }
                    }
                    catch
                    {
                        // Password may not be accessible via COM for security reasons
                    }

                    // Handle forwarding addresses
                    try
                    {
                        if (accountObj.ForwardingAddresses != null)
                        {
                            int forwardingCount = accountObj.ForwardingAddresses.Count;
                            for (int f = 0; f < forwardingCount; f++)
                            {
                                var forwardingObj = accountObj.ForwardingAddresses.Item[f];
                                if (forwardingObj != null)
                                {
                                    var address = forwardingObj.Address?.ToString();
                                    if (!string.IsNullOrWhiteSpace(address))
                                    {
                                        account.ForwardingAddresses.Add(address);
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Forwarding addresses may not be available
                    }

                    // Normalize email
                    if (account.Email != null)
                    {
                        account.Email = EmailValidator.Normalize(account.Email);
                    }
                    
                    // Normalize name if empty
                    if (string.IsNullOrWhiteSpace(account.Name) && account.Email != null)
                    {
                        account.Name = EmailValidator.ExtractLocalPart(account.Email);
                    }

                    accounts.Add(account);
                }
            }

            _logger.LogInformation("Retrieved {Count} accounts from hMailServer via COM API", accounts.Count);
        }
        catch (COMException comEx)
        {
            throw new HMailServerException("COM error while retrieving accounts.", comEx)
            {
                FailedOperation = "GetAccounts",
                ComErrorCode = comEx.ErrorCode,
                ResourceType = "Account",
                Remediation = "Check hMailServer COM API accessibility and try again."
            };
        }

        return accounts;
    }

    /// <summary>
    /// Gets all aliases from hMailServer.
    /// </summary>
    public async Task<List<EmailAlias>> GetAliasesAsync(CancellationToken cancellationToken = default)
    {
        if (IsComAvailable)
        {
            return await GetAliasesFromComAsync(cancellationToken).ConfigureAwait(false);
        }
        else if (_databaseFallback != null)
        {
            return await _databaseFallback.GetAliasesAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            throw new HMailServerException("No connection method available.")
            {
                FailedOperation = "GetAliases",
                ResourceType = "Alias",
                Remediation = "Initialize with COM API or database connection first."
            };
        }
    }

    /// <summary>
    /// Gets aliases using COM API.
    /// </summary>
    private async Task<List<EmailAlias>> GetAliasesFromComAsync(CancellationToken cancellationToken)
    {
        var aliases = new List<EmailAlias>();

        try
        {
            if (_server == null)
            {
                throw new HMailServerException("COM server not initialized.")
                {
                    FailedOperation = "GetAliases",
                    ResourceType = "Alias",
                    Remediation = "Reinitialize the client."
                };
            }

            // hMailServer stores aliases at the domain level
            dynamic domainsCollection = _server.Domains;
            
            if (domainsCollection == null)
            {
                throw new HMailServerException("Failed to access Domains collection.")
                {
                    FailedOperation = "GetAliases",
                    ResourceType = "Alias",
                    Remediation = "Check hMailServer installation and COM API accessibility."
                };
            }

            int domainCount = domainsCollection.Count;

            for (int d = 0; d < domainCount; d++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                dynamic domainObj = domainsCollection.Item[d];
                
                if (domainObj == null)
                    continue;

                // Get aliases for this domain
                dynamic aliasesCollection = domainObj.Aliases;
                
                if (aliasesCollection == null)
                    continue;

                int aliasCount = aliasesCollection.Count;

                for (int a = 0; a < aliasCount; a++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    dynamic aliasObj = aliasesCollection.Item[a];
                    
                    if (aliasObj == null)
                        continue;

                    var alias = new EmailAlias
                    {
                        Id = aliasObj.ID?.ToString(),
                        Source = aliasObj.Source?.ToString() ?? string.Empty,
                        Destination = aliasObj.Destination?.ToString() ?? string.Empty,
                        IsEnabled = aliasObj.Enabled
                    };

                    // Normalize email addresses
                    if (alias.Source != null)
                        alias.Source = EmailValidator.Normalize(alias.Source);
                    if (alias.Destination != null)
                        alias.Destination = EmailValidator.Normalize(alias.Destination);

                    aliases.Add(alias);
                }
            }

            _logger.LogInformation("Retrieved {Count} aliases from hMailServer via COM API", aliases.Count);
        }
        catch (COMException comEx)
        {
            throw new HMailServerException("COM error while retrieving aliases.", comEx)
            {
                FailedOperation = "GetAliases",
                ComErrorCode = comEx.ErrorCode,
                ResourceType = "Alias",
                Remediation = "Check hMailServer COM API accessibility and try again."
            };
        }

        return aliases;
    }

    /// <summary>
    /// Gets aliases for a specific domain.
    /// </summary>
    /// <param name="domainId">The domain ID to get aliases for.</param>
    public async Task<List<EmailAlias>> GetAliasesByDomainAsync(string domainId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(domainId))
            throw new ArgumentException("Domain ID cannot be null or empty.", nameof(domainId));

        if (IsComAvailable)
        {
            return await GetAliasesByDomainFromComAsync(domainId, cancellationToken).ConfigureAwait(false);
        }
        else if (_databaseFallback != null)
        {
            return await _databaseFallback.GetAliasesByDomainAsync(domainId, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            throw new HMailServerException("No connection method available.")
            {
                FailedOperation = "GetAliasesByDomain",
                ResourceType = "Alias",
                ResourceId = domainId,
                Remediation = "Initialize with COM API or database connection first."
            };
        }
    }

    /// <summary>
    /// Gets aliases for a specific domain using COM API.
    /// </summary>
    private async Task<List<EmailAlias>> GetAliasesByDomainFromComAsync(string domainId, CancellationToken cancellationToken)
    {
        var aliases = new List<EmailAlias>();

        try
        {
            if (_server == null)
            {
                throw new HMailServerException("COM server not initialized.")
                {
                    FailedOperation = "GetAliasesByDomain",
                    ResourceId = domainId,
                    Remediation = "Reinitialize the client."
                };
            }

            dynamic domainsCollection = _server.Domains;
            
            if (domainsCollection == null)
                return aliases;

            int domainCount = domainsCollection.Count;

            for (int d = 0; d < domainCount; d++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                dynamic domainObj = domainsCollection.Item[d];
                
                if (domainObj == null)
                    continue;

                string? currentDomainId = domainObj.ID?.ToString();
                
                if (!string.Equals(currentDomainId, domainId, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Get aliases for this domain
                dynamic aliasesCollection = domainObj.Aliases;
                
                if (aliasesCollection == null)
                    continue;

                int aliasCount = aliasesCollection.Count;

                for (int a = 0; a < aliasCount; a++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    dynamic aliasObj = aliasesCollection.Item[a];
                    
                    if (aliasObj == null)
                        continue;

                    var alias = new EmailAlias
                    {
                        Id = aliasObj.ID?.ToString(),
                        Source = aliasObj.Source?.ToString() ?? string.Empty,
                        Destination = aliasObj.Destination?.ToString() ?? string.Empty,
                        IsEnabled = aliasObj.Enabled
                    };

                    if (alias.Source != null)
                        alias.Source = EmailValidator.Normalize(alias.Source);
                    if (alias.Destination != null)
                        alias.Destination = EmailValidator.Normalize(alias.Destination);

                    aliases.Add(alias);
                }

                break; // Found the domain, exit loop
            }

            _logger.LogInformation("Retrieved {Count} aliases for domain {DomainId} from hMailServer via COM API", aliases.Count, domainId);
        }
        catch (COMException comEx)
        {
            throw new HMailServerException("COM error while retrieving aliases for domain.", comEx)
            {
                FailedOperation = "GetAliasesByDomain",
                ComErrorCode = comEx.ErrorCode,
                ResourceType = "Alias",
                ResourceId = domainId,
                Remediation = "Check hMailServer COM API accessibility and try again."
            };
        }

        return aliases;
    }

    /// <summary>
    /// Gets all email messages from hMailServer for a specific account.
    /// </summary>
    /// <param name="accountId">The account ID to get messages for.</param>
    /// <returns>List of email message metadata (not full content).</returns>
    public async Task<List<EmailMessage>> GetMessagesAsync(string accountId, CancellationToken cancellationToken = default)
    {
        var messages = new List<EmailMessage>();

        if (!IsComAvailable)
        {
            throw new HMailServerException("Message extraction requires COM API.")
            {
                FailedOperation = "GetMessages",
                ResourceId = accountId,
                Remediation = "COM API is required for message extraction. Database fallback does not support message content."
            };
        }

        try
        {
            if (_server == null)
            {
                throw new HMailServerException("COM server not initialized.")
                {
                    FailedOperation = "GetMessages",
                    ResourceId = accountId,
                    Remediation = "Reinitialize the client."
                };
            }

            // Find the account and get its messages
            dynamic domainsCollection = _server.Domains;
            
            if (domainsCollection == null)
                return messages;

            int domainCount = domainsCollection.Count;

            for (int d = 0; d < domainCount; d++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                dynamic domainObj = domainsCollection.Item[d];
                
                if (domainObj == null)
                    continue;

                dynamic accountsCollection = domainObj.Accounts;
                
                if (accountsCollection == null)
                    continue;

                int accountCount = accountsCollection.Count;

                for (int a = 0; a < accountCount; a++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    dynamic accountObj = accountsCollection.Item[a];
                    
                    if (accountObj == null)
                        continue;

                    string? currentAccountId = accountObj.ID?.ToString();
                    
                    if (!string.Equals(currentAccountId, accountId, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Get messages for this account
                    dynamic messagesCollection = accountObj.Messages;
                    
                    if (messagesCollection == null)
                        continue;

                    int messageCount = messagesCollection.Count;

                    for (int m = 0; m < messageCount; m++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        dynamic messageObj = messagesCollection.Item[m];
                        
                        if (messageObj == null)
                            continue;

                        var message = new EmailMessage
                        {
                            Id = messageObj.ID?.ToString(),
                            MessageId = messageObj.MessageID?.ToString() ?? string.Empty,
                            Subject = messageObj.Subject?.ToString() ?? string.Empty,
                            From = new EmailAddress(messageObj.From?.ToString() ?? string.Empty),
                            To = messageObj.To?.ToString() ?? string.Empty,
                            Cc = messageObj.CC?.ToString(),
                            Bcc = messageObj.BCC?.ToString(),
                            Date = GetDateFromComObject(messageObj.Date),
                            Size = messageObj.Size,
                            HasAttachment = messageObj.HasAttachment,
                            IsRead = messageObj.Read,
                            Flags = messageObj.Flags
                        };

                        // Normalize email addresses
                        message.From.Address = EmailValidator.Normalize(message.From.Address);
                        
                        messages.Add(message);
                    }

                    return messages; // Found the account, return
                }
            }

            _logger.LogInformation("Retrieved {Count} messages for account {AccountId} from hMailServer via COM API", messages.Count, accountId);
        }
        catch (COMException comEx)
        {
            throw new HMailServerException("COM error while retrieving messages.", comEx)
            {
                FailedOperation = "GetMessages",
                ComErrorCode = comEx.ErrorCode,
                ResourceType = "Message",
                ResourceId = accountId,
                Remediation = "Check hMailServer COM API accessibility and try again."
            };
        }

        return messages;
    }

    /// <summary>
    /// Converts hMailServer COM date format to DateTimeOffset.
    /// </summary>
    private DateTimeOffset? GetDateFromComObject(dynamic dateObj)
    {
        try
        {
            if (dateObj == null)
                return null;

            // hMailServer COM returns date as string in format like "2024-01-15 10:30:00"
            var dateString = dateObj.ToString();
            if (DateTimeOffset.TryParse(dateString, out DateTimeOffset result))
            {
                return result;
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Converts hMailServer size format (bytes as int) to long.
    /// hMailServer may return size in various formats.
    /// </summary>
    private long? ConvertMaxSizeToBytes(dynamic sizeObj)
    {
        try
        {
            if (sizeObj == null)
                return 0;

            // Try to get as long first
            if (sizeObj is long l)
                return l;

            if (sizeObj is int i)
                return i;

            // Try to parse as string
            var sizeString = sizeObj.ToString();
            if (long.TryParse(sizeString, out long result))
            {
                return result;
            }

            return 0;
        }
        catch (Exception ex)
        {
            string sizeValue = sizeObj?.ToString() ?? "null";
            _logger.LogWarning(ex, "Failed to convert size to bytes: {SizeValue}", sizeValue);
            return 0;
        }
    }

    /// <summary>
    /// Disposes the COM objects and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                if (_server != null)
                {
                    Marshal.ReleaseComObject(_server);
                    _server = null;
                }

                if (_application != null)
                {
                    Marshal.ReleaseComObject(_application);
                    _application = null;
                }

                _disposed = true;
                _logger.LogDebug("Disposed HMailServerClient");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while disposing HMailServerClient");
            }
        }
    }

    /// <summary>
    /// Finalizer to ensure COM objects are released.
    /// </summary>
    ~HMailServerClient()
    {
        if (!_disposed)
        {
            try
            {
                if (_server != null)
                {
                    Marshal.FinalReleaseComObject(_server);
                }
                if (_application != null)
                {
                    Marshal.FinalReleaseComObject(_application);
                }
            }
            catch
            {
                // Finalizer should not throw exceptions
            }
        }
    }
}
