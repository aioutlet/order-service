using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace OrderService.Controllers;

[ApiController]
[Route("")]
public class HomeController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IConfiguration configuration, ILogger<HomeController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Get welcome message
    /// </summary>
    /// <route>GET /</route>
    [HttpGet]
    public ActionResult<object> GetWelcomeMessage()
    {
        _logger.LogInformation("Welcome message requested");
        
        return Ok(new 
        { 
            message = "Welcome to the Order Service",
            service = _configuration["ServiceName"] ?? "Order Service",
            description = "RESTful API for managing orders in the e-commerce platform"
        });
    }

    /// <summary>
    /// Get API version information
    /// </summary>
    /// <route>GET /version</route>
    [HttpGet("version")]
    public ActionResult<object> GetVersion()
    {
        _logger.LogInformation("Version information requested");
        
        var version = _configuration["ServiceVersion"] ?? "1.0.0";
        
        return Ok(new 
        { 
            version = version,
            service = _configuration["ServiceName"] ?? "Order Service",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        });
    }
}
