// <copyright file="StalwartClientTests.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StalwartMigration.Core.Models;
using StalwartMigration.Infrastructure.Stalwart;
using System.Collections.Generic;

namespace StalwartMigration.Tests.Unit.InfrastructureTests;

[TestClass]
public class StalwartClientTests
{
    private const string TestBaseUrl = "http://localhost:8080";

    [TestMethod]
    public void Constructor_WithValidBaseUrl_CreatesClient()
    {
        // Constructor bug fixed - MaxAutomaticRedirections=0 removed
        var client = new StalwartClient(TestBaseUrl, (StalwartClientOptions?)null);
        Assert.IsNotNull(client);
        Assert.AreEqual(TestBaseUrl, client.BaseUrl);
        Assert.IsFalse(client.IsAuthenticated);
        client.Dispose();
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Constructor_WithNullBaseUrl_ThrowsArgumentException()
    {
        new StalwartClient(null!);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Constructor_WithEmptyBaseUrl_ThrowsArgumentException()
    {
        new StalwartClient(string.Empty);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Constructor_WithWhitespaceBaseUrl_ThrowsArgumentException()
    {
        new StalwartClient("   ");
    }

    [TestMethod]
    public void Constructor_WithCredentials_SetsCredentials()
    {
        // Constructor bug fixed
        var credentials = new ApiCredentials("testuser", "testpass");
        var client = new StalwartClient(TestBaseUrl, credentials);
        Assert.IsNotNull(client);
        Assert.AreEqual(TestBaseUrl, client.BaseUrl);
        Assert.IsNotNull(client.Credentials);
        Assert.AreEqual("testuser", client.Credentials.Username);
        client.Dispose();
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Constructor_WithNullCredentials_ThrowsArgumentNullException()
    {
        // Constructor bug fixed
        new StalwartClient(TestBaseUrl, (ApiCredentials)null!);
    }

    [TestMethod]
    public void Constructor_TrimsTrailingSlashFromBaseUrl()
    {
        // Constructor bug fixed
        var client = new StalwartClient("http://localhost:8080/", (StalwartClientOptions?)null);
        Assert.AreEqual("http://localhost:8080", client.BaseUrl);
        client.Dispose();
    }

    [TestMethod]
    public void BaseUrl_WithoutTrailingSlash_StaysUnchanged()
    {
        // Constructor bug fixed
        var client = new StalwartClient("http://localhost:8080", (StalwartClientOptions?)null);
        Assert.AreEqual("http://localhost:8080", client.BaseUrl);
        client.Dispose();
    }

    [TestMethod]
    public void IsAuthenticated_WithNullToken_ReturnsFalse()
    {
        // Constructor bug fixed
        var client = new StalwartClient(TestBaseUrl, (StalwartClientOptions?)null);
        Assert.IsFalse(client.IsAuthenticated);
        client.Dispose();
    }

    [TestMethod]
    public void Dispose_DisposesClient()
    {
        // Constructor bug fixed
        var client = new StalwartClient(TestBaseUrl, (StalwartClientOptions?)null);
        client.Dispose();
        // No exception should be thrown
    }

    [TestMethod]
    public void Credentials_GetAndSet_Works()
    {
        // Constructor bug fixed
        var client = new StalwartClient(TestBaseUrl, (StalwartClientOptions?)null);
        Assert.IsNull(client.Credentials);
        
        var creds = new ApiCredentials("user", "pass");
        client.Credentials = creds;
        Assert.IsNotNull(client.Credentials);
        Assert.AreEqual("user", client.Credentials.Username);
        client.Dispose();
    }
}
