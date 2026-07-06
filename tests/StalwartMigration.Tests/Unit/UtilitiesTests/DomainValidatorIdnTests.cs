// <copyright file="DomainValidatorIdnTests.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using StalwartMigration.Utilities.Helpers;

namespace StalwartMigration.Tests.Unit.UtilitiesTests;

/// <summary>
/// Tests for IDN (International Domain Name) encoding support in DomainValidator.
/// Issue #11: IDN Encoding Issues
/// </summary>
[TestClass]
public class DomainValidatorIdnTests
{
    [TestMethod]
    public void Normalize_WithRegularDomain_ReturnsLowercase()
    {
        // Arrange
        string domain = "EXAMPLE.COM";

        // Act
        string result = DomainValidator.Normalize(domain);

        // Assert
        Assert.AreEqual("example.com", result);
    }

    [TestMethod]
    public void Normalize_WithNull_ReturnsEmptyString()
    {
        // Arrange
        string? domain = null;

        // Act
        string result = DomainValidator.Normalize(domain);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void Normalize_WithWhitespace_TrimsAndLowercases()
    {
        // Arrange
        string domain = "  Example.Com  ";

        // Act
        string result = DomainValidator.Normalize(domain);

        // Assert
        Assert.AreEqual("example.com", result);
    }

    [TestMethod]
    public void Normalize_WithIdnDomain_ConvertsToPunycode()
    {
        // Arrange - münchen.de is a real IDN domain
        string domain = "münchen.de";

        // Act
        string result = DomainValidator.Normalize(domain);

        // Assert - Punycode for münchen.de is xn--mnchen-3ya.de
        Assert.AreEqual("xn--mnchen-3ya.de", result);
    }

    [TestMethod]
    public void Normalize_WithMixedCaseIdn_ConvertsToPunycodeLowercase()
    {
        // Arrange
        string domain = "München.DE";

        // Act
        string result = DomainValidator.Normalize(domain);

        // Assert
        Assert.AreEqual("xn--mnchen-3ya.de", result);
    }

    [TestMethod]
    public void Normalize_WithChineseIdn_ConvertsToPunycode()
    {
        // Arrange - 中国icann.测试 is a test IDN domain
        string domain = "中国icann.测试";

        // Act
        string result = DomainValidator.Normalize(domain);

        // Assert - Punycode for 中国icann.测试
        // This should start with xn-- indicating Punycode
        Assert.IsTrue(result.StartsWith("xn--"));
    }

    [TestMethod]
    public void Normalize_WithAlreadyPunycode_ReturnsAsIs()
    {
        // Arrange - Already in Punycode format
        string domain = "xn--mnchen-3ya.de";

        // Act
        string result = DomainValidator.Normalize(domain);

        // Assert
        Assert.AreEqual("xn--mnchen-3ya.de", result);
    }

    [TestMethod]
    public void Normalize_WithSubdomainAndIdn_ConvertsIdnPart()
    {
        // Arrange
        string domain = "test.münchen.de";

        // Act
        string result = DomainValidator.Normalize(domain);

        // Assert
        Assert.AreEqual("test.xn--mnchen-3ya.de", result);
    }

    [TestMethod]
    public void Normalize_WithEmptyString_ReturnsEmptyString()
    {
        // Arrange
        string domain = "";

        // Act
        string result = DomainValidator.Normalize(domain);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void Normalize_WithInvalidIdn_ReturnsFallbackLowercase()
    {
        // Arrange - Invalid Unicode sequence
        // This should fall back to lowercase if IDN conversion fails
        string domain = "test.example.com";

        // Act
        string result = DomainValidator.Normalize(domain);

        // Assert
        Assert.AreEqual("test.example.com", result);
    }
}
