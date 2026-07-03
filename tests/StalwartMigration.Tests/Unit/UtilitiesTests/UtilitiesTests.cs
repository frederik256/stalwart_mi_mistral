// <copyright file="UtilitiesTests.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections;
using StalwartMigration.Utilities.Extensions;
using StalwartMigration.Utilities.Helpers;
using StalwartMigration.Utilities.Logging;
using StalwartMigration.Core.Exceptions;

namespace StalwartMigration.Tests.Unit.UtilitiesTests;

/// <summary>
/// Unit tests for StringExtensions
/// </summary>
[TestClass]
public class StringExtensionsTests
{
    [TestMethod]
    public void IsNullOrWhiteSpace_WithNull_ReturnsTrue()
    {
        string? value = null;
        Assert.IsTrue(value.IsNullOrWhiteSpace());
    }

    [TestMethod]
    public void IsNullOrWhiteSpace_WithEmptyString_ReturnsTrue()
    {
        Assert.IsTrue(string.Empty.IsNullOrWhiteSpace());
    }

    [TestMethod]
    public void IsNullOrWhiteSpace_WithWhitespace_ReturnsTrue()
    {
        Assert.IsTrue("   ".IsNullOrWhiteSpace());
    }

    [TestMethod]
    public void IsNullOrWhiteSpace_WithValidString_ReturnsFalse()
    {
        Assert.IsFalse("test".IsNullOrWhiteSpace());
    }

    [TestMethod]
    public void IsNullOrEmpty_WithNull_ReturnsTrue()
    {
        string? value = null;
        Assert.IsTrue(value.IsNullOrEmpty());
    }

    [TestMethod]
    public void IsNullOrEmpty_WithEmptyString_ReturnsTrue()
    {
        Assert.IsTrue(string.Empty.IsNullOrEmpty());
    }

    [TestMethod]
    public void IsNullOrEmpty_WithWhitespace_ReturnsFalse()
    {
        Assert.IsFalse("   ".IsNullOrEmpty());
    }

    [TestMethod]
    public void Truncate_WithNull_ReturnsEmptyString()
    {
        string? value = null;
        Assert.AreEqual(string.Empty, value.Truncate(10));
    }

    [TestMethod]
    public void Truncate_WithStringShorterThanMaxLength_ReturnsOriginal()
    {
        Assert.AreEqual("short", "short".Truncate(10));
    }

    [TestMethod]
    public void Truncate_WithStringLongerThanMaxLength_ReturnsTruncated()
    {
        Assert.AreEqual("this is a ", "this is a very long string".Truncate(10));
    }

    [TestMethod]
    public void TruncateWithEllipsis_WithNull_ReturnsEmptyString()
    {
        string? value = null;
        Assert.AreEqual(string.Empty, value.TruncateWithEllipsis(10));
    }

    [TestMethod]
    public void TruncateWithEllipsis_WithStringShorterThanMaxLength_ReturnsOriginal()
    {
        Assert.AreEqual("short", "short".TruncateWithEllipsis(10));
    }

    [TestMethod]
    public void TruncateWithEllipsis_WithStringLongerThanMaxLength_ReturnsTruncatedWithEllipsis()
    {
        // "this is a very long string" is 26 chars
        // With maxLength=20, should truncate to 17 chars + "..." = 20
        // "this is a very lo" is 15 chars, + "..." = 18 chars
        // Actually: 20 - 3 = 17, so "this is a very l" + "..." = 20
        string value = "this is a very long string";
        string result = value.TruncateWithEllipsis(20);
        Assert.AreEqual(20, result.Length);
        Assert.IsTrue(result.EndsWith("..."));
    }

    [TestMethod]
    public void ToPascalCase_WithUnderscore_ReturnsPascalCase()
    {
        Assert.AreEqual("TestString", "test_string".ToPascalCase());
    }

    [TestMethod]
    public void ToPascalCase_WithHyphen_ReturnsPascalCase()
    {
        Assert.AreEqual("TestString", "test-string".ToPascalCase());
    }

    [TestMethod]
    public void ToPascalCase_WithSpace_ReturnsPascalCase()
    {
        Assert.AreEqual("TestString", "test string".ToPascalCase());
    }

