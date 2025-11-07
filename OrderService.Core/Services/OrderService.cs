using Microsoft.Extensions.Options;
using OrderService.Core.Configuration;
using OrderService.Core.Models.DTOs;
using OrderService.Core.Models.Entities;
using OrderService.Core.Models.Enums;
using OrderService.Core.Models.Events;
using OrderService.Core.Repositories;
using OrderService.Core.Services.Messaging;
using OrderService.Core.Observability.Logging;
using System.Diagnostics;

namespace OrderService.Core.Services;

/// <summary>
/// Service implementation for order business logic with enhanced logging and tracing
/// </summary>
public class OrderService : IOrderService
{
    // Business constants - TODO: Move to database/Admin UI in future
    private const string DEFAULT_CURRENCY = "USD";
    private const decimal TAX_RATE = 0.08m;
    private const decimal FREE_SHIPPING_THRESHOLD = 100.0m;
    private const decimal DEFAULT_SHIPPING_COST = 10.0m;
    private const string ORDER_NUMBER_PREFIX = "ORD";

    private readonly IOrderRepository _orderRepository;
    private readonly EnhancedLogger _logger;
    private readonly MessageBrokerServiceClient _messageBrokerClient;
    private readonly MessageBrokerSettings _messageBrokerSettings;
    private readonly ICurrentUserService _currentUserService;

