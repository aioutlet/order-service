using OrderService.Models.Entities;

namespace OrderService.Repositories;

/// <summary>
/// Repository interface for Order entity operations
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// Get all orders with their items
    /// </summary>
    Task<IEnumerable<Order>> GetAllOrdersAsync();

    /// <summary>
    /// Get order by ID with items included
    /// </summary>
    Task<Order?> GetOrderByIdAsync(Guid id);

    /// <summary>
    /// Get orders by customer ID
    /// </summary>
    Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(string customerId);

    /// <summary>
    /// Create a new order
    /// </summary>
    Task<Order> CreateOrderAsync(Order order);

    /// <summary>
    /// Update an existing order
    /// </summary>
    Task<Order> UpdateOrderAsync(Order order);

    /// <summary>
    /// Delete an order
    /// </summary>
    Task<bool> DeleteOrderAsync(Guid id);

    /// <summary>
    /// Check if order exists
    /// </summary>
    Task<bool> OrderExistsAsync(Guid id);

    /// <summary>
    /// Get orders by status
    /// </summary>
    Task<IEnumerable<Order>> GetOrdersByStatusAsync(Models.Enums.OrderStatus status);
}
