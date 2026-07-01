// <copyright file="LoggingConfiguration.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace StalwartMigration.Utilities.Logging;

/// <summary>
/// Configures logging for the Stalwart Migration application.
/// </summary>
public static class LoggingConfiguration
{
    private static ILoggerFactory? _loggerFactory;

    /// <summary>
    /// Adds logging services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="logLevel">The minimum log level. Defaults to Information.</param>
    /// <returns>The service collection with logging configured.</returns>
    public static IServiceCollection AddMigrationLogging(
        this IServiceCollection services,
        LogLevel logLevel = LogLevel.Information)
    {
        services.AddLogging(configure => configure
            .AddConsole(options =>
            {
#pragma warning disable CS0618 // ConsoleLoggerOptions.TimestampFormat and IncludeScopes are deprecated
                options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
                options.IncludeScopes = true;
#pragma warning restore CS0618
                options.LogToStandardErrorThreshold = LogLevel.Error;
            })
            .AddFilter("StalwartMigration", logLevel)
            .AddFilter("Microsoft", LogLevel.Warning)
            .AddFilter("System", LogLevel.Warning)
            .AddFilter("Functional", LogLevel.Warning));

        return services;
    }

    /// <summary>
    /// Creates a logger with the specified name.
    /// </summary>
    /// <param name="name">The logger category name.</param>
    /// <returns>An ILogger instance.</returns>
    public static ILogger CreateLogger(string name)
    {
        if (_loggerFactory == null)
        {
            // Create a default logger factory if not configured
            var serviceCollection = new ServiceCollection();
            AddMigrationLogging(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            _loggerFactory = serviceProvider.GetService<ILoggerFactory>() ?? throw new InvalidOperationException("Logger factory could not be created");
        }

        return _loggerFactory.CreateLogger(name);
    }
}
