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
    /// Get order by ID (Customer can view own orders, Admin can view all)
    /// </summary>
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

            var order = await _orderService.CreateOrderAsync(createOrderDto);
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
    /// Get orders by status (Admin only)
    /// </summary>
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
