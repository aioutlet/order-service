using Serilog.Context;

namespace OrderService.Middlewares;

/// <summary>
/// Middleware to handle correlation IDs for distributed tracing
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
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

        // Add to logging context
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            _logger.LogInformation("Processing request {Method} {Path} with correlation ID: {CorrelationId}",
                context.Request.Method, context.Request.Path, correlationId);

            await _next(context);
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
}

/// <summary>
/// Extension method to add correlation ID middleware
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
}
