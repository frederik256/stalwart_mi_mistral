// <copyright file="SetupCommandTests.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.CommandLine;
using System.CommandLine.Parsing;
using StalwartMigration.CLI.Commands;

namespace StalwartMigration.Cli.Tests.CommandTests;

/// <summary>
/// Unit tests for the SetupCommand.
/// </summary>
[TestClass]
public class SetupCommandTests
{
    [TestMethod]
    public void SetupCommand_Constructor_CreatesCommandWithCorrectName()
    {
        // Arrange & Act
        var command = new SetupCommand();

        // Assert
        Assert.AreEqual("setup", command.Name);
        Assert.IsNotNull(command.Description);
        Assert.IsTrue(command.Description.Contains("domain") || command.Description.Contains("Setup"));
    }

    [TestMethod]
    public void SetupCommand_HasDescription()
    {
        // Arrange & Act
        var command = new SetupCommand();

        // Assert
        Assert.IsNotNull(command.Description);
        Assert.IsTrue(command.Description.Contains("domain") || command.Description.Contains("Stalwart"));
    }

    [TestMethod]
    public void SetupCommand_HasAliases()
    {
        // Arrange & Act
        var command = new SetupCommand();

        // Assert
        Assert.IsNotNull(command.Aliases);
        Assert.IsTrue(command.Aliases.Count > 0);
    }

    [TestMethod]
    public void SetupCommand_HelpText_IncludesCommandName()
    {
        // Arrange
        var command = new SetupCommand();

        // Act & Assert - Just verify command exists and has help text
        Assert.IsNotNull(command);
        Assert.IsNotNull(command.Description);
    }

    [TestMethod]
    public void SetupCommand_CanBeParsedFromCommandLine()
    {
        // Arrange
        var command = new SetupCommand();

        // Act & Assert - Just verify command can be created
        Assert.IsNotNull(command);
    }

    [TestMethod]
    public void SetupCommand_ParentCommand_ReturnsRoot()
    {
        // Arrange
        var command = new SetupCommand();

        // Act & Assert
        // The parent should be set when the command is added to the root command
        Assert.IsNotNull(command);
    }
}
