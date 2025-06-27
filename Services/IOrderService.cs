using OrderService.Models.DTOs;
using OrderService.Models.Enums;

namespace OrderService.Services;

/// <summary>
/// Service interface for order business logic
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Get all orders
    /// </summary>
    Task<IEnumerable<OrderResponseDto>> GetAllOrdersAsync();

    /// <summary>
    /// Get order by ID
    /// </summary>
    Task<OrderResponseDto?> GetOrderByIdAsync(Guid id);

    /// <summary>
    /// Get orders by customer ID
    /// </summary>
    Task<IEnumerable<OrderResponseDto>> GetOrdersByCustomerIdAsync(string customerId);

    /// <summary>
    /// Create a new order
    /// </summary>
    Task<OrderResponseDto> CreateOrderAsync(CreateOrderDto createOrderDto);

    /// <summary>
    /// Update order status
    /// </summary>
    Task<OrderResponseDto?> UpdateOrderStatusAsync(Guid id, UpdateOrderStatusDto updateStatusDto);

    /// <summary>
    /// Get orders by status
    /// </summary>
    Task<IEnumerable<OrderResponseDto>> GetOrdersByStatusAsync(OrderStatus status);

    /// <summary>
    /// Delete an order
    /// </summary>
    Task<bool> DeleteOrderAsync(Guid id);

    /// <summary>
    /// Get orders with pagination and filtering
    /// </summary>
    Task<PagedResponseDto<OrderResponseDto>> GetOrdersPagedAsync(OrderQueryDto query);

    /// <summary>
    /// Get orders by customer ID with pagination
    /// </summary>
    Task<PagedResponseDto<OrderResponseDto>> GetOrdersByCustomerIdPagedAsync(string customerId, PagedRequestDto pageRequest);
}