    [TestMethod]
    public void ToCamelCase_WithPascalCase_ReturnsCamelCase()
    {
        // ToPascalCase("test_string") = "TestString"
        // ToCamelCase("TestString") = "testString"
        Assert.AreEqual("testString", "test_string".ToCamelCase());
    }

    [TestMethod]
    public void ContainsAny_WithMatchingValue_ReturnsTrue()
    {
        Assert.IsTrue("test value".ContainsAny(new[] { "test", "other", "value" }));
    }

    [TestMethod]
    public void ContainsAny_WithNoMatchingValue_ReturnsFalse()
    {
        Assert.IsFalse("test value".ContainsAny(new[] { "other", "values", "here" }));
    }

    [TestMethod]
    public void StartsWithAny_WithMatchingValue_ReturnsTrue()
    {
        Assert.IsTrue("test value".StartsWithAny(new[] { "test", "other", "value" }));
    }

    [TestMethod]
    public void EndsWithAny_WithMatchingValue_ReturnsTrue()
    {
        Assert.IsTrue("test value".EndsWithAny(new[] { "value", "other", "test" }));
    }
}

/// <summary>
/// Unit tests for CollectionExtensions
/// </summary>
[TestClass]
public class CollectionExtensionsTests
{
    [TestMethod]
    public void IsNullOrEmpty_WithNullCollection_ReturnsTrue()
    {
        List<int>? collection = null;
        Assert.IsTrue(collection.IsNullOrEmpty());
    }

    [TestMethod]
    public void IsNullOrEmpty_WithEmptyCollection_ReturnsTrue()
    {
        Assert.IsTrue(new List<int>().IsNullOrEmpty());
    }

    [TestMethod]
    public void IsNullOrEmpty_WithNonEmptyCollection_ReturnsFalse()
    {
        Assert.IsFalse(new List<int> { 1, 2, 3 }.IsNullOrEmpty());
    }

    [TestMethod]
    public void IsNullOrEmpty_WithNullEnumerable_ReturnsTrue()
    {
        IEnumerable<int>? enumerable = null;
        Assert.IsTrue(enumerable.IsNullOrEmpty());
    }

    [TestMethod]
    public void IsNullOrEmpty_WithEmptyEnumerable_ReturnsTrue()
    {
        Assert.IsTrue(Array.Empty<int>().IsNullOrEmpty());
    }

    [TestMethod]
    public void AddRange_WithValidItems_AddsAllItems()
    {
        var collection = new List<int> { 1, 2 };
        collection.AddRange(new[] { 3, 4, 5 });
        Assert.AreEqual(5, collection.Count);
    }

    [TestMethod]
    public void AddRange_WithNullItems_DoesNotAdd()
    {
        var collection = new HashSet<int> { 1, 2 };
        List<int>? items = null;
        ((ICollection<int>)collection).AddRange(items);
        Assert.AreEqual(2, collection.Count);
    }

    [TestMethod]
    public void Batch_WithExactMultiple_ReturnsCorrectBatches()
    {
        var source = new[] { 1, 2, 3, 4, 5, 6 };
        var result = source.Batch(3).ToList();
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(3, result[0].Count());
        Assert.AreEqual(3, result[1].Count());
    }

    [TestMethod]
    public void Batch_WithNonExactMultiple_ReturnsCorrectBatches()
    {
        var source = new[] { 1, 2, 3, 4, 5, 6, 7 };
        var result = source.Batch(3).ToList();
        Assert.AreEqual(3, result.Count);
        Assert.AreEqual(3, result[0].Count());
        Assert.AreEqual(3, result[1].Count());
        Assert.AreEqual(1, result[2].Count());
    }

    [TestMethod]
    public void ForEach_WithValidSourceAndAction_IteratesAllItems()
    {
        var source = new[] { 1, 2, 3 };
        var result = new List<int>();
        source.ForEach(x => result.Add(x * 2));
        Assert.AreEqual(3, result.Count);
        CollectionAssert.AreEqual(new[] { 2, 4, 6 }, result);
    }

    [TestMethod]
    public void ToHashSet_WithDuplicateItems_ReturnsUniqueItems()
    {
        var source = new[] { 1, 2, 2, 3, 3, 3 };
        var result = Enumerable.ToHashSet(source);
        Assert.AreEqual(3, result.Count);
        Assert.IsTrue(result.Contains(1));
        Assert.IsTrue(result.Contains(2));
        Assert.IsTrue(result.Contains(3));
    }

