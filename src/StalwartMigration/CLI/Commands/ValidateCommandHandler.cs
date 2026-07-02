// <copyright file="ValidateCommandHandler.cs" company="Stalwart Labs">
// Copyright (c) Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace StalwartMigration.CLI.Commands;

/// <summary>
/// Handler for the validate command.
/// </summary>
public class ValidateCommandHandler : CommandBase
{
    /// <summary>
    /// Initializes a new instance of the ValidateCommandHandler class.
    /// </summary>
    public ValidateCommandHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    /// <summary>
    /// Executes the validate command.
    /// </summary>
    public override async Task<int> ExecuteAsync(ParseResult parseResult)
    {
        Logger.LogInformation("Validate command not yet implemented");
        Console.WriteLine("Validate command will be implemented in a future update.");
        return 0;
    }
}
