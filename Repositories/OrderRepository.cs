using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models.Entities;
using OrderService.Models.Enums;

namespace OrderService.Repositories;

/// <summary>
/// Repository implementation for Order entity operations
/// </summary>
public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _context;
    private readonly ILogger<OrderRepository> _logger;

    public OrderRepository(OrderDbContext context, ILogger<OrderRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all orders with their items
    /// </summary>
    public async Task<IEnumerable<Order>> GetAllOrdersAsync()
    {
        _logger.LogDebug("Fetching all orders from database");
        
        return await _context.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get order by ID with items included
    /// </summary>
    public async Task<Order?> GetOrderByIdAsync(Guid id)
    {
        _logger.LogDebug("Fetching order with ID: {OrderId}", id);
        
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    /// <summary>
    /// Get orders by customer ID
    /// </summary>
    public async Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(string customerId)
    {
        _logger.LogDebug("Fetching orders for customer: {CustomerId}", customerId);
        
        return await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Create a new order
    /// </summary>
    public async Task<Order> CreateOrderAsync(Order order)
    {
        _logger.LogDebug("Creating new order for customer: {CustomerId}", order.CustomerId);
        
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Created order: {OrderNumber}", order.OrderNumber);
        return order;
    }

    /// <summary>
    /// Update an existing order
    /// </summary>
    public async Task<Order> UpdateOrderAsync(Order order)
    {
        _logger.LogDebug("Updating order: {OrderId}", order.Id);
        
        order.UpdatedAt = DateTime.UtcNow;
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Updated order: {OrderNumber}", order.OrderNumber);
        return order;
    }

    /// <summary>
    /// Delete an order
    /// </summary>
    public async Task<bool> DeleteOrderAsync(Guid id)
    {
        _logger.LogDebug("Deleting order: {OrderId}", id);
        
        var order = await _context.Orders.FindAsync(id);
        if (order == null)
        {
            _logger.LogWarning("Order not found for deletion: {OrderId}", id);
            return false;
        }

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Deleted order: {OrderNumber}", order.OrderNumber);
        return true;
    }

    /// <summary>
    /// Check if order exists
    /// </summary>
    public async Task<bool> OrderExistsAsync(Guid id)
    {
        return await _context.Orders.AnyAsync(o => o.Id == id);
    }

    /// <summary>
    /// Get orders by status
    /// </summary>
    public async Task<IEnumerable<Order>> GetOrdersByStatusAsync(OrderStatus status)
    {
        _logger.LogDebug("Fetching orders with status: {Status}", status);
        
        return await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.Status == status)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }
}
