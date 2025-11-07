using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OrderService.Core.Models.DTOs;
using OrderService.Core.Services;
using OrderService.Core.Observability.Logging;

namespace OrderService.Controllers;

/// <summary>
/// Admin-specific endpoints for order management
/// </summary>
[ApiController]
[Route("api/admin/orders")]
[Authorize(Policy = "AdminOnly")]
public class AdminOrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly EnhancedLogger _logger;

    public AdminOrdersController(
        IOrderService orderService,
        EnhancedLogger logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// Get order statistics for admin dashboard
    /// </summary>
    /// <route>GET /api/admin/orders/stats</route>
    [HttpGet("stats")]
    public async Task<ActionResult<OrderStatsDto>> GetStats(
        [FromQuery] bool includeRecent = false,
        [FromQuery] int recentLimit = 10)
    {
        var correlationId = GetCorrelationId();
        var stopwatch = _logger.OperationStart("GET_ORDER_STATS", correlationId, new
        {
            operation = "GET_ORDER_STATS",
            endpoint = "GET /api/admin/orders/stats",
            includeRecent,
            recentLimit
        });

        try
        {
            var stats = await _orderService.GetStatsAsync(includeRecent, recentLimit);

            _logger.OperationComplete("GET_ORDER_STATS", stopwatch, correlationId, new
            {
                total = stats.Total,
                pending = stats.Pending,
                completed = stats.Completed,
                revenue = stats.Revenue,
                endpoint = "GET /api/admin/orders/stats"
            });

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.OperationFailed("GET_ORDER_STATS", stopwatch, ex, correlationId, new
            {
                endpoint = "GET /api/admin/orders/stats",
                includeRecent,
                recentLimit
            });
            return StatusCode(500, "An error occurred while fetching order statistics");
        }
    }

    /// <summary>
    /// Helper method to get correlation ID from context
    /// </summary>
    private string GetCorrelationId()
    {
        return HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
    }
}
