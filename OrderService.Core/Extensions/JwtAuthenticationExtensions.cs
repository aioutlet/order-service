using Microsoft.AspNetCore.Authentication.JwtBearer;
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
        // Add JWT settings configuration
        services.Configure<JwtSettings>(
            configuration.GetSection(JwtSettings.SectionName));

        // Add JWT Authentication
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
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
}
