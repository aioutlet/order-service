using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OrderService.Models.DTOs;
using OrderService.Models.Enums;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Require authentication for all endpoints
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// Get all orders (Admin only)
    /// </summary>
    /// <route>GET /api/orders</route>
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetOrders()
    {
        try
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching orders");
            return StatusCode(500, "An error occurred while fetching orders");
        }
    }

    /// <summary>
    /// Get orders with pagination and filtering (Admin only)
    /// </summary>
    /// <route>GET /api/orders/paged</route>
    [HttpGet("paged")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<PagedResponseDto<OrderResponseDto>>> GetOrdersPaged([FromQuery] OrderQueryDto query)
    {
        try
        {
            var pagedOrders = await _orderService.GetOrdersPagedAsync(query);
            return Ok(pagedOrders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching paged orders");
            return StatusCode(500, "An error occurred while fetching orders");
        }
    }

    /// <summary>
    /// Get order by ID (Customer can view own orders, Admin can view all)
    /// </summary>
    /// <route>GET /api/orders/{id}</route>
    [HttpGet("{id}")]
    [Authorize(Policy = "CustomerOrAdmin")]
    public async Task<ActionResult<OrderResponseDto>> GetOrder(Guid id)
    {
        try
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            
            if (order == null)
            {
                return NotFound($"Order with ID {id} not found");
            }

            // Check if customer is trying to access their own order
            var customerIdFromToken = User.FindFirst("sub")?.Value;
            var isAdmin = User.HasClaim("roles", "admin");
            
            if (!isAdmin && customerIdFromToken != order.CustomerId)
            {
                return Forbid("You can only view your own orders");
            }

            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching order with ID: {OrderId}", id);
            return StatusCode(500, "An error occurred while fetching the order");
        }
    }

    /// <summary>
    /// Create a new order (Customer only)
    /// </summary>
    /// <route>POST /api/orders</route>
    [HttpPost]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<ActionResult<OrderResponseDto>> CreateOrder(CreateOrderDto createOrderDto)
    {
        try
        {
            // Get customer ID from JWT token claims
            var customerIdFromToken = User.FindFirst("sub")?.Value;
            
            // Ensure the customer can only create orders for themselves
            if (customerIdFromToken != createOrderDto.CustomerId)
            {
                return Forbid("You can only create orders for yourself");
            }

            // Get correlation ID from context
            var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
            
            var order = await _orderService.CreateOrderAsync(createOrderDto, correlationId);
            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for customer: {CustomerId}", createOrderDto.CustomerId);
            return StatusCode(500, "An error occurred while creating the order");
        }
    }

    /// <summary>
    /// Update order status (Admin only)
    /// </summary>
    /// <route>PUT /api/orders/{id}/status</route>
    [HttpPut("{id}/status")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<OrderResponseDto>> UpdateOrderStatus(Guid id, UpdateOrderStatusDto updateStatusDto)
    {
        try
        {
            var order = await _orderService.UpdateOrderStatusAsync(id, updateStatusDto);
            
            if (order == null)
            {
                return NotFound($"Order with ID {id} not found");
            }

            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for order: {OrderId}", id);
            return StatusCode(500, "An error occurred while updating the order status");
        }
    }

    /// <summary>
    /// Get orders by customer ID (Customer can view own orders, Admin can view all)
    /// </summary>
    /// <route>GET /api/orders/customer/{customerId}</route>
    [HttpGet("customer/{customerId}")]
    [Authorize(Policy = "CustomerOrAdmin")]
    public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetOrdersByCustomerId(string customerId)
    {
        try
        {
            var customerIdFromToken = User.FindFirst("sub")?.Value;
            var isAdmin = User.HasClaim("roles", "admin");
            
            // Customers can only view their own orders, admins can view any customer's orders
            if (!isAdmin && customerIdFromToken != customerId)
            {
                return Forbid("You can only view your own orders");
            }

            var orders = await _orderService.GetOrdersByCustomerIdAsync(customerId);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching orders for customer: {CustomerId}", customerId);
            return StatusCode(500, "An error occurred while fetching orders");
        }
    }

    /// <summary>
    /// Get orders by customer ID with pagination (Customer can view own orders, Admin can view all)
    /// </summary>
    /// <route>GET /api/orders/customer/{customerId}/paged</route>
    [HttpGet("customer/{customerId}/paged")]
    [Authorize(Policy = "CustomerOrAdmin")]
    public async Task<ActionResult<PagedResponseDto<OrderResponseDto>>> GetOrdersByCustomerIdPaged(
        string customerId, 
        [FromQuery] PagedRequestDto pageRequest)
    {
        try
        {
            var customerIdFromToken = User.FindFirst("sub")?.Value;
            var isAdmin = User.HasClaim("roles", "admin");
            
            // Customers can only view their own orders, admins can view any customer's orders
            if (!isAdmin && customerIdFromToken != customerId)
            {
                return Forbid("You can only view your own orders");
            }

            var pagedOrders = await _orderService.GetOrdersByCustomerIdPagedAsync(customerId, pageRequest);
            return Ok(pagedOrders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching paged orders for customer: {CustomerId}", customerId);
            return StatusCode(500, "An error occurred while fetching orders");
        }
    }

    /// <summary>
    /// Get orders by status (Admin only)
    /// </summary>
    /// <route>GET /api/orders/status/{status}</route>
    [HttpGet("status/{status}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetOrdersByStatus(OrderStatus status)
    {
        try
        {
            var orders = await _orderService.GetOrdersByStatusAsync(status);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching orders with status: {Status}", status);
            return StatusCode(500, "An error occurred while fetching orders");
        }
    }

    /// <summary>
    /// Delete an order (Admin only)
    /// </summary>
    /// <route>DELETE /api/orders/{id}</route>
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> DeleteOrder(Guid id)
    {
        try
        {
            var result = await _orderService.DeleteOrderAsync(id);
            
            if (!result)
            {
                return NotFound($"Order with ID {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting order: {OrderId}", id);
            return StatusCode(500, "An error occurred while deleting the order");
        }
    }
}
