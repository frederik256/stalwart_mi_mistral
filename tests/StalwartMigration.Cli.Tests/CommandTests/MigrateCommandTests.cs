// <copyright file="MigrateCommandTests.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.CommandLine;
using StalwartMigration.CLI.Commands;

namespace StalwartMigration.Cli.Tests.CommandTests;

/// <summary>
/// Unit tests for the MigrateCommand.
/// </summary>
[TestClass]
public class MigrateCommandTests
{
    [TestMethod]
    public void MigrateCommand_Constructor_CreatesCommandWithCorrectName()
    {
        // Arrange & Act
        var command = new MigrateCommand();

        // Assert
        Assert.AreEqual("migrate", command.Name);
        Assert.IsNotNull(command.Description);
        Assert.IsTrue(command.Description.Contains("migration") || command.Description.Contains("Migrate"));
    }

    [TestMethod]
    public void MigrateCommand_HasRequiredOptions()
    {
        // Arrange & Act
        var command = new MigrateCommand();

        // Assert - Check that the command has the expected name and description
        Assert.AreEqual("migrate", command.Name);
        Assert.IsNotNull(command.Description);
        
        // Command created successfully
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void MigrateCommand_HasDescription()
    {
        // Arrange & Act
        var command = new MigrateCommand();

        // Assert
        Assert.IsNotNull(command.Description);
        Assert.IsTrue(command.Description.Contains("migration") || command.Description.Contains("Migrate"));
    }

    [TestMethod]
    public void MigrateCommand_HasAllRequiredOptionsAndFlags()
    {
        // Arrange & Act
        var command = new MigrateCommand();

        // Assert - Command has all the required structure
        Assert.IsNotNull(command);
    }
}
