// <copyright file="VandelayCommandHandler.cs" company="Stalwart Labs">
// Copyright (c) Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace StalwartMigration.CLI.Commands;

/// <summary>
/// Handler for the vandelay command.
/// </summary>
public class VandelayCommandHandler : CommandBase
{
    /// <summary>
    /// Initializes a new instance of the VandelayCommandHandler class.
    /// </summary>
    public VandelayCommandHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    /// <summary>
    /// Executes the vandelay command.
    /// </summary>
    public override async Task<int> ExecuteAsync(ParseResult parseResult)
    {
        Logger.LogInformation("Vandelay command not yet implemented");
        Console.WriteLine("Vandelay command will be implemented in a future update.");
        return 0;
    }
}
