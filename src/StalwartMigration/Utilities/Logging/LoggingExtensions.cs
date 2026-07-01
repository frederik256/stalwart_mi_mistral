// <copyright file="LoggingExtensions.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace StalwartMigration.Utilities.Logging;

/// <summary>
/// Extension methods for configuring and using logging in the Stalwart Migration application.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Configures logging with default settings for the migration tool.
    /// </summary>
    /// <param name="builder">The logging builder.</param>
    /// <param name="logLevel">The minimum log level. Defaults to Information.</param>
    /// <returns>The logging builder with migration-specific configuration.</returns>
    public static ILoggingBuilder ConfigureMigrationLogging(
        this ILoggingBuilder builder,
        LogLevel logLevel = LogLevel.Information)
    {
        return builder
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
            .AddFilter("Functional", LogLevel.Warning);
    }

    /// <summary>
    /// Logs a migration progress message.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="message">The progress message.</param>
    /// <param name="progressPercent">The progress percentage (0-100).</param>
    public static void LogMigrationProgress(
        this ILogger logger,
        string message,
        int progressPercent)
    {
        logger.LogInformation("[{Progress,3}%] {Message}", progressPercent, message);
    }

    /// <summary>
    /// Logs a domain processing message.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="domainName">The domain name being processed.</param>
    /// <param name="action">The action being performed.</param>
    public static void LogDomainAction(
        this ILogger logger,
        string domainName,
        string action)
    {
        logger.LogInformation("Domain {Domain}: {Action}", domainName, action);
    }

    /// <summary>
    /// Logs an account processing message.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="accountName">The account name being processed.</param>
    /// <param name="action">The action being performed.</param>
    public static void LogAccountAction(
        this ILogger logger,
        string accountName,
        string action)
    {
        logger.LogInformation("Account {Account}: {Action}", accountName, action);
    }

    /// <summary>
    /// Logs a statistics summary.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="total">The total count.</param>
    /// <param name="success">The successful count.</param>
    /// <param name="failed">The failed count.</param>
    /// <param name="message">The summary message.</param>
    public static void LogStatistics(
        this ILogger logger,
        int total,
        int success,
        int failed,
        string message)
    {
        logger.LogInformation("{Message}: {Success}/{Total} succeeded, {Failed} failed", 
            message, success, total, failed);
    }

    /// <summary>
    /// Logs an error with context information.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="context">Additional context information.</param>
    public static void LogErrorWithContext(
        this ILogger logger,
        Exception exception,
        string context)
    {
        logger.LogError(exception, "Error during {Context}: {Message}", context, exception.Message);
    }

    /// <summary>
    /// Creates a logger for a specific type with migration-specific configuration.
    /// </summary>
    /// <typeparam name="T">The type to create a logger for.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>A configured logger instance.</returns>
    public static ILogger<T> CreateMigrationLogger<T>(this IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<ILogger<T>>();
    }
}
