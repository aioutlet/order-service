using Microsoft.AspNetCore.Builder;
using OrderService.Core.Middlewares;

namespace OrderService.Core.Extensions;

/// <summary>
/// Extension methods for registering custom middleware components
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Adds W3C Trace Context middleware to the pipeline.
    /// Extracts or generates traceparent headers and propagates trace IDs across requests.
    /// </summary>
    public static IApplicationBuilder UseTraceContext(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TraceContextMiddleware>();
    }

    /// <summary>
    /// Adds global error handling middleware to the pipeline.
    /// Catches unhandled exceptions and returns standardized error responses.
    /// </summary>
    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ErrorHandlingMiddleware>();
    }
}
