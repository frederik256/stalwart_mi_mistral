// <copyright file="ExportCommand.cs" company="Stalwart Labs">
// Copyright (c) Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.CommandLine;

namespace StalwartMigration.CLI.Commands;

/// <summary>
/// Command for exporting data from hMailServer (fallback when Vandelay is unavailable).
/// </summary>
public class ExportCommand : Command
{
    /// <summary>
    /// Initializes a new instance of the ExportCommand class.
    /// </summary>
    public ExportCommand() : base("export", "Export data from hMailServer (fallback)")
    {
        Description = "Exports data from hMailServer to JSON and EML format.";

        // Add options as specified in plan
        AddOption(new Option<string?>(name: "--source", description: "Source hMailServer configuration") { Arity = ArgumentArity.ZeroOrOne });
        AddOption(new Option<string?>(name: "--config", description: "Path to export configuration file") { Arity = ArgumentArity.ZeroOrOne });
        AddOption(new Option<string?>(name: "--output", description: "Output directory for exported files") { Arity = ArgumentArity.ZeroOrOne });
        AddOption(new Option<string[]>(name: "--domain", description: "Specific domain(s) to export") { Arity = ArgumentArity.ZeroOrMore });
    }
}
