// <copyright file="ImportCommand.cs" company="Stalwart Labs">
// Copyright (c) Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.CommandLine;

namespace StalwartMigration.CLI.Commands;

/// <summary>
/// Command for importing data into Stalwart (fallback when Vandelay is unavailable).
/// </summary>
public class ImportCommand : Command
{
    /// <summary>
    /// Initializes a new instance of the ImportCommand class.
    /// </summary>
    public ImportCommand() : base("import", "Import data into Stalwart (fallback)")
    {
        Description = "Imports data into Stalwart from JSON and EML files.";

        // Add options as specified in plan
        AddOption(new Option<string?>(name: "--target", description: "Target Stalwart configuration") { Arity = ArgumentArity.ZeroOrOne });
        AddOption(new Option<string?>(name: "--config", description: "Path to import configuration file") { Arity = ArgumentArity.ZeroOrOne });
        AddOption(new Option<string?>(name: "--input", description: "Input directory for import files") { Arity = ArgumentArity.ZeroOrOne });
    }
}
