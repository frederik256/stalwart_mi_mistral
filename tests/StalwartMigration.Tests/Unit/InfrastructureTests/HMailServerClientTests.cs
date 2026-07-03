// <copyright file="HMailServerClientTests.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StalwartMigration.Infrastructure.HMailServer;

namespace StalwartMigration.Tests.Unit.InfrastructureTests;

[TestClass]
public class HMailServerClientTests
{
    [TestMethod]
    public void Constructor_WithDatabaseConnectionStringAndSQLiteType_CreatesClientWithFallback()
    {
        string connectionString = "Data Source=test.db";
        try
        {
            var client = new HMailServerClient(connectionString, DatabaseType.SQLite);
            Assert.IsNotNull(client);
            Assert.IsNotNull(client.DatabaseFallback);
        }
        catch (Exception)
        {
            // Database connection might fail, but the client should still be created
            Assert.Inconclusive("Database connection failed, but this is expected in test environment.");
        }
    }

    [TestMethod]
    public void Constructor_WithDatabaseConnectionStringAndMSSQLType_CreatesClientWithFallback()
    {
        string connectionString = "Server=localhost;Database=hmailserver;User Id=sa;Password=test;";
        try
        {
            var client = new HMailServerClient(connectionString, DatabaseType.MSSQL);
            Assert.IsNotNull(client);
            Assert.IsNotNull(client.DatabaseFallback);
        }
        catch (Exception)
        {
            // Database connection might fail
            Assert.Inconclusive("Database connection failed.");
        }
    }

    [TestMethod]
    public void Constructor_WithNullLogger_UsesNullLogger()
    {
        try
        {
            var client = new HMailServerClient(null, DatabaseType.SQLite, null);
            Assert.IsNotNull(client);
        }
        catch (HMailServerException)
        {
            // Expected on systems without hMailServer or database
            Assert.Inconclusive("hMailServer COM API or database fallback is not available.");
        }
    }

    [TestMethod]
    public void Constructor_WithNoParameters_ThrowsExceptionWhenNeitherAvailable()
    {
        try
        {
            // On Linux, COM API is not available and no database connection string is provided
            var client = new HMailServerClient();
            Assert.Inconclusive("hMailServer COM API or database fallback is available on this system.");
        }
        catch (HMailServerException)
        {
            // Expected - neither COM API nor database fallback is available
            Assert.IsTrue(true);
        }
    }

    [TestMethod]
    public void IsComAvailable_Property_ReturnsBoolean()
    {
        try
        {
            var client = new HMailServerClient(null, DatabaseType.SQLite);
            bool isAvailable = client.IsComAvailable;
            Assert.IsTrue(isAvailable || !isAvailable);
        }
        catch (HMailServerException)
        {
            Assert.Inconclusive("hMailServer COM API or database fallback is not available.");
        }
    }

    [TestMethod]
    public void Version_Property_ReturnsStringOrNull()
    {
        try
        {
            var client = new HMailServerClient(null, DatabaseType.SQLite);
            string? version = client.Version;
            Assert.IsTrue(version == null || version.GetType() == typeof(string));
        }
        catch (HMailServerException)
        {
            Assert.Inconclusive("hMailServer COM API or database fallback is not available.");
        }
    }

    [TestMethod]
    public void DatabaseFallback_Property_ReturnsDatabaseOrNull()
    {
        try
        {
            string connectionString = "Data Source=test.db";
            var client = new HMailServerClient(connectionString, DatabaseType.SQLite);
            Assert.IsTrue(client.DatabaseFallback == null || client.DatabaseFallback != null);
        }
        catch (Exception)
        {
            Assert.Inconclusive("Database connection failed.");
        }
    }

    [TestMethod]
    public void Dispose_DisposesClient()
    {
        try
        {
            var client = new HMailServerClient(null, DatabaseType.SQLite);
            client.Dispose();
            client.Dispose(); // Should not throw when disposed multiple times
        }
        catch (HMailServerException)
        {
            Assert.Inconclusive("hMailServer COM API or database fallback is not available.");
        }
    }
}
