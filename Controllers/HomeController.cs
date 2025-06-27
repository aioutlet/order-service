using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using OrderService.Configuration;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HomeController : ControllerBase
{
    private readonly ApiSettings _apiSettings;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IOptions<ApiSettings> apiSettings, ILogger<HomeController> logger)
    {
        _apiSettings = apiSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Get welcome message
    /// </summary>
    /// <route>GET /api/home</route>
    [HttpGet]
    public ActionResult<object> GetWelcomeMessage()
    {
        _logger.LogInformation("Welcome message requested");
        
        return Ok(new 
        { 
            message = "Welcome to the Order Service",
            service = "Order Service",
            description = _apiSettings.Description
        });
    }

    /// <summary>
    /// Get API version information
    /// </summary>
    /// <route>GET /api/home/version</route>
    [HttpGet("version")]
    public ActionResult<object> GetVersion()
    {
        _logger.LogInformation("Version information requested");
        
        var version = Environment.GetEnvironmentVariable("API_VERSION") ?? _apiSettings.Version;
        
        return Ok(new 
        { 
            version = version,
            service = "Order Service",
            title = _apiSettings.Title,
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        });
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    /// <route>GET /api/home/health</route>
    [HttpGet("health")]
    public ActionResult<object> Health()
    {
        _logger.LogDebug("Health check requested");
        
        try
        {
            // Basic health check - you can extend this to check database connectivity, etc.
            var healthStatus = new
            {
                status = "ok",
                service = "Order Service",
                timestamp = DateTime.UtcNow,
                uptime = DateTime.UtcNow.Subtract(Process.GetCurrentProcess().StartTime.ToUniversalTime()),
                version = _apiSettings.Version
            };

            return Ok(healthStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            
            return StatusCode(503, new 
            { 
                status = "unhealthy",
                service = "Order Service",
                timestamp = DateTime.UtcNow,
                error = "Service is experiencing issues"
            });
        }
    }
}