    [TestMethod]
    public void FirstOrNull_WithEmptyCollection_ReturnsNull()
    {
        var source = Array.Empty<string>();
        Assert.IsNull(source.FirstOrNull());
    }

    [TestMethod]
    public void FirstOrNull_WithNonEmptyCollection_ReturnsFirst()
    {
        Assert.AreEqual("first", new[] { "first", "second" }.FirstOrNull());
    }

    [TestMethod]
    public void DistinctBy_WithDuplicateKeys_ReturnsDistinctByKey()
    {
        var source = new[]
        {
            new { Id = 1, Name = "Alice" },
            new { Id = 2, Name = "Bob" },
            new { Id = 1, Name = "Duplicate" },
            new { Id = 3, Name = "Charlie" }
        };
        var result = Enumerable.DistinctBy(source, x => x.Id).ToList();
        Assert.AreEqual(3, result.Count);
    }
}

/// <summary>
/// Unit tests for FileSystemExtensions
/// </summary>
[TestClass]
public class FileSystemExtensionsTests
{
    [TestMethod]
    public void CombinePath_WithNullPath_ReturnsCombinedParts()
    {
        string? path = null;
        var result = path.CombinePath("part1", "part2");
        Assert.AreEqual(Path.Combine("part1", "part2"), result);
    }

    [TestMethod]
    public void CombinePath_WithPathAndParts_ReturnsCombined()
    {
        string path = "/base/path";
        var result = path.CombinePath("subdir", "file.txt");
        Assert.AreEqual(Path.Combine("/base/path", "subdir", "file.txt"), result);
    }

    [TestMethod]
    public void GetDirectoryName_WithFilePath_ReturnsDirectory()
    {
        Assert.AreEqual(@"/path/to", @"/path/to/file.txt".GetDirectoryName());
    }

    [TestMethod]
    public void GetFileName_WithFilePath_ReturnsFileName()
    {
        Assert.AreEqual("file.txt", @"/path/to/file.txt".GetFileName());
    }

    [TestMethod]
    public void GetFileNameWithoutExtension_WithFilePath_ReturnsFileNameWithoutExtension()
    {
        Assert.AreEqual("file", @"/path/to/file.txt".GetFileNameWithoutExtension());
    }

    [TestMethod]
    public void GetExtension_WithFilePath_ReturnsExtension()
    {
        Assert.AreEqual(".txt", @"/path/to/file.txt".GetExtension());
    }

    [TestMethod]
    public void IsDirectoryPath_WithTrailingSeparator_ReturnsTrue()
    {
        Assert.IsTrue(@"/path/to/dir/".IsDirectoryPath());
    }

    [TestMethod]
    public void IsDirectoryPath_WithFilePath_ReturnsFalse()
    {
        Assert.IsFalse(@"/path/to/file.txt".IsDirectoryPath());
    }

    [TestMethod]
    public void EnsureTrailingSeparator_WithDirectoryPath_ReturnsPathWithSeparator()
    {
        string path = @"/path/to/dir";
        string result = path.EnsureTrailingSeparator();
        Assert.IsTrue(result.EndsWith(Path.DirectorySeparatorChar.ToString()));
    }

    [TestMethod]
    public void RemoveTrailingSeparator_WithTrailingSeparator_ReturnsPathWithoutSeparator()
    {
        string path = @"/path/to/dir/";
        string result = path.RemoveTrailingSeparator();
        Assert.AreEqual(@"/path/to/dir", result);
    }

    [TestMethod]
    public void IsRelativePath_WithRelativePath_ReturnsTrue()
    {
        Assert.IsTrue(@"relative/path/file.txt".IsRelativePath());
    }

    [TestMethod]
    public void IsRelativePath_WithAbsolutePath_ReturnsFalse()
    {
        Assert.IsFalse(@"/absolute/path/file.txt".IsRelativePath());
    }
}

/// <summary>
/// Unit tests for DomainValidator
/// </summary>
[TestClass]
public class DomainValidatorTests
{
    [TestMethod]
    public void IsValid_WithNull_ReturnsFalse()
    {
        Assert.IsFalse(DomainValidator.IsValid(null));
    }

    [TestMethod]
    public void IsValid_WithEmptyString_ReturnsFalse()
    {
        Assert.IsFalse(DomainValidator.IsValid(string.Empty));
    }

