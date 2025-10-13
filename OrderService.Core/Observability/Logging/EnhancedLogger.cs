using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Diagnostics;
using OrderService.Core.Observability.Tracing;
using Microsoft.Extensions.Configuration;

namespace OrderService.Core.Observability.Logging;

/// <summary>
/// Enhanced logger that provides the same JSON format as the user service
/// Integrates with OpenTelemetry for distributed tracing
/// </summary>
public class EnhancedLogger
{
    private readonly ILogger<EnhancedLogger> _logger;
    private readonly LoggingConfig _config;
    private readonly string _serviceName;
    private readonly string _serviceVersion;
    private readonly string _environment;

    public EnhancedLogger(ILogger<EnhancedLogger> logger, IConfiguration configuration)
    {
        _logger = logger;
        _environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        
        var envConfig = LoggingConfiguration.EnvironmentConfigs.ContainsKey(_environment)
            ? LoggingConfiguration.EnvironmentConfigs[_environment]
            : LoggingConfiguration.DefaultConfig;

        _config = new LoggingConfig
        {
            LogLevel = Environment.GetEnvironmentVariable("LOG_LEVEL") ?? 
                      configuration.GetValue<string>("Logging:LogLevel:Default") ??
                      envConfig.LogLevel,
            EnableConsole = bool.Parse(Environment.GetEnvironmentVariable("LOG_TO_CONSOLE") ?? envConfig.EnableConsole.ToString()),
            EnableFile = bool.Parse(Environment.GetEnvironmentVariable("LOG_TO_FILE") ?? envConfig.EnableFile.ToString()),
            Format = Environment.GetEnvironmentVariable("LOG_FORMAT") ?? envConfig.Format,
            EnableTracing = bool.Parse(Environment.GetEnvironmentVariable("ENABLE_TRACING") ?? envConfig.EnableTracing.ToString()),
            ServiceName = Environment.GetEnvironmentVariable("SERVICE_NAME") ?? 
                         configuration.GetValue<string>("ServiceName") ?? 
                         envConfig.ServiceName,
            ServiceVersion = Environment.GetEnvironmentVariable("SERVICE_VERSION") ?? 
                           configuration.GetValue<string>("ServiceVersion") ?? 
                           envConfig.ServiceVersion,
            FilePath = Environment.GetEnvironmentVariable("LOG_FILE_PATH") ?? GetDefaultLogPath()
        };

        _serviceName = _config.ServiceName;
        _serviceVersion = _config.ServiceVersion;

        // Set tracing enabled state
        TracingHelpers.SetTracingEnabled(_config.EnableTracing);

        // Log initialization
        Info("Logger initialized", null, new { 
            operation = "logger_initialization", 
            metadata = new { config = SanitizeConfigForLogging(_config) }
        });
    }

    /// <summary>
    /// Get default log file path
    /// </summary>
    private string GetDefaultLogPath()
    {
        var isDevelopment = _environment == "Development" || _environment == "Local";
        return isDevelopment 
            ? $"./logs/{_serviceName}-{_environment.ToLower()}.log" 
            : $"/app/logs/{_serviceName}-{_environment.ToLower()}.log";
    }

    /// <summary>
    /// Sanitize configuration for logging (remove sensitive paths in production)
    /// </summary>
    private object SanitizeConfigForLogging(LoggingConfig config)
    {
        return new
        {
            serviceName = config.ServiceName,
            version = config.ServiceVersion,
            environment = _environment,
            enableConsole = config.EnableConsole,
            enableFile = config.EnableFile,
            logLevel = config.LogLevel.ToLower(),
            format = config.Format,
            enableTracing = config.EnableTracing,
            filePath = _environment == "Production" ? "[REDACTED]" : config.FilePath
        };
    }

    /// <summary>
    /// Core logging method that creates the unified log entry format
    /// </summary>
    private void Log(string level, string message, string? correlationId = null, object? additionalData = null)
    {
        if (!ShouldLog(level)) return;

        // Get tracing context
        var tracingContext = _config.EnableTracing ? TracingHelpers.GetTracingContext() : new TracingContext();

        // Create the unified log entry
        var logEntry = new LogEntrySchema
        {
            Timestamp = DateTime.UtcNow,
            Level = level.ToUpper(),
            Service = _serviceName,
            Version = _serviceVersion,
            Environment = _environment,
            CorrelationId = correlationId,
            Message = message,
            TraceId = tracingContext.TraceId,
            SpanId = tracingContext.SpanId
        };

        // Add additional data if provided
        if (additionalData != null)
        {
            var properties = additionalData.GetType().GetProperties();
            foreach (var prop in properties)
            {
                var value = prop.GetValue(additionalData);
                switch (prop.Name.ToLower())
                {
                    case "operation":
                        logEntry.Operation = value?.ToString();
                        break;
                    case "duration":
                        if (value != null && long.TryParse(value.ToString(), out var duration))
                            logEntry.Duration = duration;
                        break;
                    case "userid":
                        logEntry.UserId = value?.ToString();
                        break;
                    case "businessevent":
                        logEntry.BusinessEvent = value?.ToString();
                        break;
                    case "securityevent":
                        logEntry.SecurityEvent = value?.ToString();
                        break;
                    case "error":
                        logEntry.Error = ProcessError(value);
                        break;
                    case "metadata":
                        logEntry.Metadata = ProcessMetadata(value);
                        break;
                    default:
                        // Add to metadata
                        logEntry.Metadata ??= new Dictionary<string, object?>();
                        logEntry.Metadata[prop.Name] = value;
                        break;
                }
            }
        }

        // Add tracing metadata if available
        if (!string.IsNullOrEmpty(tracingContext.TraceId))
        {
            logEntry.Metadata ??= new Dictionary<string, object?>();
            logEntry.Metadata["trace_id"] = tracingContext.TraceId;
            logEntry.Metadata["span_id"] = tracingContext.SpanId;
            logEntry.Metadata["trace_flags"] = "01";
        }

        // Serialize and log
        var jsonLog = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        // Log using appropriate level
        switch (level.ToUpper())
        {
            case LogLevels.DEBUG:
                _logger.LogDebug(jsonLog);
                break;
            case LogLevels.INFO:
                _logger.LogInformation(jsonLog);
                break;
            case LogLevels.WARN:
                _logger.LogWarning(jsonLog);
                break;
            case LogLevels.ERROR:
                _logger.LogError(jsonLog);
                break;
            case LogLevels.FATAL:
                _logger.LogCritical(jsonLog);
                break;
        }
    }

