// <copyright file="HMailServerDatabase.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StalwartMigration.Core.Models;
using StalwartMigration.Utilities.Helpers;

namespace StalwartMigration.Infrastructure.HMailServer;

/// <summary>
/// Provides fallback database access to hMailServer when COM API is unavailable.
/// Supports both SQLite (hMailServer 5.6+) and MSSQL (older versions) databases.
/// </summary>
public class HMailServerDatabase : IDisposable
{
    private readonly string _connectionString;
    private readonly ILogger<HMailServerDatabase> _logger;
    private DbConnection? _connection;
    private bool _disposed;

    /// <summary>
    /// Gets the database type (SQLite or MSSQL).
    /// </summary>
    public DatabaseType DatabaseType { get; }

    /// <summary>
    /// Initializes a new instance of the HMailServerDatabase class.
    /// </summary>
    /// <param name="connectionString">The database connection string.</param>
    /// <param name="databaseType">The type of database.</param>
    /// <param name="logger">The logger instance.</param>
    public HMailServerDatabase(string connectionString, DatabaseType databaseType = DatabaseType.SQLite, ILogger<HMailServerDatabase>? logger = null)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        _connectionString = connectionString;
        DatabaseType = databaseType;
        _logger = logger ?? NullLogger<HMailServerDatabase>.Instance;
    }

    /// <summary>
    /// Opens a connection to the database.
    /// </summary>
    private DbConnection OpenConnection()
    {
        if (_connection != null && _connection.State == ConnectionState.Open)
            return _connection;

        _connection?.Dispose();

        try
        {
            _connection = DatabaseType switch
            {
                DatabaseType.SQLite => new SqliteConnection(_connectionString),
                DatabaseType.MSSQL => new SqlConnection(_connectionString),
                _ => throw new HMailServerException("Unsupported database type.")
                    { FailedOperation = "Connection", Remediation = "Use SQLite or MSSQL database type." }
            };

            _connection.Open();
            _logger.LogInformation("Opened {DatabaseType} connection to hMailServer database", DatabaseType);
            return _connection;
        }
        catch (Exception ex)
        {
            throw HMailServerException.ForDatabaseError(
                $"Failed to open {DatabaseType} connection: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Tests the database connection.
    /// </summary>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(cancellationToken);
            _logger.LogDebug("Database connection test successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection test failed");
            return false;
        }
    }

    /// <summary>
    /// Gets the hMailServer version from the database.
    /// </summary>
    public async Task<string?> GetServerVersionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();

            if (DatabaseType == DatabaseType.SQLite)
                command.CommandText = "SELECT hm_version FROM hm_settings WHERE hm_setting_name = 'version';";
            else
                command.CommandText = "SELECT hm_value FROM hm_settings WHERE hm_name = 'version';";

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve hMailServer version from database");
            return null;
        }
    }

    /// <summary>
    /// Gets all domains from hMailServer.
    /// </summary>
    public async Task<List<Domain>> GetDomainsAsync(CancellationToken cancellationToken = default)
    {
        var domains = new List<Domain>();

        try
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();

            if (DatabaseType == DatabaseType.SQLite)
                command.CommandText = @"
                    SELECT 
                        hm_id as id, 
                        hm_name as name, 
                        hm_description as description,
                        hm_enabled as enabled,
                        hm_max_accounts as max_accounts,
                        hm_max_size as quota,
                        hm_dkim_enabled as dkim_enabled,
                        hm_spf_enabled as spf_enabled,
                        hm_dmarc_enabled as dmarc_enabled
                    FROM hm_domains 
                    ORDER BY hm_name;";
            else
                command.CommandText = @"
                    SELECT 
                        id as id, 
                        name as name, 
                        [description] as description,
                        enabled as enabled,
                        max_accounts as max_accounts,
                        max_size as quota,
                        dkim_enabled as dkim_enabled,
                        spf_enabled as spf_enabled,
                        dmarc_enabled as dmarc_enabled
                    FROM domains 
                    ORDER BY name;";

            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var domain = new Domain
                {
                    Id = reader.GetStringOrDefault("id"),
                    Name = reader.GetStringOrDefault("name") ?? string.Empty,
                    Description = reader.GetStringOrDefault("description"),
                    IsEnabled = reader.GetBooleanOrDefault("enabled", true),
                    MaxAccounts = reader.GetIntOrNull("max_accounts"),
                    Quota = reader.GetLongOrNull("quota"),
                    DkimEnabled = reader.GetBooleanOrDefault("dkim_enabled", true),
                    SpfEnabled = reader.GetBooleanOrDefault("spf_enabled", true),
                    DmarcEnabled = reader.GetBooleanOrDefault("dmarc_enabled", true)
                };

                // Normalize domain name
                domain.Name = DomainValidator.Normalize(domain.Name);
                domains.Add(domain);
            }

            _logger.LogInformation("Retrieved {Count} domains from database", domains.Count);
        }
        catch (Exception ex)
        {
            throw HMailServerException.ForDatabaseError(
                $"Failed to retrieve domains: {ex.Message}", ex);
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

        try
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();

            if (DatabaseType == DatabaseType.SQLite)
            {
                command.CommandText = @"
                    SELECT 
                        hm_id as id, 
                        hm_name as name, 
                        hm_description as description,
                        hm_enabled as enabled,
                        hm_max_accounts as max_accounts,
                        hm_max_size as quota
                    FROM hm_domains 
                    WHERE hm_name = @name 
                    LIMIT 1;";
            }
            else
            {
                command.CommandText = @"
                    SELECT 
                        id as id, 
                        name as name, 
                        [description] as description,
                        enabled as enabled,
                        max_accounts as max_accounts,
                        max_size as quota
                    FROM domains 
                    WHERE name = @name;";
            }

            var param = command.CreateParameter();
            param.ParameterName = "@name";
            param.Value = normalizedName;
            command.Parameters.Add(param);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                return new Domain
                {
                    Id = reader.GetStringOrDefault("id"),
                    Name = normalizedName,
                    Description = reader.GetStringOrDefault("description"),
                    IsEnabled = reader.GetBooleanOrDefault("enabled", true),
                    MaxAccounts = reader.GetIntOrNull("max_accounts"),
                    Quota = reader.GetLongOrNull("quota")
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            throw HMailServerException.ForDatabaseError(
                $"Failed to retrieve domain '{domainName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets all accounts from hMailServer, optionally filtered by domain.
    /// </summary>
    /// <param name="domainId">Optional domain ID to filter by.</param>
    public async Task<List<Account>> GetAccountsAsync(string? domainId = null, CancellationToken cancellationToken = default)
    {
        var accounts = new List<Account>();

        try
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();

            string baseQuery;
            if (DatabaseType == DatabaseType.SQLite)
                baseQuery = @"
                    SELECT 
                        hm_id as id,
                        hm_domain_id as domain_id,
                        hm_name as name,
                        hm_address as email,
                        hm_password as password,
                        hm_display_name as display_name,
                        hm_max_size as quota,
                        hm_enabled as enabled,
                        hm_is_admin as is_admin,
                        hm_forwarding_address as forwarding_address,
                        hm_forwarding_enabled as forwarding_enabled,
                        hm_keep_forwarded_copy as keep_forwarded_copy
                    FROM hm_accounts";
            else
                baseQuery = @"
                    SELECT 
                        id as id,
                        domain_id as domain_id,
                        name as name,
                        address as email,
                        [password] as password,
                        display_name as display_name,
                        max_size as quota,
                        enabled as enabled,
                        is_admin as is_admin,
                        forwarding_address as forwarding_address,
                        forwarding_enabled as forwarding_enabled,
                        keep_forwarded_copy as keep_forwarded_copy
                    FROM accounts";

            if (domainId != null)
            {
                if (DatabaseType == DatabaseType.SQLite)
                    baseQuery += " WHERE hm_domain_id = @domainId";
                else
                    baseQuery += " WHERE domain_id = @domainId";
            }

            baseQuery += " ORDER BY email;";
            command.CommandText = baseQuery;

            if (domainId != null)
            {
                var param = command.CreateParameter();
                param.ParameterName = "@domainId";
                param.Value = domainId;
                command.Parameters.Add(param);
            }

            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var account = new Account
                {
                    Id = reader.GetStringOrDefault("id"),
                    DomainId = reader.GetStringOrDefault("domain_id"),
                    Name = reader.GetStringOrDefault("name") ?? string.Empty,
                    Email = reader.GetStringOrDefault("email") ?? string.Empty,
                    Password = reader.GetStringOrDefault("password"),
                    DisplayName = reader.GetStringOrDefault("display_name"),
                    Quota = reader.GetLongOrNull("quota"),
                    IsEnabled = reader.GetBooleanOrDefault("enabled", true),
                    IsAdmin = reader.GetBooleanOrDefault("is_admin", false),
                    ForwardingEnabled = reader.GetBooleanOrDefault("forwarding_enabled", false),
                    KeepForwardedCopy = reader.GetBooleanOrDefault("keep_forwarded_copy", true)
                };

                // Handle forwarding address
                var forwardingAddress = reader.GetStringOrDefault("forwarding_address");
                if (!string.IsNullOrWhiteSpace(forwardingAddress))
                {
                    account.ForwardingAddresses = new List<string> { forwardingAddress };
                }

                // Normalize email
                if (account.Email != null)
                {
                    account.Email = EmailValidator.Normalize(account.Email);
                }

                accounts.Add(account);
            }

            _logger.LogInformation("Retrieved {Count} accounts from database", accounts.Count);
        }
        catch (Exception ex)
        {
            throw HMailServerException.ForDatabaseError(
                $"Failed to retrieve accounts: {ex.Message}", ex);
        }

        return accounts;
    }

    /// <summary>
    /// Gets all aliases from hMailServer.
    /// </summary>
    public async Task<List<EmailAlias>> GetAliasesAsync(CancellationToken cancellationToken = default)
    {
        var aliases = new List<EmailAlias>();

        try
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();

            if (DatabaseType == DatabaseType.SQLite)
                command.CommandText = @"
                    SELECT 
                        hm_id as id,
                        hm_source as source,
                        hm_destination as destination,
                        hm_enabled as enabled
                    FROM hm_aliases 
                    ORDER BY hm_source;";
            else
                command.CommandText = @"
                    SELECT 
                        id as id,
                        source as source,
                        destination as destination,
                        enabled as enabled
                    FROM aliases 
                    ORDER BY source;";

            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var alias = new EmailAlias
                {
                    Id = reader.GetStringOrDefault("id"),
                    Source = reader.GetStringOrDefault("source") ?? string.Empty,
                    Destination = reader.GetStringOrDefault("destination") ?? string.Empty,
                    IsEnabled = reader.GetBooleanOrDefault("enabled", true)
                };

                // Normalize email addresses
                if (alias.Source != null)
                    alias.Source = EmailValidator.Normalize(alias.Source);
                if (alias.Destination != null)
                    alias.Destination = EmailValidator.Normalize(alias.Destination);

                aliases.Add(alias);
            }

            _logger.LogInformation("Retrieved {Count} aliases from database", aliases.Count);
        }
        catch (Exception ex)
        {
            throw HMailServerException.ForDatabaseError(
                $"Failed to retrieve aliases: {ex.Message}", ex);
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

        var aliases = new List<EmailAlias>();

        try
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();

            // For SQLite
            if (DatabaseType == DatabaseType.SQLite)
            {
                command.CommandText = @"
                    SELECT 
                        a.hm_id as id,
                        a.hm_source as source,
                        a.hm_destination as destination,
                        a.hm_enabled as enabled
                    FROM hm_aliases a
                    JOIN hm_accounts ac ON a.hm_account_id = ac.hm_id
                    WHERE ac.hm_domain_id = @domainId
                    ORDER BY a.hm_source;";
            }
            else
            {
                // For MSSQL, we need to join through accounts
                command.CommandText = @"
                    SELECT 
                        a.id as id,
                        a.source as source,
                        a.destination as destination,
                        a.enabled as enabled
                    FROM aliases a
                    JOIN accounts ac ON a.account_id = ac.id
                    WHERE ac.domain_id = @domainId
                    ORDER BY a.source;";
            }

            var param = command.CreateParameter();
            param.ParameterName = "@domainId";
            param.Value = domainId;
            command.Parameters.Add(param);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var alias = new EmailAlias
                {
                    Id = reader.GetStringOrDefault("id"),
                    Source = reader.GetStringOrDefault("source") ?? string.Empty,
                    Destination = reader.GetStringOrDefault("destination") ?? string.Empty,
                    IsEnabled = reader.GetBooleanOrDefault("enabled", true)
                };

                if (alias.Source != null)
                    alias.Source = EmailValidator.Normalize(alias.Source);
                if (alias.Destination != null)
                    alias.Destination = EmailValidator.Normalize(alias.Destination);

                aliases.Add(alias);
            }

            _logger.LogInformation("Retrieved {Count} aliases for domain {DomainId} from database", aliases.Count, domainId);
        }
        catch (Exception ex)
        {
            throw HMailServerException.ForDatabaseError(
                $"Failed to retrieve aliases for domain '{domainId}': {ex.Message}", ex);
        }

        return aliases;
    }

    /// <summary>
    /// Disposes the database connection.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _connection?.Dispose();
            _connection = null;
            _disposed = true;
            _logger.LogDebug("Disposed HMailServerDatabase connection");
        }
    }
}

