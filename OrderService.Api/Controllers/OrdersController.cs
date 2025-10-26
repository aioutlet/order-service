using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using OrderService.Core.Models.DTOs;
using OrderService.Core.Models.Enums;
using OrderService.Core.Models.Events;
using OrderService.Core.Services;
using OrderService.Core.Configuration;
using OrderService.Core.Observability.Logging;
using System.Diagnostics;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Require authentication for all endpoints
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly EnhancedLogger _logger;
    private readonly MessageBrokerServiceClient _messageBrokerClient;
    private readonly MessageBrokerSettings _messageBrokerSettings;

    public OrdersController(
        IOrderService orderService, 
        EnhancedLogger logger,
        MessageBrokerServiceClient messageBrokerClient,
        IOptions<MessageBrokerSettings> messageBrokerSettings)
    {
        _orderService = orderService;
        _logger = logger;
        _messageBrokerClient = messageBrokerClient;
        _messageBrokerSettings = messageBrokerSettings.Value;
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
                return StatusCode(403, new { message = "You can only view your own orders" });
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
                return StatusCode(403, new { 
                    message = "You can only create orders for yourself",
                    requestedCustomerId = createOrderDto.CustomerId,
                    authenticatedCustomerId = customerId
                });
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
        var correlationId = GetCorrelationId();
        var stopwatch = _logger.OperationStart("UPDATE_ORDER_STATUS", correlationId, new { 
            operation = "UPDATE_ORDER_STATUS",
            endpoint = "PUT /api/orders/{id}/status",
            orderId = id,
            newStatus = updateStatusDto.Status
        });

        try
        {
            var order = await _orderService.UpdateOrderStatusAsync(id, updateStatusDto);
            
            if (order == null)
            {
                _logger.Warn($"Order with ID {id} not found", correlationId, new {
                    orderId = id,
                    endpoint = "PUT /api/orders/{id}/status"
                });
                return NotFound($"Order with ID {id} not found");
            }

            // Publish appropriate event based on status change
            try
            {
                string routingKey = updateStatusDto.Status switch
                {
                    OrderStatus.Cancelled => _messageBrokerSettings.Topics.OrderCancelled,
                    OrderStatus.Shipped => _messageBrokerSettings.Topics.OrderShipped,
                    OrderStatus.Delivered => _messageBrokerSettings.Topics.OrderDelivered,
                    _ => _messageBrokerSettings.Topics.OrderUpdated
                };

                var orderEvent = new OrderStatusChangedEvent
                {
                    OrderId = order.Id.ToString(),
                    OrderNumber = order.OrderNumber,
                    CustomerId = order.CustomerId,
                    PreviousStatus = updateStatusDto.Status.ToString(), // Note: We don't have previous status, could enhance this
                    NewStatus = updateStatusDto.Status.ToString(),
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = GetCurrentUserId() ?? "system",
                    CorrelationId = correlationId
                };

                await _messageBrokerClient.PublishEventAsync(
                    "aioutlet.events",
                    routingKey,
                    orderEvent);

                _logger.Info($"Published order status changed event: {routingKey}", correlationId, new {
                    orderId = order.Id,
                    orderNumber = order.OrderNumber,
                    status = updateStatusDto.Status,
                    routingKey = routingKey
                });
            }
            catch (Exception eventEx)
            {
                _logger.Warn($"Failed to publish order status changed event, but order was updated: {eventEx.Message}", correlationId, new {
                    orderId = order.Id,
                    status = updateStatusDto.Status,
                    error = eventEx.Message
                });
            }

            _logger.OperationComplete("UPDATE_ORDER_STATUS", stopwatch, correlationId, new {
                orderId = id,
                orderNumber = order.OrderNumber,
                newStatus = order.Status,
                endpoint = "PUT /api/orders/{id}/status"
            });

            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.OperationFailed("UPDATE_ORDER_STATUS", stopwatch, ex, correlationId, new {
                orderId = id,
                newStatus = updateStatusDto.Status,
                endpoint = "PUT /api/orders/{id}/status"
            });
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
            var customerIdFromToken = GetCurrentUserId();
            var isAdmin = IsCurrentUserAdmin();
            
            // Customers can only view their own orders, admins can view any customer's orders
            if (!isAdmin && customerIdFromToken != customerId)
            {
                return StatusCode(403, new { message = "You can only view your own orders" });
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
            var customerIdFromToken = GetCurrentUserId();
            var isAdmin = IsCurrentUserAdmin();
            
            // Customers can only view their own orders, admins can view any customer's orders
            if (!isAdmin && customerIdFromToken != customerId)
            {
                return StatusCode(403, new { message = "You can only view your own orders" });
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
        var correlationId = GetCorrelationId();
        var stopwatch = _logger.OperationStart("DELETE_ORDER", correlationId, new { 
            operation = "DELETE_ORDER",
            endpoint = "DELETE /api/orders/{id}",
            orderId = id
        });

        try
        {
            // Get order details before deletion for event publishing
            var order = await _orderService.GetOrderByIdAsync(id);
            
            if (order == null)
            {
                _logger.Warn($"Order with ID {id} not found", correlationId, new {
                    orderId = id,
                    endpoint = "DELETE /api/orders/{id}"
                });
                return NotFound($"Order with ID {id} not found");
            }

            var result = await _orderService.DeleteOrderAsync(id);
            
            if (!result)
            {
                _logger.Warn($"Failed to delete order with ID {id}", correlationId, new {
                    orderId = id,
                    endpoint = "DELETE /api/orders/{id}"
                });
                return NotFound($"Order with ID {id} not found");
            }

            // Publish order deleted event
            try
            {
                var orderDeletedEvent = new OrderDeletedEvent
                {
                    OrderId = order.Id.ToString(),
                    OrderNumber = order.OrderNumber,
                    CustomerId = order.CustomerId,
                    DeletedAt = DateTime.UtcNow,
                    DeletedBy = GetCurrentUserId() ?? "system",
                    CorrelationId = correlationId
                };

                await _messageBrokerClient.PublishEventAsync(
                    "aioutlet.events",
                    _messageBrokerSettings.Topics.OrderDeleted,
                    orderDeletedEvent);

                _logger.Info("Published order deleted event", correlationId, new {
                    orderId = order.Id,
                    orderNumber = order.OrderNumber
                });
            }
            catch (Exception eventEx)
            {
                _logger.Warn($"Failed to publish order deleted event, but order was deleted: {eventEx.Message}", correlationId, new {
                    orderId = order.Id,
                    error = eventEx.Message
                });
            }

            _logger.OperationComplete("DELETE_ORDER", stopwatch, correlationId, new {
                orderId = id,
                orderNumber = order.OrderNumber,
                endpoint = "DELETE /api/orders/{id}"
            });

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.OperationFailed("DELETE_ORDER", stopwatch, ex, correlationId, new {
                orderId = id,
                endpoint = "DELETE /api/orders/{id}"
            });
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
        // Try 'id' claim first (from auth-service), then fall back to 'sub' (standard)
        return User.FindFirst("id")?.Value ?? User.FindFirst("sub")?.Value;
    }
    
    /// <summary>
    /// Helper method to check if current user is admin
    /// </summary>
    private bool IsCurrentUserAdmin()
    {
        return User.HasClaim("roles", "admin");
    }
}
