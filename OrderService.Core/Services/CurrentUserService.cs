using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace OrderService.Core.Services;

/// <summary>
/// Service to extract current user information from JWT authentication context
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Get the current authenticated user ID
    /// </summary>
    string? GetUserId();

    /// <summary>
    /// Get the current authenticated user's email
    /// </summary>
    string? GetUserEmail();

    /// <summary>
    /// Get the current authenticated user's roles
    /// </summary>
    IEnumerable<string> GetUserRoles();

    /// <summary>
    /// Get the current authenticated user's display name
    /// </summary>
    string? GetUserName();

    /// <summary>
    /// Check if user is authenticated
    /// </summary>
    bool IsAuthenticated();
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CurrentUserService> _logger;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, ILogger<CurrentUserService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public string? GetUserId()
    {
        var userId = GetClaimValue(ClaimTypes.NameIdentifier) ?? GetClaimValue("sub");
        
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Unable to extract user ID from JWT token");
        }
        
        return userId;
    }

    public string? GetUserEmail()
    {
        var email = GetClaimValue(ClaimTypes.Email) ?? GetClaimValue("email");
        
        if (string.IsNullOrEmpty(email))
        {
            _logger.LogWarning("Unable to extract user email from JWT token");
        }
        
        return email;
    }

    public IEnumerable<string> GetUserRoles()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null || !user.Identity?.IsAuthenticated == true)
        {
            return Enumerable.Empty<string>();
        }

        // Get roles from multiple possible claim types
        var roles = user.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "roles" || c.Type == "role")
            .Select(c => c.Value)
            .ToList();

        return roles;
    }

    public string? GetUserName()
    {
        var userName = GetClaimValue(ClaimTypes.Name) ?? 
                      GetClaimValue("name") ?? 
                      GetClaimValue("preferred_username") ??
                      GetUserEmail(); // Fallback to email if name not available
        
        return userName;
    }

    public bool IsAuthenticated()
    {
        return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
    }

    private string? GetClaimValue(string claimType)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null || !user.Identity?.IsAuthenticated == true)
        {
            return null;
        }

        return user.FindFirst(claimType)?.Value;
    }
}
