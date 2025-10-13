using OrderService.Core.Observability.Logging;
using OrderService.Core.Observability.Tracing;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace OrderService.Api.Observability;

/// <summary>
/// Main observability configuration and setup
/// </summary>
public static class ObservabilitySetup
{
    /// <summary>
    /// Configure comprehensive logging and tracing for the application
    /// </summary>
    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        // Configure Serilog for structured logging
        ConfigureSerilog(builder);
        
        // Add distributed tracing
        builder.Services.AddDistributedTracing(builder.Configuration);
        
        // Register enhanced logger
        builder.Services.AddSingleton<EnhancedLogger>();
        
        return builder;
    }

    /// <summary>
    /// Configure Serilog with structured logging optimized for the enhanced logger
    /// </summary>
    private static void ConfigureSerilog(WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var environment = builder.Environment.EnvironmentName;
        
        // Get logging configuration
        var envConfig = LoggingConfiguration.EnvironmentConfigs.ContainsKey(environment)
            ? LoggingConfiguration.EnvironmentConfigs[environment] 
            : LoggingConfiguration.DefaultConfig;

        var logLevel = Enum.Parse<LogEventLevel>(
            Environment.GetEnvironmentVariable("LOG_LEVEL") ?? 
            configuration.GetValue<string>("Logging:LogLevel:Default") ?? 
            envConfig.LogLevel, true);

        var enableConsole = bool.Parse(Environment.GetEnvironmentVariable("LOG_TO_CONSOLE") ?? envConfig.EnableConsole.ToString());
        var enableFile = bool.Parse(Environment.GetEnvironmentVariable("LOG_TO_FILE") ?? envConfig.EnableFile.ToString());
        
        var serviceName = Environment.GetEnvironmentVariable("SERVICE_NAME") ?? 
                         configuration.GetValue<string>("ServiceName") ?? 
                         envConfig.ServiceName;
        
        var logFilePath = Environment.GetEnvironmentVariable("LOG_FILE_PATH") ?? 
                         GetDefaultLogPath(environment, serviceName);

        // Configure Serilog
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Is(logLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId();

        // Add console sink for development environments
        if (enableConsole && environment != "Test")
        {
            loggerConfig = loggerConfig.WriteTo.Console(
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message}{NewLine}{Exception}",
                theme: AnsiConsoleTheme.Code);
        }

        // Add file sink
        if (enableFile)
        {
            // Ensure log directory exists
            var logDir = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            loggerConfig = loggerConfig.WriteTo.File(
                path: logFilePath,
                outputTemplate: "{Message}{NewLine}",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                buffered: false,
                shared: true);

            // Add separate file for errors
            loggerConfig = loggerConfig.WriteTo.File(
                path: logFilePath.Replace(".log", "-errors.log"),
                restrictedToMinimumLevel: LogEventLevel.Error,
                outputTemplate: "{Message}{NewLine}",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                buffered: false,
                shared: true);
        }

        // Create and configure the logger
        Log.Logger = loggerConfig.CreateLogger();

        // Replace default logging with Serilog
        builder.Host.UseSerilog();
        
        // Add cleanup on shutdown
        builder.Services.AddHostedService<LoggingCleanupService>();
    }

    /// <summary>
    /// Get default log file path based on environment
    /// </summary>
    private static string GetDefaultLogPath(string environment, string serviceName)
    {
        var isDevelopment = environment == "Development" || environment == "Local";
        return isDevelopment 
            ? $"./logs/{serviceName}-{environment.ToLower()}.log"
            : $"/app/logs/{serviceName}-{environment.ToLower()}.log";
    }
}

/// <summary>
/// Cleanup service for proper logging shutdown
/// </summary>
public class LoggingCleanupService : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Log.CloseAndFlush();
        return Task.CompletedTask;
    }
}