/// <summary>
/// Enumeration of supported database types for hMailServer.
/// </summary>
public enum DatabaseType
{
    /// <summary>
    /// SQLite database (hMailServer 5.6+ default).
    /// </summary>
    SQLite,

    /// <summary>
    /// Microsoft SQL Server database (older hMailServer versions).
    /// </summary>
    MSSQL
}

/// <summary>
/// Extension methods for DbDataReader to simplify null handling.
/// </summary>
public static class DbDataReaderExtensions
{
    /// <summary>
    /// Gets a string value from the reader or returns default if the column is null or DBNull.
    /// </summary>
    /// <param name="reader">The data reader.</param>
    /// <param name="columnName">The column name.</param>
    /// <param name="defaultValue">The default value to return if null.</param>
    /// <returns>The value or default.</returns>
    public static string? GetStringOrDefault(this DbDataReader reader, string columnName, string? defaultValue = null)
    {
        var index = reader.GetOrdinal(columnName);
        return reader.IsDBNull(index) ? defaultValue : reader.GetString(index);
    }

    /// <summary>
    /// Gets a boolean value from the reader or returns default if the column is null or DBNull.
    /// </summary>
    /// <param name="reader">The data reader.</param>
    /// <param name="columnName">The column name.</param>
    /// <param name="defaultValue">The default value to return if null.</param>
    /// <returns>The value or default.</returns>
    public static bool GetBooleanOrDefault(this DbDataReader reader, string columnName, bool defaultValue)
    {
        var index = reader.GetOrdinal(columnName);
        return reader.IsDBNull(index) ? defaultValue : reader.GetBoolean(index);
    }

