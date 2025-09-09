using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace OrderService.Middlewares;

/// <summary>
/// Rate limiting configuration and middleware for Order Service
/// Implements .NET 8 built-in rate limiting capabilities
/// </summary>
public static class RateLimitingConfiguration
{
    public const string GeneralPolicy = "GeneralPolicy";
    public const string OrderCreationPolicy = "OrderCreationPolicy";
    public const string OrderUpdatePolicy = "OrderUpdatePolicy";
    public const string OrderRetrievalPolicy = "OrderRetrievalPolicy";
    public const string AdminPolicy = "AdminPolicy";

    /// <summary>
    /// Configure rate limiting services
    /// </summary>
    public static IServiceCollection AddRateLimitingServices(this IServiceCollection services, IConfiguration configuration)
    {
        var rateLimitConfig = configuration.GetSection("RateLimiting");
        var isEnabled = rateLimitConfig.GetValue<bool>("Enabled", false); // Default to false for safety
        
        // Also check if we're in test environment
        var environment = configuration.GetValue<string>("ENVIRONMENT") ?? 
                         configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") ?? 
                         Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? 
                         "Production";
        
        // Disable rate limiting in test environments
        if (environment.Equals("Test", StringComparison.OrdinalIgnoreCase) || 
            environment.Equals("Testing", StringComparison.OrdinalIgnoreCase))
        {
            isEnabled = false;
        }

        if (!isEnabled)
        {
            // Add a no-op rate limiter if disabled
            services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                    PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    {
                        return RateLimitPartition.GetNoLimiter<string>("disabled");
                    })
                );
            });
            return services;
        }

        var windowSizeInMinutes = rateLimitConfig.GetValue<int>("WindowSizeInMinutes", 15);
        var requestLimit = rateLimitConfig.GetValue<int>("RequestLimit", 1000);

        services.AddRateLimiter(options =>
        {
            // General API endpoints - lenient
            options.AddPolicy(GeneralPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = requestLimit,
                        Window = TimeSpan.FromMinutes(windowSizeInMinutes),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10
                    }));

            // Order creation - more restrictive
            options.AddPolicy(OrderCreationPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = Math.Max(requestLimit / 4, 50), // Quarter of general limit, minimum 50
                        Window = TimeSpan.FromMinutes(windowSizeInMinutes),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 5
                    }));

            // Order updates - moderate
            options.AddPolicy(OrderUpdatePolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = Math.Max(requestLimit / 2, 100), // Half of general limit, minimum 100
                        Window = TimeSpan.FromMinutes(windowSizeInMinutes),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10
                    }));

            // Order retrieval - lenient
            options.AddPolicy(OrderRetrievalPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = requestLimit * 2, // Double the general limit for reads
                        Window = TimeSpan.FromMinutes(windowSizeInMinutes),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 20
                    }));

            // Admin operations - restrictive
            options.AddPolicy(AdminPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = Math.Max(requestLimit / 10, 20), // Tenth of general limit, minimum 20
                        Window = TimeSpan.FromMinutes(windowSizeInMinutes),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 3
                    }));

            // Global limiter as fallback
            options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: GetPartitionKey(httpContext),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = requestLimit * 3, // Higher limit for global
                            Window = TimeSpan.FromMinutes(windowSizeInMinutes),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 50
                        })));

            // Rate limit rejection response
            options.OnRejected = async (context, token) =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var correlationId = context.HttpContext.Items["CorrelationId"]?.ToString() ?? "unknown";

                logger.LogWarning("Rate limit exceeded for {IP} on path {Path}. CorrelationId: {CorrelationId}",
                    context.HttpContext.Connection.RemoteIpAddress,
                    context.HttpContext.Request.Path,
                    correlationId);

                context.HttpContext.Response.StatusCode = 429;
                context.HttpContext.Response.Headers.RetryAfter = "60"; // Retry after 60 seconds

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Rate limit exceeded",
                    message = "Too many requests. Please try again later.",
                    retryAfter = 60,
                    correlationId
                }, cancellationToken: token);
            };
        });

        return services;
    }

    /// <summary>
    /// Get partition key for rate limiting based on IP and user context
    /// </summary>
    private static string GetPartitionKey(HttpContext httpContext)
    {
        // Skip rate limiting for health checks
        var path = httpContext.Request.Path.Value?.ToLowerInvariant();
        if (path != null && (path.StartsWith("/health") || path.StartsWith("/metrics")))
        {
            return "health-check";
        }

        // Use user ID if available, otherwise use IP
        var userId = httpContext.User?.FindFirst("sub")?.Value ?? 
                    httpContext.User?.FindFirst("userId")?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user:{userId}";
        }

        // Fallback to IP address
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{ipAddress}";
    }
}

/// <summary>
/// Attribute to apply specific rate limiting policies to controllers/actions
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RateLimitAttribute : Attribute
{
    public string Policy { get; }

    public RateLimitAttribute(string policy)
    {
        Policy = policy;
    }
}

/// <summary>
/// Extension methods for applying rate limiting to controllers
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Apply rate limiting middleware to the application
    /// </summary>
    public static IApplicationBuilder UseOrderServiceRateLimiting(this IApplicationBuilder app, IConfiguration configuration)
    {
        var rateLimitConfig = configuration.GetSection("RateLimiting");
        var isEnabled = rateLimitConfig.GetValue<bool>("Enabled");

        if (isEnabled)
        {
            app.UseRateLimiter();
        }

        return app;
    }
}