    public OrderService(
        IOrderRepository orderRepository, 
        EnhancedLogger logger,
        MessageBrokerServiceClient messageBrokerClient,
        IOptions<MessageBrokerSettings> messageBrokerSettings,
        ICurrentUserService currentUserService)
    {
        _orderRepository = orderRepository;
        _logger = logger;
        _messageBrokerClient = messageBrokerClient;
        _messageBrokerSettings = messageBrokerSettings.Value;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Get all orders
    /// </summary>
    public async Task<IEnumerable<OrderResponseDto>> GetAllOrdersAsync()
    {
        var stopwatch = _logger.OperationStart("GET_ALL_ORDERS", null, new {
            operation = "GET_ALL_ORDERS"
        });
        
        try
        {
            var orders = await _orderRepository.GetAllOrdersAsync();
            var orderDtos = orders.Select(MapToOrderResponseDto).ToList();
            
            _logger.OperationComplete("GET_ALL_ORDERS", stopwatch, null, new {
                orderCount = orderDtos.Count
            });
            
            return orderDtos;
        }
        catch (Exception ex)
        {
            _logger.OperationFailed("GET_ALL_ORDERS", stopwatch, ex, null);
            throw;
        }
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    public async Task<OrderResponseDto?> GetOrderByIdAsync(Guid id)
    {
        var stopwatch = _logger.OperationStart("GET_ORDER_BY_ID", null, new {
            operation = "GET_ORDER_BY_ID",
            orderId = id
        });
        
        try
        {
            var order = await _orderRepository.GetOrderByIdAsync(id);
            if (order == null)
            {
                _logger.Warn($"Order with ID {id} not found", null, new {
                    orderId = id,
                    operation = "GET_ORDER_BY_ID"
                });
                return null;
            }

            _logger.OperationComplete("GET_ORDER_BY_ID", stopwatch, null, new {
                orderId = id,
                orderNumber = order.OrderNumber
            });
            
            return MapToOrderResponseDto(order);
        }
        catch (Exception ex)
        {
            _logger.OperationFailed("GET_ORDER_BY_ID", stopwatch, ex, null, new {
                orderId = id
            });
            throw;
        }
    }

    /// <summary>
    /// Get orders by customer ID
    /// </summary>
    public async Task<IEnumerable<OrderResponseDto>> GetOrdersByCustomerIdAsync(string customerId)
    {
        var stopwatch = _logger.OperationStart("GET_ORDERS_BY_CUSTOMER", null, new {
            operation = "GET_ORDERS_BY_CUSTOMER",
            customerId = customerId
        });
        
        try
        {
            var orders = await _orderRepository.GetOrdersByCustomerIdAsync(customerId);
            var orderDtos = orders.Select(MapToOrderResponseDto).ToList();
            
            _logger.OperationComplete("GET_ORDERS_BY_CUSTOMER", stopwatch, null, new {
                customerId = customerId,
                orderCount = orderDtos.Count
            });
            
            return orderDtos;
        }
        catch (Exception ex)
        {
            _logger.OperationFailed("GET_ORDERS_BY_CUSTOMER", stopwatch, ex, null, new {
                customerId = customerId
            });
            throw;
        }
    }

    /// <summary>
    /// Create a new order
    /// </summary>
    public async Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto createOrderDto, string correlationId = "")
    {
        // Use provided correlation ID or generate new one
        var currentCorrelationId = !string.IsNullOrEmpty(correlationId) ? correlationId : Guid.NewGuid().ToString();
        
        // Get current user info
        var currentUser = _currentUserService.GetUserName() ?? _currentUserService.GetUserId() ?? "System";
        
        var stopwatch = _logger.OperationStart("CREATE_ORDER", currentCorrelationId, new {
            operation = "CREATE_ORDER",
            customerId = createOrderDto.CustomerId,
            orderItemsCount = createOrderDto.Items?.Count ?? 0,
            createdBy = currentUser
        });

        try
        {
            // Generate order number using configuration
            var orderNumber = GenerateOrderNumber();

            // Create order entity
            var order = new Order
            {
                CustomerId = createOrderDto.CustomerId,
                OrderNumber = orderNumber,
                Status = OrderStatus.Created,
                PaymentStatus = PaymentStatus.Pending,
                ShippingStatus = ShippingStatus.NotShipped,
                Currency = DEFAULT_CURRENCY,
                CreatedBy = currentUser,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Add order items and calculate totals
            decimal subtotal = 0;
            if (createOrderDto.Items != null)
            {
                foreach (var itemDto in createOrderDto.Items)
            {
                var orderItem = new OrderItem
                {
                    ProductId = itemDto.ProductId,
                    ProductName = itemDto.ProductName,
                    UnitPrice = itemDto.UnitPrice,
                    Quantity = itemDto.Quantity,
                    TotalPrice = itemDto.UnitPrice * itemDto.Quantity,
                    DiscountAmount = 0,
                    TaxAmount = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                order.Items.Add(orderItem);
                subtotal += orderItem.TotalPrice;
            }
            }

            // Set addresses
            order.ShippingAddress = new Address
            {
                AddressLine1 = createOrderDto.ShippingAddress.AddressLine1,
                AddressLine2 = createOrderDto.ShippingAddress.AddressLine2,
                City = createOrderDto.ShippingAddress.City,
                State = createOrderDto.ShippingAddress.State,
                ZipCode = createOrderDto.ShippingAddress.ZipCode,
                Country = createOrderDto.ShippingAddress.Country
            };

            order.BillingAddress = new Address
            {
                AddressLine1 = createOrderDto.BillingAddress.AddressLine1,
                AddressLine2 = createOrderDto.BillingAddress.AddressLine2,
                City = createOrderDto.BillingAddress.City,
                State = createOrderDto.BillingAddress.State,
                ZipCode = createOrderDto.BillingAddress.ZipCode,
                Country = createOrderDto.BillingAddress.Country
            };

            // Calculate order totals
            order.Subtotal = subtotal;
            order.TaxAmount = subtotal * TAX_RATE;
            order.ShippingCost = subtotal > FREE_SHIPPING_THRESHOLD ? 0 : DEFAULT_SHIPPING_COST;
            order.DiscountAmount = 0;
            order.TotalAmount = order.Subtotal + order.TaxAmount + order.ShippingCost - order.DiscountAmount;

            // Save to database
            var createdOrder = await _orderRepository.CreateOrderAsync(order);
            
            _logger.OperationComplete("CREATE_ORDER", stopwatch, currentCorrelationId, new {
                orderId = createdOrder.Id,
                orderNumber = createdOrder.OrderNumber,
                totalAmount = createdOrder.TotalAmount,
                itemCount = createdOrder.Items.Count
            });

            _logger.Business("ORDER_CREATED", currentCorrelationId, new {
                orderId = createdOrder.Id,
                orderNumber = createdOrder.OrderNumber,
                customerId = createdOrder.CustomerId,
                totalAmount = createdOrder.TotalAmount
            });

            // Publish order created event for loose coupling
            try
            {
                var orderCreatedEvent = MapToOrderCreatedEvent(createdOrder, currentCorrelationId);
                await _messageBrokerClient.PublishEventAsync(
                    "aioutlet.events", 
                    _messageBrokerSettings.Topics.OrderCreated, 
                    orderCreatedEvent);
                
                _logger.Info("Published OrderCreated event", currentCorrelationId, new {
                    orderNumber = createdOrder.OrderNumber,
                    eventType = "ORDER_CREATED_EVENT_PUBLISHED"
                });
            }
            catch (Exception eventEx)
            {
                // Log error but don't fail the order creation
                _logger.Error("Failed to publish OrderCreated event", currentCorrelationId, new {
                    orderNumber = createdOrder.OrderNumber,
                    error = eventEx
                });
            }

            return MapToOrderResponseDto(createdOrder);
        }
        catch (Exception ex)
        {
            _logger.OperationFailed("CREATE_ORDER", stopwatch, ex, currentCorrelationId, new {
                customerId = createOrderDto.CustomerId
            });
            throw;
        }
    }

    /// <summary>
    /// Update order status
    /// </summary>
    public async Task<OrderResponseDto?> UpdateOrderStatusAsync(Guid id, UpdateOrderStatusDto updateStatusDto)
    {
        var currentUser = _currentUserService.GetUserName() ?? _currentUserService.GetUserId() ?? "System";
        var stopwatch = _logger.OperationStart("UPDATE_ORDER_STATUS", null, new {
            operation = "UPDATE_ORDER_STATUS",
            orderId = id,
            newStatus = updateStatusDto.Status,
            updatedBy = currentUser
        });

        try
        {
            var order = await _orderRepository.GetOrderByIdAsync(id);
            if (order == null)
            {
                _logger.Warn($"Order with ID {id} not found", null, new {
                    orderId = id,
                    operation = "UPDATE_ORDER_STATUS"
                });
                return null;
            }

            var oldStatus = order.Status;
            order.Status = updateStatusDto.Status;
            order.UpdatedBy = currentUser;
            order.UpdatedAt = DateTime.UtcNow;

            var updatedOrder = await _orderRepository.UpdateOrderAsync(order);

            _logger.OperationComplete("UPDATE_ORDER_STATUS", stopwatch, null, new {
                orderId = id,
                orderNumber = updatedOrder.OrderNumber,
                oldStatus = oldStatus,
                newStatus = updatedOrder.Status,
                updatedBy = currentUser
            });

            _logger.Business("ORDER_STATUS_UPDATED", null, new {
                orderId = id,
                orderNumber = updatedOrder.OrderNumber,
                oldStatus = oldStatus,
                newStatus = updatedOrder.Status
            });

            return MapToOrderResponseDto(updatedOrder);
        }
        catch (Exception ex)
        {
            _logger.OperationFailed("UPDATE_ORDER_STATUS", stopwatch, ex, null, new {
                orderId = id,
                newStatus = updateStatusDto.Status
            });
            throw;
        }
    }

    /// <summary>
    /// Get orders by status
    /// </summary>
    public async Task<IEnumerable<OrderResponseDto>> GetOrdersByStatusAsync(OrderStatus status)
    {
        var stopwatch = _logger.OperationStart("GET_ORDERS_BY_STATUS", null, new {
            operation = "GET_ORDERS_BY_STATUS",
            status = status
        });
        
        try
        {
            var orders = await _orderRepository.GetOrdersByStatusAsync(status);
            var orderDtos = orders.Select(MapToOrderResponseDto).ToList();
            
            _logger.OperationComplete("GET_ORDERS_BY_STATUS", stopwatch, null, new {
                status = status,
                orderCount = orderDtos.Count
            });
            
            return orderDtos;
        }
        catch (Exception ex)
        {
            _logger.OperationFailed("GET_ORDERS_BY_STATUS", stopwatch, ex, null, new {
                status = status
            });
            throw;
        }
    }

    /// <summary>
    /// Delete an order
    /// </summary>
    public async Task<bool> DeleteOrderAsync(Guid id)
    {
        var stopwatch = _logger.OperationStart("DELETE_ORDER", null, new {
            operation = "DELETE_ORDER",
            orderId = id
        });
        
        try
        {
            var result = await _orderRepository.DeleteOrderAsync(id);
            
            if (result)
            {
                _logger.OperationComplete("DELETE_ORDER", stopwatch, null, new {
                    orderId = id,
                    deleted = true
                });
                
                _logger.Business("ORDER_DELETED", null, new {
                    orderId = id
                });
            }
            else
            {
                _logger.Warn($"Failed to delete order: {id}", null, new {
                    orderId = id,
                    operation = "DELETE_ORDER"
                });
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.OperationFailed("DELETE_ORDER", stopwatch, ex, null, new {
                orderId = id
            });
            throw;
        }
    }

    /// <summary>
    /// Get orders with pagination and filtering
    /// </summary>
    public async Task<PagedResponseDto<OrderResponseDto>> GetOrdersPagedAsync(OrderQueryDto query)
    {
        var stopwatch = _logger.OperationStart("GET_ORDERS_PAGED", null, new {
            operation = "GET_ORDERS_PAGED",
            page = query.Page,
            pageSize = query.PageSize,
            status = query.Status,
            customerId = query.CustomerId
        });
        
        try
        {
            var (orders, totalCount) = await _orderRepository.GetOrdersPagedAsync(query);
            var orderDtos = orders.Select(MapToOrderResponseDto).ToList();

            _logger.OperationComplete("GET_ORDERS_PAGED", stopwatch, null, new {
                page = query.Page,
                pageSize = query.PageSize,
                returnedCount = orderDtos.Count,
                totalCount = totalCount
            });

            return PagedResponseDto<OrderResponseDto>.Create(orderDtos, query.Page, query.PageSize, totalCount);
        }
        catch (Exception ex)
        {
            _logger.OperationFailed("GET_ORDERS_PAGED", stopwatch, ex, null, new {
                page = query.Page,
                pageSize = query.PageSize,
                status = query.Status,
                customerId = query.CustomerId
            });
            throw;
        }
    }

    /// <summary>
    /// Get orders by customer ID with pagination
    /// </summary>
    public async Task<PagedResponseDto<OrderResponseDto>> GetOrdersByCustomerIdPagedAsync(string customerId, PagedRequestDto pageRequest)
    {
        var stopwatch = _logger.OperationStart("GET_ORDERS_BY_CUSTOMER_PAGED", null, new {
            operation = "GET_ORDERS_BY_CUSTOMER_PAGED",
            customerId = customerId,
            page = pageRequest.Page,
            pageSize = pageRequest.PageSize
        });
        
        try
        {
            var (orders, totalCount) = await _orderRepository.GetOrdersByCustomerIdPagedAsync(customerId, pageRequest);
            var orderDtos = orders.Select(MapToOrderResponseDto).ToList();

            _logger.OperationComplete("GET_ORDERS_BY_CUSTOMER_PAGED", stopwatch, null, new {
                customerId = customerId,
                page = pageRequest.Page,
                pageSize = pageRequest.PageSize,
                returnedCount = orderDtos.Count,
                totalCount = totalCount
            });

            return PagedResponseDto<OrderResponseDto>.Create(orderDtos, pageRequest.Page, pageRequest.PageSize, totalCount);
        }
        catch (Exception ex)
        {
            _logger.OperationFailed("GET_ORDERS_BY_CUSTOMER_PAGED", stopwatch, ex, null, new {
                customerId = customerId,
                page = pageRequest.Page,
                pageSize = pageRequest.PageSize
            });
            throw;
        }
    }

    /// <summary>
    /// Generate order number using configuration
    /// </summary>
    private string GenerateOrderNumber()
    {
        return $"{ORDER_NUMBER_PREFIX}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }

    /// <summary>
    /// Map Order entity to OrderResponseDto
    /// </summary>
    private static OrderResponseDto MapToOrderResponseDto(Order order)
    {
        return new OrderResponseDto
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            PaymentStatus = order.PaymentStatus,
            ShippingStatus = order.ShippingStatus,
            Subtotal = order.Subtotal,
            TaxAmount = order.TaxAmount,
            ShippingCost = order.ShippingCost,
            DiscountAmount = order.DiscountAmount,
            TotalAmount = order.TotalAmount,
            Currency = order.Currency,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            CreatedBy = order.CreatedBy,
            UpdatedBy = order.UpdatedBy,
            Items = order.Items.Select(item => new OrderItemResponseDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                ProductSku = item.ProductSku,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                TotalPrice = item.TotalPrice,
                DiscountAmount = item.DiscountAmount,
                TaxAmount = item.TaxAmount,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            }).ToList(),
            ShippingAddress = new AddressDto
            {
                AddressLine1 = order.ShippingAddress.AddressLine1,
                AddressLine2 = order.ShippingAddress.AddressLine2,
                City = order.ShippingAddress.City,
                State = order.ShippingAddress.State,
                ZipCode = order.ShippingAddress.ZipCode,
                Country = order.ShippingAddress.Country
            },
            BillingAddress = new AddressDto
            {
                AddressLine1 = order.BillingAddress.AddressLine1,
                AddressLine2 = order.BillingAddress.AddressLine2,
                City = order.BillingAddress.City,
                State = order.BillingAddress.State,
                ZipCode = order.BillingAddress.ZipCode,
                Country = order.BillingAddress.Country
            }
        };
    }

    /// <summary>
    /// Get order statistics for admin dashboard
    /// </summary>
    public async Task<OrderStatsDto> GetStatsAsync(bool includeRecent = false, int recentLimit = 10)
    {
        var stopwatch = _logger.OperationStart("GET_ORDER_STATS", null, new {
            operation = "GET_ORDER_STATS",
            includeRecent,
            recentLimit
        });

        try
        {
            // Get all orders (consider adding caching for large datasets)
            var allOrders = await _orderRepository.GetAllOrdersAsync();

            // Calculate date boundaries
            var now = DateTime.UtcNow;
            var firstDayThisMonth = new DateTime(now.Year, now.Month, 1);
            var firstDayLastMonth = firstDayThisMonth.AddMonths(-1);
            var lastDayLastMonth = firstDayThisMonth.AddDays(-1);

            // Aggregate statistics
            var total = allOrders.Count();
            var pending = allOrders.Count(o => o.Status == OrderStatus.Created || o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Processing);
            var completed = allOrders.Count(o => o.Status == OrderStatus.Delivered);
            var newThisMonth = allOrders.Count(o => o.CreatedAt >= firstDayThisMonth);
            var newLastMonth = allOrders.Count(o => o.CreatedAt >= firstDayLastMonth && o.CreatedAt < firstDayThisMonth);
            var revenue = allOrders
                .Where(o => o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Shipped)
                .Sum(o => o.TotalAmount);

            // Calculate growth percentage
            var growth = newLastMonth > 0
                ? ((decimal)(newThisMonth - newLastMonth) / newLastMonth) * 100
                : newThisMonth > 0
                    ? 100
                    : 0;

            var stats = new OrderStatsDto
            {
                Total = total,
                Pending = pending,
                Completed = completed,
                NewThisMonth = newThisMonth,
                Growth = Math.Round(growth, 1),
                Revenue = Math.Round(revenue, 2)
            };

            // Add recent orders if requested
            if (includeRecent)
            {
                stats.RecentOrders = allOrders
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(recentLimit)
                    .Select(o => new RecentOrderDto
                    {
                        Id = o.Id.ToString(),
                        OrderNumber = o.OrderNumber,
                        CustomerId = o.CustomerId,
                        Status = o.Status.ToString(),
                        TotalAmount = o.TotalAmount,
                        CreatedAt = o.CreatedAt
                    })
                    .ToList();
            }

            _logger.OperationComplete("GET_ORDER_STATS", stopwatch, null, new {
                total,
                pending,
                completed,
                newThisMonth,
                revenue,
                includeRecent,
                recentCount = stats.RecentOrders?.Count() ?? 0
            });

            return stats;
        }
        catch (Exception ex)
        {
            _logger.OperationFailed("GET_ORDER_STATS", stopwatch, ex, null, new {
                includeRecent,
                recentLimit
            });
            throw;
        }
    }

    /// <summary>
    /// Map Order entity to OrderCreatedEvent for messaging
    /// </summary>
    private static OrderCreatedEvent MapToOrderCreatedEvent(Order order, string correlationId)
    {
        return new OrderCreatedEvent
        {
            OrderId = order.Id,
            CorrelationId = correlationId,
            CustomerId = order.CustomerId,
            OrderNumber = order.OrderNumber,
            TotalAmount = order.TotalAmount,
            Currency = order.Currency,
            CreatedAt = order.CreatedAt,
            Items = order.Items.Select(item => new OrderItemEvent
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice
            }).ToList(),
            ShippingAddress = new AddressEvent
            {
                AddressLine1 = order.ShippingAddress.AddressLine1,
                AddressLine2 = order.ShippingAddress.AddressLine2,
                City = order.ShippingAddress.City,
                State = order.ShippingAddress.State,
                ZipCode = order.ShippingAddress.ZipCode,
                Country = order.ShippingAddress.Country
            },
            BillingAddress = new AddressEvent
            {
                AddressLine1 = order.BillingAddress.AddressLine1,
                AddressLine2 = order.BillingAddress.AddressLine2,
                City = order.BillingAddress.City,
                State = order.BillingAddress.State,
                ZipCode = order.BillingAddress.ZipCode,
                Country = order.BillingAddress.Country
            }
        };
    }
}
