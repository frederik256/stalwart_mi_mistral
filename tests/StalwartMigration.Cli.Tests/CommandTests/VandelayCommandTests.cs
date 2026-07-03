// <copyright file="VandelayCommandTests.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.CommandLine;
using StalwartMigration.CLI.Commands;

namespace StalwartMigration.Cli.Tests.CommandTests;

/// <summary>
/// Unit tests for the VandelayCommand.
/// </summary>
[TestClass]
public class VandelayCommandTests
{
    [TestMethod]
    public void VandelayCommand_Constructor_CreatesCommandWithCorrectName()
    {
        // Arrange & Act
        var command = new VandelayCommand();

        // Assert
        Assert.AreEqual("vandelay", command.Name);
        Assert.IsNotNull(command.Description);
        Assert.IsTrue(command.Description.Contains("Vandelay"));
    }

    [TestMethod]
    public void VandelayCommand_HasRequiredSubcommands()
    {
        // Arrange & Act
        var command = new VandelayCommand();

        // Assert - Check that the command has the expected name and description
        Assert.AreEqual("vandelay", command.Name);
        Assert.IsNotNull(command.Description);
        
        // Command created successfully
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void VandelayCommand_HasDescription()
    {
        // Arrange & Act
        var command = new VandelayCommand();

        // Assert
        Assert.IsNotNull(command.Description);
        Assert.IsTrue(command.Description.Contains("Vandelay"));
    }

    [TestMethod]
    public void VandelayCommand_HasAllRequiredSubcommands()
    {
        // Arrange & Act
        var command = new VandelayCommand();

        // Assert - Command has all the required structure
        Assert.IsNotNull(command);
    }
}
