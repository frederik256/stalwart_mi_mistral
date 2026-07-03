// <copyright file="MigrateCommand.cs" company="Stalwart Labs">
// Copyright (c) Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.CommandLine;

namespace StalwartMigration.CLI.Commands;

/// <summary>
/// Command for full migration workflow.
/// </summary>
public class MigrateCommand : Command
{
    /// <summary>
    /// Initializes a new instance of the MigrateCommand class.
    /// </summary>
    public MigrateCommand() : base("migrate", "Run full migration from hMailServer to Stalwart")
    {
        Description = "Performs the complete migration including setup and message migration.";

        // Add options as specified in plan
        AddOption(new Option<string?>(name: "--source", description: "Source hMailServer configuration file") { Arity = ArgumentArity.ZeroOrOne });
        AddOption(new Option<string?>(name: "--target", description: "Target Stalwart configuration file") { Arity = ArgumentArity.ZeroOrOne });
        AddOption(new Option<string?>(name: "--source-config", description: "Path to hMailServer configuration file") { Arity = ArgumentArity.ZeroOrOne });
        AddOption(new Option<string?>(name: "--target-config", description: "Path to Stalwart configuration file") { Arity = ArgumentArity.ZeroOrOne });

        // Add flags
        AddOption(new Option<bool>(name: "--setup-first", description: "Run setup phase first") { Arity = ArgumentArity.ZeroOrOne });
        AddOption(new Option<bool>(name: "--run-vandelay", description: "Run Vandelay for message migration") { Arity = ArgumentArity.ZeroOrOne });
        AddOption(new Option<bool>(name: "--resume", description: "Resume from last checkpoint") { Arity = ArgumentArity.ZeroOrOne });

        // Add option
        AddOption(new Option<string?>(name: "--last-checkpoint", description: "Resume from specific checkpoint") { Arity = ArgumentArity.ZeroOrOne });
    }
}
