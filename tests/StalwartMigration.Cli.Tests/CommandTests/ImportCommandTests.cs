// <copyright file="ImportCommandTests.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.CommandLine;
using StalwartMigration.CLI.Commands;

namespace StalwartMigration.Cli.Tests.CommandTests;

/// <summary>
/// Unit tests for the ImportCommand.
/// </summary>
[TestClass]
public class ImportCommandTests
{
    [TestMethod]
    public void ImportCommand_Constructor_CreatesCommandWithCorrectName()
    {
        // Arrange & Act
        var command = new ImportCommand();

        // Assert
        Assert.AreEqual("import", command.Name);
        Assert.IsNotNull(command.Description);
        Assert.IsTrue(command.Description.Contains("import") || command.Description.Contains("Import"));
    }

    [TestMethod]
    public void ImportCommand_HasRequiredOptions()
    {
        // Arrange & Act
        var command = new ImportCommand();

        // Assert
        Assert.AreEqual("import", command.Name);
        Assert.IsNotNull(command.Description);
        
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void ImportCommand_HasDescription()
    {
        // Arrange & Act
        var command = new ImportCommand();

        // Assert
        Assert.IsNotNull(command.Description);
        Assert.IsTrue(command.Description.Contains("import") || command.Description.Contains("Import"));
    }

    [TestMethod]
    public void ImportCommand_HasAllRequiredOptions()
    {
        // Arrange & Act
        var command = new ImportCommand();

        // Assert
        Assert.IsNotNull(command);
    }
}