    /// <summary>
    /// Process error object for serialization
    /// </summary>
    private ErrorInfo? ProcessError(object? error)
    {
        if (error == null) return null;

        if (error is Exception ex)
        {
            return new ErrorInfo
            {
                Name = ex.GetType().Name,
                Message = ex.Message,
                Stack = ex.StackTrace
            };
        }

        return new ErrorInfo
        {
            Name = error.GetType().Name,
            Message = error.ToString() ?? string.Empty
        };
    }

    /// <summary>
    /// Process metadata for consistent format
    /// </summary>
    private Dictionary<string, object?>? ProcessMetadata(object? metadata)
    {
        if (metadata == null) return null;

        if (metadata is Dictionary<string, object?> dict)
            return dict;

        // Convert object to dictionary
        try
        {
            var json = JsonSerializer.Serialize(metadata);
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
        }
        catch
        {
            return new Dictionary<string, object?> { ["value"] = metadata };
        }
    }

    /// <summary>
    /// Check if we should log at this level
    /// </summary>
    private bool ShouldLog(string level)
    {
        var currentLevelValue = LogLevels.Values.GetValueOrDefault(_config.LogLevel.ToUpper(), 1);
        var logLevelValue = LogLevels.Values.GetValueOrDefault(level.ToUpper(), 1);
        return logLevelValue >= currentLevelValue;
    }

    // Public logging methods

    public void Debug(string message, string? correlationId = null, object? additionalData = null)
    {
        Log(LogLevels.DEBUG, message, correlationId, additionalData);
    }

    public void Info(string message, string? correlationId = null, object? additionalData = null)
    {
        Log(LogLevels.INFO, message, correlationId, additionalData);
    }

    public void Warn(string message, string? correlationId = null, object? additionalData = null)
    {
        Log(LogLevels.WARN, message, correlationId, additionalData);
    }

    public void Error(string message, string? correlationId = null, object? additionalData = null)
    {
        Log(LogLevels.ERROR, message, correlationId, additionalData);
    }

    public void Fatal(string message, string? correlationId = null, object? additionalData = null)
    {
        Log(LogLevels.FATAL, message, correlationId, additionalData);
    }

    // Convenience methods

    public Stopwatch OperationStart(string operation, string? correlationId = null, object? additionalData = null)
    {
        Debug($"Starting operation: {operation}", correlationId, new { operation, additionalData });
        return Stopwatch.StartNew();
    }

    public long OperationComplete(string operation, Stopwatch stopwatch, string? correlationId = null, object? additionalData = null)
    {
        stopwatch.Stop();
        var duration = stopwatch.ElapsedMilliseconds;
        Info($"Completed operation: {operation}", correlationId, new { operation, duration, additionalData });
        return duration;
    }

    public long OperationFailed(string operation, Stopwatch stopwatch, Exception error, string? correlationId = null, object? additionalData = null)
    {
        stopwatch.Stop();
        var duration = stopwatch.ElapsedMilliseconds;
        Error($"Failed operation: {operation}", correlationId, new { operation, duration, error, additionalData });
        return duration;
    }

    public void Business(string eventName, string? correlationId = null, object? additionalData = null)
    {
        Info($"Business event: {eventName}", correlationId, new { businessEvent = eventName, additionalData });
    }

    public void Security(string eventName, string? correlationId = null, object? additionalData = null)
    {
        Warn($"Security event: {eventName}", correlationId, new { securityEvent = eventName, additionalData });
    }

    public void Performance(string operation, long durationMs, string? correlationId = null, object? additionalData = null)
    {
        var level = durationMs > 1000 ? LogLevels.WARN : LogLevels.INFO;
        Log(level, $"Performance: {operation}", correlationId, new { operation, duration = durationMs, additionalData });
    }
}
