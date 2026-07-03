// <copyright file="VandelayCommand.cs" company="Stalwart Labs">
// Copyright (c) Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.CommandLine;

namespace StalwartMigration.CLI.Commands;

/// <summary>
/// Command for Vandelay-specific operations.
/// </summary>
public class VandelayCommand : Command
{
    /// <summary>
    /// Initializes a new instance of the VandelayCommand class.
    /// </summary>
    public VandelayCommand() : base("vandelay", "Run Vandelay for message migration")
    {
        Description = "Executes Vandelay to migrate messages from hMailServer to Stalwart.";

        // Create subcommands as specified in plan
        var installCommand = new Command("install", "Validate and install Vandelay")
        {
            Description = "Validates Vandelay installation or installs it if not present."
        };
        installCommand.AddOption(new Option<string?>(name: "--version", description: "Specific Vandelay version to install") { Arity = ArgumentArity.ZeroOrOne });
        AddCommand(installCommand);

        var checkCommand = new Command("check", "Check Vandelay installation")
        {
            Description = "Checks if Vandelay is properly installed and accessible."
        };
        AddCommand(checkCommand);

        var runImportCommand = new Command("run-import", "Run Vandelay import only")
        {
            Description = "Runs Vandelay import command to migrate messages."
        };
        runImportCommand.AddOption(new Option<string?>(name: "--config", description: "Path to Vandelay configuration file") { Arity = ArgumentArity.ZeroOrOne });
        runImportCommand.AddOption(new Option<string[]>(name: "--domain", description: "Specific domain(s) to import") { Arity = ArgumentArity.ZeroOrMore });
        AddCommand(runImportCommand);

        var runExportCommand = new Command("run-export", "Run Vandelay export only")
        {
            Description = "Runs Vandelay export command to extract messages."
        };
        runExportCommand.AddOption(new Option<string?>(name: "--config", description: "Path to Vandelay configuration file") { Arity = ArgumentArity.ZeroOrOne });
        runExportCommand.AddOption(new Option<string[]>(name: "--domain", description: "Specific domain(s) to export") { Arity = ArgumentArity.ZeroOrMore });
        AddCommand(runExportCommand);
    }
}
