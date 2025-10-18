using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OrderService.Core.Configuration;
using System.Text;

namespace OrderService.Core.Extensions;

/// <summary>
/// Extension methods for configuring JWT authentication and authorization
/// </summary>
public static class JwtAuthenticationExtensions
{
    /// <summary>
    /// Adds JWT authentication to the service collection
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add JWT Authentication
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,           // Auth-service doesn't set issuer in tokens
                    ValidateAudience = false,         // Auth-service doesn't set audience in tokens
                    ValidateLifetime = true,          // Validate token expiration
                    ValidateIssuerSigningKey = true,  // Validate signature with secret key
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["Jwt:Key"] 
                            ?? throw new InvalidOperationException("JWT Key not configured")))
                };
            });

        return services;
    }

    /// <summary>
    /// Adds role-based authorization policies for Order Service
    /// </summary>
    public static IServiceCollection AddOrderServiceAuthorization(
        this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("CustomerOnly", policy => 
                policy.RequireRole("customer"));
            
            options.AddPolicy("AdminOnly", policy => 
                policy.RequireRole("admin"));
            
            // User can be EITHER customer OR admin (not both at same time)
            options.AddPolicy("CustomerOrAdmin", policy => 
                policy.RequireRole("customer", "admin"));
        });

        return services;
    }

    /// <summary>
    /// Adds authentication and authorization middleware to the pipeline
    /// Must be called after UseCorrelationId and before MapControllers
    /// </summary>
    public static IApplicationBuilder UseOrderServiceAuthentication(this IApplicationBuilder app)
    {
        // Add Authentication middleware (validates JWT tokens)
        app.UseAuthentication();
        
        // Add Authorization middleware (enforces policies)
        app.UseAuthorization();
        
        return app;
    }
}
