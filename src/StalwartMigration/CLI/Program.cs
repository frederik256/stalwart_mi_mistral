// <copyright file="Program.cs" company="Stalwart Labs">
// Copyright (c) Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StalwartMigration.CLI.Commands;

namespace StalwartMigration.CLI;

/// <summary>
/// Entry point for the Stalwart Migration CLI tool.
/// </summary>
public static class Program
{
    private static ServiceProvider? _serviceProvider;
    private static ILogger<ProgramWrapper>? _logger;

    /// <summary>
    /// Main entry point for the application.
    /// </summary>
    public static async Task<int> Main(string[] args)
    {
        // Set up exception handling
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            Console.Error.WriteLine("Unhandled exception: {0}", e.ExceptionObject);
            Environment.Exit(1);
        };

        // Set up dependency injection
        _serviceProvider = BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<ProgramWrapper>>();

        // Build the command line parser
        var rootCommand = BuildRootCommand();
        var parser = new CommandLineBuilder(rootCommand)
            .UseTypoCorrections()
            .UseSuggestDirective()
            .UseHelp()
            .UseVersionOption()
            .UseEnvironmentVariableDirective()
            .Build();

        _logger.LogInformation("Stalwart Migration Tool starting...");

        try
        {
            return await parser.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Command execution failed");
            Console.Error.WriteLine("Error: {0}", ex.Message);
            return 1;
        }
        finally
        {
            _serviceProvider?.Dispose();
        }
    }

    /// <summary>
    /// Builds the root command with all subcommands.
    /// </summary>
    private static RootCommand BuildRootCommand()
    {
        var rootCommand = new RootCommand("hMailServer to Stalwart Mail Server Migration Tool")
        {
            Description = "Migrate from hMailServer to Stalwart Mail Server with support for accounts, domains, aliases, and messages."
        };

        // Use the new Command classes
        var setupCommand = new Commands.SetupCommand();
        setupCommand.SetHandler((context) => new Commands.SetupCommandHandler(_serviceProvider!).ExecuteAsync(context.ParseResult));
        rootCommand.AddCommand(setupCommand);

        var migrateCommand = new Commands.MigrateCommand();
        migrateCommand.SetHandler((context) => new Commands.MigrateCommandHandler(_serviceProvider!).ExecuteAsync(context.ParseResult));
        rootCommand.AddCommand(migrateCommand);

        var vandelayCommand = new Commands.VandelayCommand();
        // Note: VandelayCommand has subcommands, so we set handlers for each subcommand
        // For now, we'll use the VandelayCommandHandler for all subcommands
        foreach (var subCommand in vandelayCommand.Children.OfType<Command>())
        {
            subCommand.SetHandler((context) => new Commands.VandelayCommandHandler(_serviceProvider!).ExecuteAsync(context.ParseResult));
        }
        rootCommand.AddCommand(vandelayCommand);

        // Use the new ExportCommand class
        var exportCommand = new Commands.ExportCommand();
        exportCommand.SetHandler((context) => new Commands.ExportCommandHandler(_serviceProvider!).ExecuteAsync(context.ParseResult));
        rootCommand.AddCommand(exportCommand);

        // Use the new ImportCommand class
        var importCommand = new Commands.ImportCommand();
        importCommand.SetHandler((context) => new Commands.ImportCommandHandler(_serviceProvider!).ExecuteAsync(context.ParseResult));
        rootCommand.AddCommand(importCommand);

        // Use the new ValidateCommand class
        var validateCommand = new Commands.ValidateCommand();
        validateCommand.SetHandler((context) => new Commands.ValidateCommandHandler(_serviceProvider!).ExecuteAsync(context.ParseResult));
        rootCommand.AddCommand(validateCommand);

        return rootCommand;
    }

    /// <summary>
    /// Builds the service provider with dependency injection.
    /// </summary>
    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddLogging(configure => configure.AddConsole());

        // Note: Command handlers are instantiated directly in SetHandler callbacks
        // and passed the service provider, so they don't need to be registered here.
        // CommandBase is abstract and cannot be instantiated.

        return services.BuildServiceProvider();
    }

    // Wrapper class for logging (since Program is static)
    private sealed class ProgramWrapper { }
}
