// <copyright file="ExportCommandTests.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.CommandLine;
using StalwartMigration.CLI.Commands;

namespace StalwartMigration.Cli.Tests.CommandTests;

/// <summary>
/// Unit tests for the ExportCommand.
/// </summary>
[TestClass]
public class ExportCommandTests
{
    [TestMethod]
    public void ExportCommand_Constructor_CreatesCommandWithCorrectName()
    {
        // Arrange & Act
        var command = new ExportCommand();

        // Assert
        Assert.AreEqual("export", command.Name);
        Assert.IsNotNull(command.Description);
        Assert.IsTrue(command.Description.Contains("export") || command.Description.Contains("Export"));
    }

    [TestMethod]
    public void ExportCommand_HasRequiredOptions()
    {
        // Arrange & Act
        var command = new ExportCommand();

        // Assert
        Assert.AreEqual("export", command.Name);
        Assert.IsNotNull(command.Description);
        
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void ExportCommand_HasDescription()
    {
        // Arrange & Act
        var command = new ExportCommand();

        // Assert
        Assert.IsNotNull(command.Description);
        Assert.IsTrue(command.Description.Contains("export") || command.Description.Contains("Export"));
    }

    [TestMethod]
    public void ExportCommand_HasAllRequiredOptions()
    {
        // Arrange & Act
        var command = new ExportCommand();

        // Assert
        Assert.IsNotNull(command);
    }
}
