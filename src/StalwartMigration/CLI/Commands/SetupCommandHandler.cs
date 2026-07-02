// <copyright file="SetupCommandHandler.cs" company="Stalwart Labs">
// Copyright (c) Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace StalwartMigration.CLI.Commands;

/// <summary>
/// Handler for the setup command.
/// </summary>
public class SetupCommandHandler : CommandBase
{
    /// <summary>
    /// Initializes a new instance of the SetupCommandHandler class.
    /// </summary>
    public SetupCommandHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    /// <summary>
    /// Executes the setup command.
    /// </summary>
    public override async Task<int> ExecuteAsync(ParseResult parseResult)
    {
        Logger.LogInformation("Setup command executed");
        Console.WriteLine("Setup command will be implemented to create domains, accounts, and aliases in Stalwart.");
        Console.WriteLine("This is the first phase of the migration process.");
        return 0;
    }
}
