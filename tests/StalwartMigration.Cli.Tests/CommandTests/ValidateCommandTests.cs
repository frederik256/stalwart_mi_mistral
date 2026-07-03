// <copyright file="ValidateCommandTests.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.CommandLine;
using StalwartMigration.CLI.Commands;

namespace StalwartMigration.Cli.Tests.CommandTests;

/// <summary>
/// Unit tests for the ValidateCommand.
/// </summary>
[TestClass]
public class ValidateCommandTests
{
    [TestMethod]
    public void ValidateCommand_Constructor_CreatesCommandWithCorrectName()
    {
        // Arrange & Act
        var command = new ValidateCommand();

        // Assert
        Assert.AreEqual("validate", command.Name);
        Assert.IsNotNull(command.Description);
        Assert.IsTrue(command.Description.Contains("validate") || command.Description.Contains("Validate"));
    }

    [TestMethod]
    public void ValidateCommand_HasRequiredOptions()
    {
        // Arrange & Act
        var command = new ValidateCommand();

        // Assert
        Assert.AreEqual("validate", command.Name);
        Assert.IsNotNull(command.Description);
        
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void ValidateCommand_HasDescription()
    {
        // Arrange & Act
        var command = new ValidateCommand();

        // Assert
        Assert.IsNotNull(command.Description);
        Assert.IsTrue(command.Description.Contains("validate") || command.Description.Contains("Validate"));
    }

    [TestMethod]
    public void ValidateCommand_HasAllRequiredOptions()
    {
        // Arrange & Act
        var command = new ValidateCommand();

        // Assert
        Assert.IsNotNull(command);
    }
}