    [TestMethod]
    public void IsValid_WithTooShortDomain_ReturnsFalse()
    {
        Assert.IsFalse(DomainValidator.IsValid("ab"));
    }

    [TestMethod]
    public void IsValid_WithValidDomain_ReturnsTrue()
    {
        Assert.IsTrue(DomainValidator.IsValid("example.com"));
    }

    [TestMethod]
    public void IsValid_WithSubdomain_ReturnsTrue()
    {
        Assert.IsTrue(DomainValidator.IsValid("sub.example.com"));
    }

    [TestMethod]
    public void IsValid_WithHyphenInLabel_ReturnsTrue()
    {
        Assert.IsTrue(DomainValidator.IsValid("sub-domain.example.com"));
    }

    [TestMethod]
    public void IsValid_WithLeadingHyphen_ReturnsFalse()
    {
        Assert.IsFalse(DomainValidator.IsValid("-example.com"));
    }

    [TestMethod]
    public void IsValid_WithTrailingHyphen_ReturnsFalse()
    {
        Assert.IsFalse(DomainValidator.IsValid("example-.com"));
    }

    [TestMethod]
    public void IsValid_WithLeadingDot_ReturnsFalse()
    {
        Assert.IsFalse(DomainValidator.IsValid(".example.com"));
    }

    [TestMethod]
    public void IsValid_WithTrailingDot_ReturnsFalse()
    {
        Assert.IsFalse(DomainValidator.IsValid("example.com."));
    }

    [TestMethod]
    public void IsValid_WithDoubleDot_ReturnsFalse()
    {
        Assert.IsFalse(DomainValidator.IsValid("example..com"));
    }

    [TestMethod]
    public void IsValid_WithUnderscore_ReturnsFalse()
    {
        Assert.IsFalse(DomainValidator.IsValid("example_test.com"));
    }

    [TestMethod]
    public void Normalize_WithDomain_TrimsAndLowercases()
    {
        Assert.AreEqual("example.com", DomainValidator.Normalize("  EXAMPLE.COM  "));
    }

    [TestMethod]
    public void ExtractTld_WithValidDomain_ReturnsTld()
    {
        Assert.AreEqual("com", DomainValidator.ExtractTld("example.com"));
    }

    [TestMethod]
    public void ExtractSubdomain_WithValidDomain_ReturnsSubdomain()
    {
        Assert.AreEqual("sub.example", DomainValidator.ExtractSubdomain("sub.example.com"));
    }

    [TestMethod]
    public void IsSubdomainOf_WithValidSubdomain_ReturnsTrue()
    {
        Assert.IsTrue(DomainValidator.IsSubdomainOf("sub.example.com", "example.com"));
    }

    [TestMethod]
    public void IsSubdomainOf_WithExactMatch_ReturnsFalse()
    {
        Assert.IsFalse(DomainValidator.IsSubdomainOf("example.com", "example.com"));
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Validate_WithInvalidDomain_ThrowsArgumentException()
    {
        DomainValidator.Validate("invalid_domain", "testParam");
    }
}

/// <summary>
/// Unit tests for EmailValidator
/// </summary>
[TestClass]
public class EmailValidatorTests
{
    [TestMethod]
    public void IsValid_WithNull_ReturnsFalse()
    {
        Assert.IsFalse(EmailValidator.IsValid(null));
    }

    [TestMethod]
    public void IsValid_WithEmptyString_ReturnsFalse()
    {
        Assert.IsFalse(EmailValidator.IsValid(string.Empty));
    }

    [TestMethod]
    public void IsValid_WithSimpleEmail_ReturnsTrue()
    {
        Assert.IsTrue(EmailValidator.IsValid("test@example.com"));
    }

    [TestMethod]
    public void IsValid_WithEmailWithDots_ReturnsTrue()
    {
        Assert.IsTrue(EmailValidator.IsValid("first.last@example.com"));
    }

    [TestMethod]
    public void IsValid_WithEmailWithPlus_ReturnsTrue()
    {
        Assert.IsTrue(EmailValidator.IsValid("test+tag@example.com"));
    }

    [TestMethod]
    public void IsValid_WithEmailWithHyphen_ReturnsTrue()
    {
        Assert.IsTrue(EmailValidator.IsValid("test-email@example-domain.com"));
    }

