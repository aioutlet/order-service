using Microsoft.AspNetCore.Builder;
using OrderService.Core.Middlewares;

namespace OrderService.Core.Extensions;

/// <summary>
/// Extension methods for registering custom middleware components
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Adds correlation ID middleware to the pipeline.
    /// Generates or propagates X-Correlation-ID headers for distributed tracing.
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
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
