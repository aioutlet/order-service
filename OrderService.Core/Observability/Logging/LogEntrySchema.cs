using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OrderService.Core.Observability.Logging;

/// <summary>
/// Unified log entry structure matching the user service JSON format
/// </summary>
public class LogEntrySchema
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("level")]
    [Required]
    public string Level { get; set; } = string.Empty;

    [JsonPropertyName("service")]
    [Required]
    public string Service { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    [Required] 
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("environment")]
    [Required]
    public string Environment { get; set; } = string.Empty;

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }

    [JsonPropertyName("message")]
    [Required]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("traceId")]
    public string? TraceId { get; set; }

    [JsonPropertyName("spanId")]
    public string? SpanId { get; set; }

    [JsonPropertyName("operation")]
    public string? Operation { get; set; }

    [JsonPropertyName("duration")]
    public long? Duration { get; set; }

    [JsonPropertyName("userId")]
    public string? UserId { get; set; }

    [JsonPropertyName("businessEvent")]
    public string? BusinessEvent { get; set; }

    [JsonPropertyName("securityEvent")]
    public string? SecurityEvent { get; set; }

    [JsonPropertyName("error")]
    public ErrorInfo? Error { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object?>? Metadata { get; set; }
}

/// <summary>
/// Error information structure
/// </summary>
public class ErrorInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("stack")]
    public string? Stack { get; set; }
}

/// <summary>
/// Log levels with numeric values for comparison
/// </summary>
public static class LogLevels
{
    public const string DEBUG = "DEBUG";
    public const string INFO = "INFO"; 
    public const string WARN = "WARN";
    public const string ERROR = "ERROR";
    public const string FATAL = "FATAL";

    public static readonly Dictionary<string, int> Values = new()
    {
        { DEBUG, 1 },
        { INFO, 2 },
        { WARN, 3 },
        { ERROR, 4 },
        { FATAL, 5 }
    };
}

/// <summary>
/// Default configuration for different environments
/// </summary>
public static class LoggingConfiguration
{
    public static readonly Dictionary<string, LoggingConfig> EnvironmentConfigs = new()
    {
        ["Development"] = new LoggingConfig
        {
            LogLevel = LogLevels.DEBUG,
            EnableConsole = true,
            EnableFile = true,
            Format = "console",
            EnableTracing = true
        },
        ["Local"] = new LoggingConfig
        {
            LogLevel = LogLevels.DEBUG,
            EnableConsole = true,
            EnableFile = true,
            Format = "json",
            EnableTracing = true
        },
        ["Production"] = new LoggingConfig
        {
            LogLevel = LogLevels.INFO,
            EnableConsole = false,
            EnableFile = true,
            Format = "json",
            EnableTracing = true
        },
        ["Test"] = new LoggingConfig
        {
            LogLevel = LogLevels.ERROR,
            EnableConsole = false,
            EnableFile = false,
            Format = "json",
            EnableTracing = false
        }
    };

    public static readonly LoggingConfig DefaultConfig = new()
    {
        LogLevel = LogLevels.INFO,
        EnableConsole = true,
        EnableFile = true,
        Format = "json",
        EnableTracing = true
    };
}

/// <summary>
/// Logging configuration structure
/// </summary>
public class LoggingConfig
{
    public string LogLevel { get; set; } = LogLevels.INFO;
    public bool EnableConsole { get; set; } = true;
    public bool EnableFile { get; set; } = true;
    public string Format { get; set; } = "json";
    public bool EnableTracing { get; set; } = true;
    public string ServiceName { get; set; } = "order-service";
    public string ServiceVersion { get; set; } = "1.0.0";
    public string? FilePath { get; set; }
}