    [TestMethod]
    public void IsValid_WithNoAtSymbol_ReturnsFalse()
    {
        Assert.IsFalse(EmailValidator.IsValid("test.example.com"));
    }

    [TestMethod]
    public void IsValid_WithMultipleAtSymbols_ReturnsFalse()
    {
        Assert.IsFalse(EmailValidator.IsValid("test@example@example.com"));
    }

    [TestMethod]
    public void IsValid_WithAtAtStart_ReturnsFalse()
    {
        Assert.IsFalse(EmailValidator.IsValid("@example.com"));
    }

    [TestMethod]
    public void IsValid_WithAtAtEnd_ReturnsFalse()
    {
        Assert.IsFalse(EmailValidator.IsValid("test@"));
    }

    [TestMethod]
    public void ExtractDomain_WithValidEmail_ReturnsDomain()
    {
        Assert.AreEqual("example.com", EmailValidator.ExtractDomain("test@example.com"));
    }

    [TestMethod]
    public void ExtractLocalPart_WithValidEmail_ReturnsLocalPart()
    {
        Assert.AreEqual("test", EmailValidator.ExtractLocalPart("test@example.com"));
    }

    [TestMethod]
    public void Normalize_WithEmail_TrimsAndLowercases()
    {
        Assert.AreEqual("test@example.com", EmailValidator.Normalize("  TEST@EXAMPLE.COM  "));
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Validate_WithInvalidEmail_ThrowsArgumentException()
    {
        EmailValidator.Validate("invalid_email", "testParam");
    }
}

/// <summary>
/// Unit tests for PathSanitizer
/// </summary>
[TestClass]
public class PathSanitizerTests
{
    [TestMethod]
    public void SanitizeFileName_WithNull_ReturnsEmptyString()
    {
        Assert.AreEqual(string.Empty, PathSanitizer.SanitizeFileName(null));
    }

    [TestMethod]
    public void SanitizeFileName_WithEmptyString_ReturnsEmptyString()
    {
        Assert.AreEqual(string.Empty, PathSanitizer.SanitizeFileName(string.Empty));
    }

    [TestMethod]
    public void SanitizeFileName_WithValidFileName_ReturnsFileName()
    {
        Assert.AreEqual("valid-file.txt", PathSanitizer.SanitizeFileName("valid-file.txt"));
    }

    [TestMethod]
    public void SanitizeFileName_WithInvalidCharacters_ReplacesCharacters()
    {
        // Use characters that are always invalid: null character
        string result = PathSanitizer.SanitizeFileName("invalid file name.txt");
        Assert.IsFalse(result.Contains(" "));
    }

    [TestMethod]
    public void SanitizePath_WithNull_ReturnsEmptyString()
    {
        Assert.AreEqual(string.Empty, PathSanitizer.SanitizePath(null));
    }

    [TestMethod]
    public void SanitizePath_WithEmptyString_ReturnsEmptyString()
    {
        Assert.AreEqual(string.Empty, PathSanitizer.SanitizePath(string.Empty));
    }

    [TestMethod]
    public void SanitizePath_WithValidPath_ReturnsPath()
    {
        Assert.AreEqual("valid/path/file.txt", PathSanitizer.SanitizePath("valid/path/file.txt"));
    }

    [TestMethod]
    public void IsSafePath_WithNull_ReturnsFalse()
    {
        Assert.IsFalse(PathSanitizer.IsSafePath(null));
    }

    [TestMethod]
    public void IsSafePath_WithDirectoryTraversal_ReturnsFalse()
    {
        Assert.IsFalse(PathSanitizer.IsSafePath("path/../file"));
    }

    [TestMethod]
    public void IsSafePath_WithAbsoluteUnixPath_ReturnsFalse()
    {
        Assert.IsFalse(PathSanitizer.IsSafePath("/absolute/path"));
    }

    [TestMethod]
    public void IsSafePath_WithRelativePath_ReturnsTrue()
    {
        Assert.IsTrue(PathSanitizer.IsSafePath("relative/path/file.txt"));
    }

    [TestMethod]
    public void CreateSafeFileName_WithNull_ReturnsUnnamed()
    {
        Assert.AreEqual("unnamed", PathSanitizer.CreateSafeFileName(null));
    }

