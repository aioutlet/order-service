using System.Diagnostics;
using System.Text;

namespace OrderService.Observability.Tracing;

/// <summary>
/// Tracing helper functions for OpenTelemetry integration
/// </summary>
public static class TracingHelpers
{
    private static bool _tracingEnabled = true;

    /// <summary>
    /// Check if tracing is enabled
    /// </summary>
    public static bool IsTracingEnabled() => _tracingEnabled;

    /// <summary>
    /// Set tracing enabled state
    /// </summary>
    public static void SetTracingEnabled(bool enabled) => _tracingEnabled = enabled;

    /// <summary>
    /// Get service information from environment variables and configuration
    /// </summary>
    public static ServiceInfo GetServiceInfo(IConfiguration? configuration = null)
    {
        return new ServiceInfo
        {
            ServiceName = Environment.GetEnvironmentVariable("SERVICE_NAME") ?? 
                         Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ??
                         configuration?.GetValue<string>("ServiceName") ??
                         "order-service",
            ServiceVersion = Environment.GetEnvironmentVariable("SERVICE_VERSION") ?? 
                            Environment.GetEnvironmentVariable("OTEL_SERVICE_VERSION") ??
                            configuration?.GetValue<string>("ServiceVersion") ??
                            "1.0.0"
        };
    }

    /// <summary>
    /// Get current trace and span IDs from Activity (OpenTelemetry context)
    /// </summary>
    public static TracingContext GetTracingContext()
    {
        if (!IsTracingEnabled())
        {
            return new TracingContext { TraceId = null, SpanId = null };
        }

        try
        {
            var activity = Activity.Current;
            if (activity == null)
            {
                return new TracingContext { TraceId = null, SpanId = null };
            }

            return new TracingContext
            {
                TraceId = activity.TraceId.ToString(),
                SpanId = activity.SpanId.ToString()
            };
        }
        catch (Exception)
        {
            // If tracing fails, return nulls
            return new TracingContext { TraceId = null, SpanId = null };
        }
    }

    /// <summary>
    /// Create a new activity (span) for operation tracking
    /// </summary>
    public static OperationSpan CreateOperationSpan(string operationName, object? attributes = null)
    {
        if (!IsTracingEnabled())
        {
            return new OperationSpan
            {
                Activity = null,
                TraceId = null,
                SpanId = null,
                Dispose = () => { },
                SetStatus = (_, __) => { },
                AddEvent = (_, __) => { }
            };
        }

        try
        {
            var activitySource = GetOrCreateActivitySource();
            var activity = activitySource.StartActivity(operationName);

            if (activity != null && attributes != null)
            {
                AddAttributesToActivity(activity, attributes);
            }

            return new OperationSpan
            {
                Activity = activity,
                TraceId = activity?.TraceId.ToString(),
                SpanId = activity?.SpanId.ToString(),
                Dispose = () => activity?.Dispose(),
                SetStatus = (code, message) => SetActivityStatus(activity, code, message),
                AddEvent = (name, eventAttributes) => AddActivityEvent(activity, name, eventAttributes)
            };
        }
        catch (Exception)
        {
            // Return a no-op span if tracing fails
            return new OperationSpan
            {
                Activity = null,
                TraceId = null,
                SpanId = null,
                Dispose = () => { },
                SetStatus = (_, __) => { },
                AddEvent = (_, __) => { }
            };
        }
    }

    /// <summary>
    /// Get or create activity source for the service
    /// </summary>
    private static ActivitySource GetOrCreateActivitySource()
    {
        var serviceInfo = GetServiceInfo();
        return new ActivitySource(serviceInfo.ServiceName, serviceInfo.ServiceVersion);
    }

    /// <summary>
    /// Add attributes to activity from object
    /// </summary>
    private static void AddAttributesToActivity(Activity activity, object attributes)
    {
        if (attributes == null) return;

        var properties = attributes.GetType().GetProperties();
        foreach (var property in properties)
        {
            try
            {
                var value = property.GetValue(attributes);
                if (value != null)
                {
                    activity.SetTag(property.Name, value.ToString());
                }
            }
            catch
            {
                // Ignore property access errors
            }
        }
    }

    /// <summary>
    /// Set activity status
    /// </summary>
    private static void SetActivityStatus(Activity? activity, ActivityStatusCode code, string? message)
    {
        if (activity != null)
        {
            activity.SetStatus(code, message);
        }
    }

    /// <summary>
    /// Add event to activity
    /// </summary>
    private static void AddActivityEvent(Activity? activity, string name, object? attributes)
    {
        if (activity != null)
        {
            var activityEvent = new ActivityEvent(name);
            if (attributes != null)
            {
                var tags = new ActivityTagsCollection();
                var properties = attributes.GetType().GetProperties();
                foreach (var property in properties)
                {
                    try
                    {
                        var value = property.GetValue(attributes);
                        if (value != null)
                        {
                            tags[property.Name] = value.ToString();
                        }
                    }
                    catch
                    {
                        // Ignore property access errors
                    }
                }
                activityEvent = new ActivityEvent(name, DateTimeOffset.UtcNow, tags);
            }
            activity.AddEvent(activityEvent);
        }
    }
}

/// <summary>
/// Service information structure
/// </summary>
public class ServiceInfo
{
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceVersion { get; set; } = string.Empty;
}

/// <summary>
/// Tracing context structure
/// </summary>
public class TracingContext
{
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
}

/// <summary>
/// Operation span wrapper for easier usage
/// </summary>
public class OperationSpan : IDisposable
{
    public Activity? Activity { get; set; }
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
    public Action Dispose { get; set; } = () => { };
    public Action<ActivityStatusCode, string?> SetStatus { get; set; } = (_, __) => { };
    public Action<string, object?> AddEvent { get; set; } = (_, __) => { };

    void IDisposable.Dispose()
    {
        Dispose();
    }
}
