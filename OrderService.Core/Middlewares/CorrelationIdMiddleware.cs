using Microsoft.AspNetCore.Http;
using OrderService.Core.Observability.Logging;
using System.Diagnostics;

namespace OrderService.Core.Middlewares;

/// <summary>
/// Middleware to handle correlation IDs for distributed tracing and enhanced logging
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly EnhancedLogger _logger;
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private const string TraceIdHeader = "X-Trace-ID";
    private const string SpanIdHeader = "X-Span-ID";

    public CorrelationIdMiddleware(RequestDelegate next, EnhancedLogger logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get correlation ID from header or generate new one
        var correlationId = GetOrGenerateCorrelationId(context);

        // Store in context for use in controllers/services
        context.Items["CorrelationId"] = correlationId;

        // Add to response headers
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // Get current tracing context
        var activity = Activity.Current;
        if (activity != null)
        {
            context.Response.Headers[TraceIdHeader] = activity.TraceId.ToString();
            context.Response.Headers[SpanIdHeader] = activity.SpanId.ToString();
        }

        // Log request processing start
        var stopwatch = _logger.OperationStart("PROCESS_REQUEST", correlationId, new 
        { 
            method = context.Request.Method,
            path = context.Request.Path.Value,
            userAgent = context.Request.Headers.UserAgent.ToString(),
            remoteIp = GetRemoteIpAddress(context)
        });

        try
        {
            await _next(context);

            // Log successful completion
            _logger.OperationComplete("PROCESS_REQUEST", stopwatch, correlationId, new 
            { 
                method = context.Request.Method,
                path = context.Request.Path.Value,
                statusCode = context.Response.StatusCode
            });
        }
        catch (Exception ex)
        {
            // Log failed request
            _logger.OperationFailed("PROCESS_REQUEST", stopwatch, ex, correlationId, new 
            { 
                method = context.Request.Method,
                path = context.Request.Path.Value,
                statusCode = context.Response.StatusCode
            });
            throw;
        }
    }

    private static string GetOrGenerateCorrelationId(HttpContext context)
    {
        // Try to get from header first
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId) && 
            !string.IsNullOrEmpty(correlationId))
        {
            return correlationId!;
        }

        // Generate new correlation ID
        return Guid.NewGuid().ToString();
    }

    private static string? GetRemoteIpAddress(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ??
               context.Request.Headers["X-Forwarded-For"].FirstOrDefault() ??
               context.Request.Headers["X-Real-IP"].FirstOrDefault();
    }
}