    [TestMethod]
    public void CreateSafeFileName_WithValidName_ReturnsName()
    {
        Assert.AreEqual("valid-file.txt", PathSanitizer.CreateSafeFileName("valid-file.txt"));
    }
}

/// <summary>
/// Unit tests for SensitiveDataFilter
/// </summary>
[TestClass]
public class SensitiveDataFilterTests
{
    [TestMethod]
    public void FilterSensitiveData_WithNull_ReturnsNull()
    {
        Assert.IsNull(SensitiveDataFilter.FilterSensitiveData(null));
    }

    [TestMethod]
    public void FilterSensitiveData_WithEmptyString_ReturnsEmptyString()
    {
        Assert.AreEqual(string.Empty, SensitiveDataFilter.FilterSensitiveData(string.Empty));
    }

    [TestMethod]
    public void FilterSensitiveData_WithNoSensitiveData_ReturnsOriginal()
    {
        string message = "This is a normal log message";
        Assert.AreEqual(message, SensitiveDataFilter.FilterSensitiveData(message));
    }

    [TestMethod]
    public void FilterSensitiveData_WithPassword_RedactsPassword()
    {
        string result = SensitiveDataFilter.FilterSensitiveData("password=secret123");
        Assert.IsFalse(result.Contains("secret123"));
        Assert.IsTrue(result.Contains("[REDACTED]"));
    }

    [TestMethod]
    public void FilterSensitiveData_WithToken_RedactsToken()
    {
        string result = SensitiveDataFilter.FilterSensitiveData("token=abc123xyz");
        Assert.IsFalse(result.Contains("abc123xyz"));
        Assert.IsTrue(result.Contains("[REDACTED]"));
    }

    [TestMethod]
    public void FilterSensitiveData_WithMultipleSensitiveItems_RedactsAll()
    {
        string result = SensitiveDataFilter.FilterSensitiveData("password=secret and token=abc123");
        Assert.IsFalse(result.Contains("secret"));
        Assert.IsFalse(result.Contains("abc123"));
        Assert.IsTrue(result.Contains("[REDACTED]"));
    }

    [TestMethod]
    public void FilterSensitiveData_PreservesNonSensitiveData()
    {
        string message = "User logged in successfully with username: john.doe";
        Assert.AreEqual(message, SensitiveDataFilter.FilterSensitiveData(message));
    }
}

/// <summary>
/// Unit tests for LoggingConfiguration
/// </summary>
[TestClass]
public class LoggingConfigurationTests
{
    [TestMethod]
    public void CreateLogger_WithValidName_ReturnsLogger()
    {
        ILogger logger = LoggingConfiguration.CreateLogger("TestLogger");
        Assert.IsNotNull(logger);
    }

    [TestMethod]
    public void AddMigrationLogging_ConfiguresServices()
    {
        var services = new ServiceCollection();
        services.AddMigrationLogging();
        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
        Assert.IsNotNull(loggerFactory);
    }
}

/// <summary>
/// Unit tests for custom exceptions
/// </summary>
[TestClass]
public class CustomExceptionsTests
{
    [TestMethod]
    public void MigrationException_WithMessage_CreatesExceptionWithMessage()
    {
        string message = "Test migration error";
        var exception = new MigrationException(message);
        Assert.AreEqual(message, exception.Message);
    }

    [TestMethod]
    public void ConfigurationException_WithMessage_CreatesExceptionWithMessage()
    {
        string message = "Test configuration error";
        var exception = new ConfigurationException(message);
        Assert.AreEqual(message, exception.Message);
    }

    [TestMethod]
    public void ConfigurationException_ForMissingKey_CreatesExceptionWithCorrectMessage()
    {
        string key = "database.connectionString";
        var exception = ConfigurationException.ForMissingKey(key);
        Assert.IsTrue(exception.Message.Contains(key));
        Assert.IsTrue(exception.Message.Contains("missing or empty"));
        Assert.AreEqual("Configuration Validation", exception.Context);
    }

    [TestMethod]
    public void ConfigurationException_ForInvalidValue_CreatesExceptionWithCorrectMessage()
    {
        string key = "database.port";
        string value = "not-a-number";
        string expectedFormat = "numeric value";
        var exception = ConfigurationException.ForInvalidValue(key, value, expectedFormat);
        Assert.IsTrue(exception.Message.Contains(key));
        Assert.IsTrue(exception.Message.Contains(value));
        Assert.IsTrue(exception.Message.Contains(expectedFormat));
    }
}
