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

        // Keep other commands using the existing Create methods for now
        rootCommand.AddCommand(CreateExportCommand());
        rootCommand.AddCommand(CreateImportCommand());
        rootCommand.AddCommand(CreateValidateCommand());

        return rootCommand;
    }

    /// <summary>
    /// Creates the migrate command.
    /// </summary>
    private static Command CreateMigrateCommand()
    {
        var command = new Command("migrate", "Run full migration from hMailServer to Stalwart")
        {
            Description = "Performs the complete migration including setup and message migration."
        };

        command.AddOption(new Option<string?>("--hmailserver", "hMailServer server address") { Arity = ArgumentArity.ZeroOrOne });
        command.AddOption(new Option<string?>("--hmail-password", "hMailServer administrator password") { Arity = ArgumentArity.ZeroOrOne });
        command.AddOption(new Option<string?>("--stalwart-url", "Stalwart API base URL") { Arity = ArgumentArity.ZeroOrOne });
        command.AddOption(new Option<string?>("--stalwart-username", "Stalwart API username") { Arity = ArgumentArity.ZeroOrOne });
        command.AddOption(new Option<string?>("--stalwart-password", "Stalwart API password") { Arity = ArgumentArity.ZeroOrOne });
        command.AddOption(new Option<bool>("--use-vandelay", "Use Vandelay for message migration") { Arity = ArgumentArity.ZeroOrOne });
        command.AddOption(new Option<bool>("--skip-messages", "Skip message migration") { Arity = ArgumentArity.ZeroOrOne });
        command.AddOption(new Option<bool>("--skip-validation", "Skip validation phase") { Arity = ArgumentArity.ZeroOrOne });
        command.AddOption(new Option<string[]>("--domain", "Specific domain(s) to migrate") { Arity = ArgumentArity.ZeroOrMore });

        command.SetHandler((context) => new Commands.MigrateCommandHandler(_serviceProvider!).ExecuteAsync(context.ParseResult));
        return command;
    }

    /// <summary>
    /// Creates the vandelay command.
    /// </summary>
    private static Command CreateVandelayCommand()
    {
        var command = new Command("vandelay", "Run Vandelay for message migration")
        {
            Description = "Executes Vandelay to migrate messages from hMailServer to Stalwart."
        };

        command.AddOption(new Option<string?>("--domain", "Specific domain to migrate") { Arity = ArgumentArity.ZeroOrOne });
        command.AddOption(new Option<string?>("--config", "Path to Vandelay configuration file") { Arity = ArgumentArity.ZeroOrOne });
        command.AddOption(new Option<bool>("--validate", "Validate Vandelay installation") { Arity = ArgumentArity.ZeroOrOne });

        command.SetHandler((context) => new Commands.VandelayCommandHandler(_serviceProvider!).ExecuteAsync(context.ParseResult));
        return command;
    }

    /// <summary>
    /// Creates the export command.
    /// </summary>
    private static Command CreateExportCommand()
    {
        var command = new Command("export", "Export data from hMailServer (fallback)")
        {
            Description = "Exports data from hMailServer to JSON and EML format."
        };

        command.AddOption(new Option<string?>("--hmailserver", "hMailServer server address") { Arity = ArgumentArity.ZeroOrOne });
        command.AddOption(new Option<string?>("--hmail-password", "hMailServer administrator password") { Arity = ArgumentArity.ZeroOrOne });
        command.AddOption(new Option<string?>("--output", "Output directory for exported files") { Arity = ArgumentArity.ZeroOrOne });
        command.AddOption(new Option<string[]>("--domain", "Specific domain(s) to export") { Arity = ArgumentArity.ZeroOrMore });

        command.SetHandler((context) => new Commands.ExportCommandHandler(_serviceProvider!).ExecuteAsync(context.ParseResult));
        return command;
    }

    /// <summary>
    /// Creates the import command.
    /// </summary>
    private static Command CreateImportCommand()
    {
        var command = new Command("import", "Import data into Stalwart (fallback)")
        {
            Description = "Imports data into Stalwart from JSON and EML files."
        };

        command.AddOption(new Option<string?>("--stalwart-url", "Stalwart API base URL") { Arity = ArgumentArity.ZeroOrOne });
        command.AddOption(new Option<string?>("--stalwart-username", "Stalwart API username") { Arity = ArgumentArity.ZeroOrOne });
        command.AddOption(new Option<string?>("--stalwart-password", "Stalwart API password") { Arity = ArgumentArity.ZeroOrOne });
        command.AddOption(new Option<string?>("--input", "Input directory for import files") { Arity = ArgumentArity.ZeroOrOne });

        command.SetHandler((context) => new Commands.ImportCommandHandler(_serviceProvider!).ExecuteAsync(context.ParseResult));
        return command;
    }

    /// <summary>
    /// Creates the validate command.
    /// </summary>
    private static Command CreateValidateCommand()
    {
        var command = new Command("validate", "Validate migration results")
        {
            Description = "Validates that all data was migrated correctly from hMailServer to Stalwart."
        };

        command.AddOption(new Option<string?>("--hmailserver", "hMailServer server address") { Arity = ArgumentArity.ZeroOrOne });
        command.AddOption(new Option<string?>("--hmail-password", "hMailServer administrator password") { Arity = ArgumentArity.ZeroOrOne });
        command.AddOption(new Option<string?>("--stalwart-url", "Stalwart API base URL") { Arity = ArgumentArity.ZeroOrOne });
        command.AddOption(new Option<string?>("--stalwart-username", "Stalwart API username") { Arity = ArgumentArity.ZeroOrOne });
        command.AddOption(new Option<string?>("--stalwart-password", "Stalwart API password") { Arity = ArgumentArity.ZeroOrOne });
        command.AddOption(new Option<string[]>("--domain", "Specific domain(s) to validate") { Arity = ArgumentArity.ZeroOrMore });

        command.SetHandler((context) => new Commands.ValidateCommandHandler(_serviceProvider!).ExecuteAsync(context.ParseResult));
        return command;
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
