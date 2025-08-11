using Microsoft.Extensions.Options;
using OrderService.Configuration;
using OrderService.Models.DTOs;
using OrderService.Models.Entities;
using OrderService.Models.Enums;
using OrderService.Models.Events;
using OrderService.Repositories;
using OrderService.Services.Messaging;

namespace OrderService.Services;

/// <summary>
/// Service implementation for order business logic
/// </summary>
public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<OrderService> _logger;
    private readonly OrderServiceSettings _settings;
    private readonly IMessagePublisher _messagePublisher;
    private readonly MessageBrokerSettings _messageBrokerSettings;
    private readonly ICurrentUserService _currentUserService;

    public OrderService(
        IOrderRepository orderRepository, 
        ILogger<OrderService> logger,
        IOptions<OrderServiceSettings> settings,
        IMessagePublisher messagePublisher,
        IOptions<MessageBrokerSettings> messageBrokerSettings,
        ICurrentUserService currentUserService)
    {
        _orderRepository = orderRepository;
        _logger = logger;
        _settings = settings.Value;
        _messagePublisher = messagePublisher;
        _messageBrokerSettings = messageBrokerSettings.Value;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Get all orders
    /// </summary>
    public async Task<IEnumerable<OrderResponseDto>> GetAllOrdersAsync()
    {
        _logger.LogInformation("Fetching all orders");
        
        var orders = await _orderRepository.GetAllOrdersAsync();
        var orderDtos = orders.Select(MapToOrderResponseDto).ToList();
        
        _logger.LogInformation("Retrieved {Count} orders", orderDtos.Count);
        return orderDtos;
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    public async Task<OrderResponseDto?> GetOrderByIdAsync(Guid id)
    {
        _logger.LogInformation("Fetching order with ID: {OrderId}", id);
        
        var order = await _orderRepository.GetOrderByIdAsync(id);
        if (order == null)
        {
            _logger.LogWarning("Order with ID {OrderId} not found", id);
            return null;
        }

        _logger.LogInformation("Retrieved order: {OrderNumber}", order.OrderNumber);
        return MapToOrderResponseDto(order);
    }

    /// <summary>
    /// Get orders by customer ID
    /// </summary>
    public async Task<IEnumerable<OrderResponseDto>> GetOrdersByCustomerIdAsync(string customerId)
    {
        _logger.LogInformation("Fetching orders for customer: {CustomerId}", customerId);
        
        var orders = await _orderRepository.GetOrdersByCustomerIdAsync(customerId);
        var orderDtos = orders.Select(MapToOrderResponseDto).ToList();
        
        _logger.LogInformation("Retrieved {Count} orders for customer {CustomerId}", orderDtos.Count, customerId);
        return orderDtos;
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
        
        _logger.LogInformation("Creating new order for customer: {CustomerId} by user: {CreatedBy} [CorrelationId: {CorrelationId}]", 
            createOrderDto.CustomerId, currentUser, currentCorrelationId);

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
            Currency = _settings.DefaultCurrency,
            CreatedBy = currentUser,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add order items and calculate totals
        decimal subtotal = 0;
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

        // Calculate order totals using configuration
        order.Subtotal = subtotal;
        order.TaxAmount = subtotal * (decimal)_settings.TaxRate;
        order.ShippingCost = subtotal > (decimal)_settings.FreeShippingThreshold ? 0 : (decimal)_settings.DefaultShippingCost;
        order.DiscountAmount = 0;
        order.TotalAmount = order.Subtotal + order.TaxAmount + order.ShippingCost - order.DiscountAmount;

        // Save to database
        var createdOrder = await _orderRepository.CreateOrderAsync(order);
        
        _logger.LogInformation("Created order: {OrderNumber} with total: {Total:C}", 
            createdOrder.OrderNumber, createdOrder.TotalAmount);

        // Publish order created event for loose coupling
        try
        {
            var orderCreatedEvent = MapToOrderCreatedEvent(createdOrder, currentCorrelationId);
            await _messagePublisher.PublishAsync(_messageBrokerSettings.Topics.OrderCreated, orderCreatedEvent);
            
            _logger.LogInformation("Published OrderCreated event for order: {OrderNumber} [CorrelationId: {CorrelationId}]", 
                createdOrder.OrderNumber, currentCorrelationId);
        }
        catch (Exception ex)
        {
            // Log error but don't fail the order creation
            _logger.LogError(ex, "Failed to publish OrderCreated event for order: {OrderNumber}. Order was created successfully.", 
                createdOrder.OrderNumber);
        }

        return MapToOrderResponseDto(createdOrder);
    }

    /// <summary>
    /// Update order status
    /// </summary>
    public async Task<OrderResponseDto?> UpdateOrderStatusAsync(Guid id, UpdateOrderStatusDto updateStatusDto)
    {
        var currentUser = _currentUserService.GetUserName() ?? _currentUserService.GetUserId() ?? "System";
        
        _logger.LogInformation("Updating status for order {OrderId} to {Status} by user: {UpdatedBy}", 
            id, updateStatusDto.Status, currentUser);

        var order = await _orderRepository.GetOrderByIdAsync(id);
        if (order == null)
        {
            _logger.LogWarning("Order with ID {OrderId} not found", id);
            return null;
        }

        order.Status = updateStatusDto.Status;
        order.UpdatedBy = currentUser;
        order.UpdatedAt = DateTime.UtcNow;

        var updatedOrder = await _orderRepository.UpdateOrderAsync(order);
        
        _logger.LogInformation("Updated order {OrderNumber} status to {Status}", 
            updatedOrder.OrderNumber, updateStatusDto.Status);

        return MapToOrderResponseDto(updatedOrder);
    }

    /// <summary>
    /// Get orders by status
    /// </summary>
    public async Task<IEnumerable<OrderResponseDto>> GetOrdersByStatusAsync(OrderStatus status)
    {
        _logger.LogInformation("Fetching orders with status: {Status}", status);
        
        var orders = await _orderRepository.GetOrdersByStatusAsync(status);
        var orderDtos = orders.Select(MapToOrderResponseDto).ToList();
        
        _logger.LogInformation("Retrieved {Count} orders with status {Status}", orderDtos.Count, status);
        return orderDtos;
    }

    /// <summary>
    /// Delete an order
    /// </summary>
    public async Task<bool> DeleteOrderAsync(Guid id)
    {
        _logger.LogInformation("Deleting order: {OrderId}", id);
        
        var result = await _orderRepository.DeleteOrderAsync(id);
        
        if (result)
        {
            _logger.LogInformation("Successfully deleted order: {OrderId}", id);
        }
        else
        {
            _logger.LogWarning("Failed to delete order: {OrderId}", id);
        }
        
        return result;
    }

    /// <summary>
    /// Get orders with pagination and filtering
    /// </summary>
    public async Task<PagedResponseDto<OrderResponseDto>> GetOrdersPagedAsync(OrderQueryDto query)
    {
        _logger.LogInformation("Fetching paged orders - Page: {Page}, PageSize: {PageSize}, Status: {Status}, CustomerId: {CustomerId}", 
            query.Page, query.PageSize, query.Status, query.CustomerId);

        var (orders, totalCount) = await _orderRepository.GetOrdersPagedAsync(query);
        var orderDtos = orders.Select(MapToOrderResponseDto).ToList();

        _logger.LogInformation("Retrieved page {Page} with {Count} orders out of {Total} total", 
            query.Page, orderDtos.Count, totalCount);

        return PagedResponseDto<OrderResponseDto>.Create(orderDtos, query.Page, query.PageSize, totalCount);
    }

    /// <summary>
    /// Get orders by customer ID with pagination
    /// </summary>
    public async Task<PagedResponseDto<OrderResponseDto>> GetOrdersByCustomerIdPagedAsync(string customerId, PagedRequestDto pageRequest)
    {
        _logger.LogInformation("Fetching paged orders for customer: {CustomerId} - Page: {Page}, PageSize: {PageSize}", 
            customerId, pageRequest.Page, pageRequest.PageSize);

        var (orders, totalCount) = await _orderRepository.GetOrdersByCustomerIdPagedAsync(customerId, pageRequest);
        var orderDtos = orders.Select(MapToOrderResponseDto).ToList();

        _logger.LogInformation("Retrieved page {Page} with {Count} orders for customer {CustomerId} out of {Total} total", 
            pageRequest.Page, orderDtos.Count, customerId, totalCount);

        return PagedResponseDto<OrderResponseDto>.Create(orderDtos, pageRequest.Page, pageRequest.PageSize, totalCount);
    }

    /// <summary>
    /// Generate order number using configuration
    /// </summary>
    private string GenerateOrderNumber()
    {
        return $"{_settings.OrderNumberPrefix}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
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
