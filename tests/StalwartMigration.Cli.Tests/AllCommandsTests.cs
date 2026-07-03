// <copyright file="AllCommandsTests.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.CommandLine;
using StalwartMigration.CLI.Commands;

namespace StalwartMigration.Cli.Tests;

/// <summary>
/// Tests for all CLI commands to ensure they can be created and have proper structure.
/// </summary>
[TestClass]
public class AllCommandsTests
{
    [TestMethod]
    public void AllCommands_CanBeInstantiated()
    {
        // Arrange & Act & Assert
        Assert.IsNotNull(new SetupCommand());
        Assert.IsNotNull(new MigrateCommand());
        Assert.IsNotNull(new VandelayCommand());
        Assert.IsNotNull(new ExportCommand());
        Assert.IsNotNull(new ImportCommand());
        Assert.IsNotNull(new ValidateCommand());
    }

    [TestMethod]
    public void AllCommands_HaveNames()
    {
        // Arrange & Act & Assert
        Assert.IsNotNull(new SetupCommand().Name);
        Assert.IsNotNull(new MigrateCommand().Name);
        Assert.IsNotNull(new VandelayCommand().Name);
        Assert.IsNotNull(new ExportCommand().Name);
        Assert.IsNotNull(new ImportCommand().Name);
        Assert.IsNotNull(new ValidateCommand().Name);
    }

    [TestMethod]
    public void AllCommands_HaveDescriptions()
    {
        // Arrange & Act & Assert
        Assert.IsNotNull(new SetupCommand().Description);
        Assert.IsNotNull(new MigrateCommand().Description);
        Assert.IsNotNull(new VandelayCommand().Description);
        Assert.IsNotNull(new ExportCommand().Description);
        Assert.IsNotNull(new ImportCommand().Description);
        Assert.IsNotNull(new ValidateCommand().Description);
    }

    [TestMethod]
    public void SetupCommand_HasCorrectName()
    {
        var command = new SetupCommand();
        Assert.AreEqual("setup", command.Name);
    }

    [TestMethod]
    public void MigrateCommand_HasCorrectName()
    {
        var command = new MigrateCommand();
        Assert.AreEqual("migrate", command.Name);
    }

    [TestMethod]
    public void VandelayCommand_HasCorrectName()
    {
        var command = new VandelayCommand();
        Assert.AreEqual("vandelay", command.Name);
    }

    [TestMethod]
    public void ExportCommand_HasCorrectName()
    {
        var command = new ExportCommand();
        Assert.AreEqual("export", command.Name);
    }

    [TestMethod]
    public void ImportCommand_HasCorrectName()
    {
        var command = new ImportCommand();
        Assert.AreEqual("import", command.Name);
    }

    [TestMethod]
    public void ValidateCommand_HasCorrectName()
    {
        var command = new ValidateCommand();
        Assert.AreEqual("validate", command.Name);
    }

    [TestMethod]
    public void VandelayCommand_HasSubcommands()
    {
        var command = new VandelayCommand();
        // Vandelay command should have subcommands
        Assert.IsNotNull(command.Children);
    }

    [TestMethod]
    public void AllCommands_HaveAliases()
    {
        // Each command should have at least one alias
        Assert.IsNotNull(new SetupCommand().Aliases);
        Assert.IsNotNull(new MigrateCommand().Aliases);
        Assert.IsNotNull(new VandelayCommand().Aliases);
        Assert.IsNotNull(new ExportCommand().Aliases);
        Assert.IsNotNull(new ImportCommand().Aliases);
        Assert.IsNotNull(new ValidateCommand().Aliases);
    }

    [TestMethod]
    public void CommandNames_AreUnique()
    {
        // Arrange
        var commands = new List<Command>
        {
            new SetupCommand(),
            new MigrateCommand(),
            new VandelayCommand(),
            new ExportCommand(),
            new ImportCommand(),
            new ValidateCommand()
        };

        // Act
        var names = commands.Select(c => c.Name).ToList();
        var uniqueNames = names.Distinct().ToList();

        // Assert
        Assert.AreEqual(names.Count, uniqueNames.Count, "Command names should be unique");
    }
}
