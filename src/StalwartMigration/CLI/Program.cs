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

        // Parse arguments
        var parseResult = parser.Parse(args);

        _logger.LogInformation("Stalwart Migration Tool starting...");

        try
        {
            return await ExecuteCommandAsync(parseResult);
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

        rootCommand.AddCommand(CreateSetupCommand());
        rootCommand.AddCommand(CreateMigrateCommand());
        rootCommand.AddCommand(CreateVandelayCommand());
        rootCommand.AddCommand(CreateExportCommand());
        rootCommand.AddCommand(CreateImportCommand());
        rootCommand.AddCommand(CreateValidateCommand());

        return rootCommand;
    }

    /// <summary>
    /// Creates the setup command.
    /// </summary>
    private static Command CreateSetupCommand()
    {
        var command = new Command("setup", "Setup domains, accounts, and aliases in Stalwart")
        {
            Description = "Creates the domain, account, and alias infrastructure in Stalwart Mail Server."
        };

        command.AddOption(new Option<string?>("--hmailserver", "hMailServer server address") { Arity = ArgumentArity.ZeroOrOne });
        command.AddOption(new Option<string?>("--hmail-password", "hMailServer administrator password") { Arity = ArgumentArity.ZeroOrOne });
        command.AddOption(new Option<string?>("--stalwart-url", "Stalwart API base URL") { Arity = ArgumentArity.ZeroOrOne });
        command.AddOption(new Option<string?>("--stalwart-username", "Stalwart API username") { Arity = ArgumentArity.ZeroOrOne });
        command.AddOption(new Option<string?>("--stalwart-password", "Stalwart API password") { Arity = ArgumentArity.ZeroOrOne });
        command.AddOption(new Option<string[]>("--domain", "Specific domain(s) to setup") { Arity = ArgumentArity.ZeroOrMore });
        command.AddOption(new Option<bool>("--dry-run", "Perform a dry run without making changes") { Arity = ArgumentArity.ZeroOrOne });
        command.AddOption(new Option<string?>("--output", "Output directory for logs and reports") { Arity = ArgumentArity.ZeroOrOne });

        command.SetHandler((context) => new Commands.SetupCommandHandler(_serviceProvider!).ExecuteAsync(context.ParseResult));
        return command;
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

        services.AddTransient<Commands.CommandBase>();
        services.AddTransient<Commands.SetupCommandHandler>();
        services.AddTransient<Commands.MigrateCommandHandler>();
        services.AddTransient<Commands.VandelayCommandHandler>();
        services.AddTransient<Commands.ExportCommandHandler>();
        services.AddTransient<Commands.ImportCommandHandler>();
        services.AddTransient<Commands.ValidateCommandHandler>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Executes the parsed command.
    /// </summary>
    private static async Task<int> ExecuteCommandAsync(ParseResult parseResult)
    {
        var commandName = parseResult.CommandResult.Command.Name;
        _logger!.LogDebug("Executing command: {CommandName}", commandName);

        CommandBase? commandHandler = commandName switch
        {
            "setup" => _serviceProvider!.GetRequiredService<Commands.SetupCommandHandler>(),
            "migrate" => _serviceProvider!.GetRequiredService<Commands.MigrateCommandHandler>(),
            "vandelay" => _serviceProvider!.GetRequiredService<Commands.VandelayCommandHandler>(),
            "export" => _serviceProvider!.GetRequiredService<Commands.ExportCommandHandler>(),
            "import" => _serviceProvider!.GetRequiredService<Commands.ImportCommandHandler>(),
            "validate" => _serviceProvider!.GetRequiredService<Commands.ValidateCommandHandler>(),
            _ => null
        };

        if (commandHandler == null)
        {
            _logger.LogError("Unknown command: {CommandName}", commandName);
            return 1;
        }

        return await commandHandler.ExecuteAsync(parseResult);
    }

    // Wrapper class for logging (since Program is static)
    private sealed class ProgramWrapper { }
}
