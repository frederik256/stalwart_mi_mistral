// <copyright file="ImportCommandHandler.cs" company="Stalwart Labs">
// Copyright (c) Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace StalwartMigration.CLI.Commands;

/// <summary>
/// Handler for the import command.
/// </summary>
public class ImportCommandHandler : CommandBase
{
    /// <summary>
    /// Initializes a new instance of the ImportCommandHandler class.
    /// </summary>
    public ImportCommandHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    /// <summary>
    /// Executes the import command.
    /// </summary>
    public override async Task<int> ExecuteAsync(ParseResult parseResult)
    {
        Logger.LogInformation("Import command not yet implemented");
        Console.WriteLine("Import command will be implemented in a future update.");
        return 0;
    }
}
