// <copyright file="CommandBase.cs" company="Stalwart Labs">
// Copyright (c) Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace StalwartMigration.CLI.Commands;

/// <summary>
/// Base class for CLI command handlers.
/// </summary>
public abstract class CommandBase
{
    /// <summary>
    /// Gets the service provider.
    /// </summary>
    protected IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Gets the logger.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Initializes a new instance of the CommandBase class.
    /// </summary>
    protected CommandBase(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Logger = ServiceProvider.GetRequiredService<ILogger<CommandBase>>();
    }

    /// <summary>
    /// Executes the command.
    /// </summary>
    public abstract Task<int> ExecuteAsync(ParseResult parseResult);
}
