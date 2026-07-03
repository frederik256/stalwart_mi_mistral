// <copyright file="SetupCommand.cs" company="Stalwart Labs">
// Copyright (c) Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.CommandLine;

namespace StalwartMigration.CLI.Commands;

/// <summary>
/// Command for creating domains, accounts, and aliases in Stalwart (fills Vandelay's gap).
/// </summary>
public class SetupCommand : Command
{
    /// <summary>
    /// Initializes a new instance of the SetupCommand class.
    /// </summary>
    public SetupCommand() : base("setup", "Setup domains, accounts, and aliases in Stalwart")
    {
        Description = "Creates the domain, account, and alias infrastructure in Stalwart Mail Server.";

        // Add options as specified in plan
        AddOption(new Option<string?>(name: "--source", description: "Source hMailServer configuration file") { Arity = ArgumentArity.ZeroOrOne });
        AddOption(new Option<string?>(name: "--target", description: "Target Stalwart configuration file") { Arity = ArgumentArity.ZeroOrOne });
        AddOption(new Option<string?>(name: "--source-config", description: "Path to hMailServer configuration file") { Arity = ArgumentArity.ZeroOrOne });
        AddOption(new Option<string?>(name: "--target-config", description: "Path to Stalwart configuration file") { Arity = ArgumentArity.ZeroOrOne });

        // Add flags
        AddOption(new Option<bool>(name: "--create-domains", description: "Create domains in Stalwart") { Arity = ArgumentArity.ZeroOrOne });
        AddOption(new Option<bool>(name: "--create-accounts", description: "Create accounts in Stalwart") { Arity = ArgumentArity.ZeroOrOne });
        AddOption(new Option<bool>(name: "--migrate-aliases", description: "Migrate email aliases") { Arity = ArgumentArity.ZeroOrOne });

        // Add per-domain support
        AddOption(new Option<string[]>(name: "--domain", description: "Specific domain(s) to setup") { Arity = ArgumentArity.ZeroOrMore });
    }
}
