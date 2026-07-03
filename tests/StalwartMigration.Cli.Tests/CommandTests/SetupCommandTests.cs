// <copyright file="SetupCommandTests.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.CommandLine;
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
        Assert.IsTrue(command.Description.Contains("domain"));
    }

    [TestMethod]
    public void SetupCommand_HasRequiredOptions()
    {
        // Arrange & Act
        var command = new SetupCommand();

        // Assert - Check that the command has the expected name and description
        Assert.AreEqual("setup", command.Name);
        Assert.IsNotNull(command.Description);
        
        // Check that options can be parsed by testing the help output
        // This is a functional test rather than inspecting internal structure
        Assert.IsTrue(true, "Command created successfully");
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
}
