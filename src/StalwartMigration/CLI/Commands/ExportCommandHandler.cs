// <copyright file="ExportCommandHandler.cs" company="Stalwart Labs">
// Copyright (c) Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace StalwartMigration.CLI.Commands;

/// <summary>
/// Handler for the export command.
/// </summary>
public class ExportCommandHandler : CommandBase
{
    /// <summary>
    /// Initializes a new instance of the ExportCommandHandler class.
    /// </summary>
    public ExportCommandHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    /// <summary>
    /// Executes the export command.
    /// </summary>
    public override async Task<int> ExecuteAsync(ParseResult parseResult)
    {
        Logger.LogInformation("Export command not yet implemented");
        Console.WriteLine("Export command will be implemented in a future update.");
        return 0;
    }
}
