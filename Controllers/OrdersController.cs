using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OrderService.Models.DTOs;
using OrderService.Models.Enums;
using OrderService.Services;
using OrderService.Observability.Logging;
using System.Diagnostics;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Require authentication for all endpoints
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly EnhancedLogger _logger;

    public OrdersController(IOrderService orderService, EnhancedLogger logger)
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
        var correlationId = GetCorrelationId();
        var stopwatch = _logger.OperationStart("GET_ALL_ORDERS", correlationId, new { 
            operation = "GET_ALL_ORDERS",
            endpoint = "GET /api/orders"
        });

        try
        {
            var orders = await _orderService.GetAllOrdersAsync();
            
            _logger.OperationComplete("GET_ALL_ORDERS", stopwatch, correlationId, new {
                orderCount = orders.Count(),
                endpoint = "GET /api/orders"
            });

            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.OperationFailed("GET_ALL_ORDERS", stopwatch, ex, correlationId, new {
                endpoint = "GET /api/orders"
            });
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
        var correlationId = GetCorrelationId();
        var stopwatch = _logger.OperationStart("GET_ORDERS_PAGED", correlationId, new { 
            operation = "GET_ORDERS_PAGED",
            endpoint = "GET /api/orders/paged",
            page = query.Page,
            pageSize = query.PageSize
        });

        try
        {
            var pagedOrders = await _orderService.GetOrdersPagedAsync(query);
            
            _logger.OperationComplete("GET_ORDERS_PAGED", stopwatch, correlationId, new {
                totalItems = pagedOrders.TotalItems,
                page = pagedOrders.Page,
                pageSize = pagedOrders.PageSize,
                endpoint = "GET /api/orders/paged"
            });

            return Ok(pagedOrders);
        }
        catch (Exception ex)
        {
            _logger.OperationFailed("GET_ORDERS_PAGED", stopwatch, ex, correlationId, new {
                endpoint = "GET /api/orders/paged",
                page = query.Page,
                pageSize = query.PageSize
            });
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
        var correlationId = GetCorrelationId();
        var currentUserId = GetCurrentUserId();
        var isAdmin = IsCurrentUserAdmin();
        
        var stopwatch = _logger.OperationStart("GET_ORDER", correlationId, new { 
            operation = "GET_ORDER",
            endpoint = "GET /api/orders/{id}",
            orderId = id,
            requestedBy = currentUserId
        });

        try
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            
            if (order == null)
            {
                _logger.Warn($"Order with ID {id} not found", correlationId, new {
                    orderId = id,
                    endpoint = "GET /api/orders/{id}"
                });
                return NotFound($"Order with ID {id} not found");
            }

            // Check if customer is trying to access their own order
            if (!isAdmin && currentUserId != order.CustomerId)
            {
                _logger.Security("UNAUTHORIZED_ORDER_ACCESS_ATTEMPT", correlationId, new {
                    orderId = id,
                    requestedBy = currentUserId,
                    orderOwner = order.CustomerId,
                    endpoint = "GET /api/orders/{id}"
                });
                return Forbid("You can only view your own orders");
            }

            _logger.OperationComplete("GET_ORDER", stopwatch, correlationId, new {
                orderId = id,
                orderNumber = order.OrderNumber,
                customerId = order.CustomerId,
                endpoint = "GET /api/orders/{id}"
            });

            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.OperationFailed("GET_ORDER", stopwatch, ex, correlationId, new {
                orderId = id,
                endpoint = "GET /api/orders/{id}"
            });
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
        var correlationId = GetCorrelationId();
        var customerId = GetCurrentUserId();
        var stopwatch = _logger.OperationStart("CREATE_ORDER", correlationId, new { 
            operation = "CREATE_ORDER",
            endpoint = "POST /api/orders",
            customerId = customerId
        });

        try
        {
            // Ensure the customer can only create orders for themselves
            if (customerId != createOrderDto.CustomerId)
            {
                _logger.Security("UNAUTHORIZED_ORDER_CREATION_ATTEMPT", correlationId, new {
                    requestedCustomerId = createOrderDto.CustomerId,
                    actualCustomerId = customerId,
                    endpoint = "POST /api/orders"
                });
                return Forbid("You can only create orders for yourself");
            }

            var order = await _orderService.CreateOrderAsync(createOrderDto, correlationId);
            
            _logger.OperationComplete("CREATE_ORDER", stopwatch, correlationId, new {
                orderId = order.Id,
                customerId = order.CustomerId,
                totalAmount = order.TotalAmount,
                endpoint = "POST /api/orders"
            });

            _logger.Business("ORDER_CREATED", correlationId, new {
                orderId = order.Id,
                customerId = order.CustomerId,
                totalAmount = order.TotalAmount,
                orderNumber = order.OrderNumber
            });

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }
        catch (Exception ex)
        {
            _logger.OperationFailed("CREATE_ORDER", stopwatch, ex, correlationId, new {
                customerId = createOrderDto.CustomerId,
                endpoint = "POST /api/orders"
            });
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
            _logger.Error($"Error updating status for order: {{OrderId}} - {ex.Message}", GetCorrelationId(), new { OrderId = id, Exception = ex });
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
            _logger.Error($"Error fetching orders for customer: {{CustomerId}} - {ex.Message}", GetCorrelationId(), new { CustomerId = customerId, Exception = ex });
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
            _logger.Error($"Error fetching paged orders for customer: {{CustomerId}} - {ex.Message}", GetCorrelationId(), new { CustomerId = customerId, Exception = ex });
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
            _logger.Error($"Error fetching orders with status: {{Status}} - {ex.Message}", GetCorrelationId(), new { Status = status, Exception = ex });
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
            _logger.Error($"Error deleting order: {{OrderId}} - {ex.Message}", GetCorrelationId(), new { OrderId = id, Exception = ex });
            return StatusCode(500, "An error occurred while deleting the order");
        }
    }
    
    /// <summary>
    /// Helper method to get correlation ID from context
    /// </summary>
    private string GetCorrelationId()
    {
        return HttpContext.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();
    }
    
    /// <summary>
    /// Helper method to get current user ID from JWT token
    /// </summary>
    private string? GetCurrentUserId()
    {
        return User.FindFirst("sub")?.Value;
    }
    
    /// <summary>
    /// Helper method to check if current user is admin
    /// </summary>
    private bool IsCurrentUserAdmin()
    {
        return User.HasClaim("roles", "admin");
    }
}
