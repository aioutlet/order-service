using Microsoft.AspNetCore.Mvc;
using OrderService.Models.DTOs;
using OrderService.Models.Enums;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    /// Get all orders
    /// </summary>
    [HttpGet]
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
    /// Get order by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<OrderResponseDto>> GetOrder(Guid id)
    {
        try
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            
            if (order == null)
            {
                return NotFound($"Order with ID {id} not found");
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
    /// Create a new order
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<OrderResponseDto>> CreateOrder(CreateOrderDto createOrderDto)
    {
        try
        {
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
    /// Update order status
    /// </summary>
    [HttpPut("{id}/status")]
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
    /// Get orders by customer ID
    /// </summary>
    [HttpGet("customer/{customerId}")]
    public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetOrdersByCustomerId(string customerId)
    {
        try
        {
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
    /// Get orders by status
    /// </summary>
    [HttpGet("status/{status}")]
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
    /// Delete an order
    /// </summary>
    [HttpDelete("{id}")]
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
