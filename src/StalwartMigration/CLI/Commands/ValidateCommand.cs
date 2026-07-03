// <copyright file="ValidateCommand.cs" company="Stalwart Labs">
// Copyright (c) Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.CommandLine;

namespace StalwartMigration.CLI.Commands;

/// <summary>
/// Command for validating migration results.
/// </summary>
public class ValidateCommand : Command
{
    /// <summary>
    /// Initializes a new instance of the ValidateCommand class.
    /// </summary>
    public ValidateCommand() : base("validate", "Validate migration results")
    {
        Description = "Validates that all data was migrated correctly from hMailServer to Stalwart.";

        // Add options as specified in plan
        AddOption(new Option<string?>(name: "--source", description: "Source hMailServer configuration file") { Arity = ArgumentArity.ZeroOrOne });
        AddOption(new Option<string?>(name: "--target", description: "Target Stalwart configuration file") { Arity = ArgumentArity.ZeroOrOne });
        AddOption(new Option<string?>(name: "--source-config", description: "Path to hMailServer configuration file") { Arity = ArgumentArity.ZeroOrOne });
        AddOption(new Option<string?>(name: "--target-config", description: "Path to Stalwart configuration file") { Arity = ArgumentArity.ZeroOrOne });
        
        // Add flag
        AddOption(new Option<bool>(name: "--validate-target", description: "Test API connectivity to target") { Arity = ArgumentArity.ZeroOrOne });
        
        // Add per-domain support
        AddOption(new Option<string[]>(name: "--domain", description: "Specific domain(s) to validate") { Arity = ArgumentArity.ZeroOrMore });
    }
}
