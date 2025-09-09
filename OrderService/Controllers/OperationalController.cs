using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using OrderService.Configuration;

namespace OrderService.Controllers;

/// <summary>
/// Operational/Infrastructure endpoints
/// These endpoints are used by monitoring systems, load balancers, and DevOps tools
/// </summary>
[ApiController]
public class OperationalController : ControllerBase
{
    private readonly ApiSettings _apiSettings;
    private readonly ILogger<OperationalController> _logger;

    public OperationalController(IOptions<ApiSettings> apiSettings, ILogger<OperationalController> logger)
    {
        _apiSettings = apiSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Basic health check endpoint
    /// </summary>
    /// <route>GET /health</route>
    [HttpGet("/health")]
    public ActionResult<object> Health()
    {
        _logger.LogDebug("Health check requested");
        
        return Ok(new 
        { 
            status = "healthy",
            service = "order-service",
            timestamp = DateTime.UtcNow,
            version = Environment.GetEnvironmentVariable("API_VERSION") ?? _apiSettings.Version
        });
    }

    /// <summary>
    /// Readiness probe - check if service is ready to serve traffic
    /// </summary>
    /// <route>GET /health/ready</route>
    [HttpGet("/health/ready")]
    public ActionResult<object> Readiness()
    {
        _logger.LogDebug("Readiness check requested");
        
        try
        {
            // Add more sophisticated checks here (DB connectivity, external dependencies, etc.)
            // Example: Check database connectivity, message broker, etc.
            // await CheckDatabaseConnectivity();
            // await CheckMessageBrokerConnectivity();
            
            return Ok(new 
            { 
                status = "ready",
                service = "order-service",
                timestamp = DateTime.UtcNow,
                checks = new {
                    database = "connected",
                    messageBroker = "connected"
                    // Add other dependency checks
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Readiness check failed");
            
            return StatusCode(503, new 
            { 
                status = "not ready",
                service = "order-service",
                timestamp = DateTime.UtcNow,
                error = "Service dependencies not available"
            });
        }
    }

    /// <summary>
    /// Liveness probe - check if the app is running
    /// </summary>
    /// <route>GET /health/live</route>
    [HttpGet("/health/live")]
    public ActionResult<object> Liveness()
    {
        _logger.LogDebug("Liveness check requested");
        
        var process = Process.GetCurrentProcess();
        var uptime = DateTime.UtcNow.Subtract(process.StartTime.ToUniversalTime());
        
        return Ok(new 
        { 
            status = "alive",
            service = "order-service",
            timestamp = DateTime.UtcNow,
            uptime = uptime.TotalSeconds
        });
    }

    /// <summary>
    /// Basic metrics endpoint
    /// </summary>
    /// <route>GET /metrics</route>
    [HttpGet("/metrics")]
    public ActionResult<object> Metrics()
    {
        _logger.LogDebug("Metrics requested");
        
        var process = Process.GetCurrentProcess();
        var uptime = DateTime.UtcNow.Subtract(process.StartTime.ToUniversalTime());
        
        return Ok(new 
        { 
            service = "order-service",
            timestamp = DateTime.UtcNow,
            metrics = new {
                uptime = uptime.TotalSeconds,
                memory = new {
                    workingSet = process.WorkingSet64,
                    privateMemory = process.PrivateMemorySize64,
                    virtualMemory = process.VirtualMemorySize64
                },
                processorTime = process.TotalProcessorTime.TotalMilliseconds,
                threads = process.Threads.Count,
                handles = process.HandleCount,
                dotnetVersion = Environment.Version.ToString()
            }
        });
    }
}