    /// <summary>
    /// Gets an int value from the reader or returns default if the column is null or DBNull.
    /// </summary>
    /// <param name="reader">The data reader.</param>
    /// <param name="columnName">The column name.</param>
    /// <param name="defaultValue">The default value to return if null.</param>
    /// <returns>The value or default.</returns>
    public static int GetIntOrDefault(this DbDataReader reader, string columnName, int defaultValue = 0)
    {
        var index = reader.GetOrdinal(columnName);
        return reader.IsDBNull(index) ? defaultValue : reader.GetInt32(index);
    }

    /// <summary>
    /// Gets a long value from the reader or returns default if the column is null or DBNull.
    /// </summary>
    /// <param name="reader">The data reader.</param>
    /// <param name="columnName">The column name.</param>
    /// <param name="defaultValue">The default value to return if null.</param>
    /// <returns>The value or default.</returns>
    public static long GetLongOrDefault(this DbDataReader reader, string columnName, long defaultValue = 0)
    {
        var index = reader.GetOrdinal(columnName);
        return reader.IsDBNull(index) ? defaultValue : reader.GetInt64(index);
    }

    /// <summary>
    /// Gets an int? value from the reader or returns default if the column is null or DBNull.
    /// </summary>
    /// <param name="reader">The data reader.</param>
    /// <param name="columnName">The column name.</param>
    /// <returns>The value or null.</returns>
    public static int? GetIntOrNull(this DbDataReader reader, string columnName)
    {
        var index = reader.GetOrdinal(columnName);
        return reader.IsDBNull(index) ? null : reader.GetInt32(index);
    }

    /// <summary>
    /// Gets a long? value from the reader or returns default if the column is null or DBNull.
    /// </summary>
    /// <param name="reader">The data reader.</param>
    /// <param name="columnName">The column name.</param>
    /// <returns>The value or null.</returns>
    public static long? GetLongOrNull(this DbDataReader reader, string columnName)
    {
        var index = reader.GetOrdinal(columnName);
        return reader.IsDBNull(index) ? null : reader.GetInt64(index);
    }

    /// <summary>
    /// Gets a bool? value from the reader or returns default if the column is null or DBNull.
    /// </summary>
    /// <param name="reader">The data reader.</param>
    /// <param name="columnName">The column name.</param>
    /// <returns>The value or null.</returns>
    public static bool? GetBooleanOrNull(this DbDataReader reader, string columnName)
    {
        var index = reader.GetOrdinal(columnName);
        return reader.IsDBNull(index) ? null : reader.GetBoolean(index);
    }
}
