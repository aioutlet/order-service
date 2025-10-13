using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using OrderService.Core.Data;
using OrderService.Core.Models.Entities;
using OrderService.Core.Models.Enums;
using OrderService.Core.Models.DTOs;

namespace OrderService.Core.Repositories;

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
    /// Get all orders with their items (addresses are automatically included as owned types)
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
    /// Get order by ID with items included (addresses are automatically included as owned types)
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

    /// <summary>
    /// Get orders with pagination and filtering
    /// </summary>
    public async Task<(IEnumerable<Order> Orders, int TotalCount)> GetOrdersPagedAsync(OrderQueryDto query)
    {
        _logger.LogDebug("Fetching paged orders - Page: {Page}, PageSize: {PageSize}", query.Page, query.PageSize);

        var queryable = _context.Orders.Include(o => o.Items).AsQueryable();

        // Apply filters
        if (query.Status.HasValue)
        {
            queryable = queryable.Where(o => o.Status == query.Status.Value);
        }

        if (!string.IsNullOrEmpty(query.CustomerId))
        {
            queryable = queryable.Where(o => o.CustomerId == query.CustomerId);
        }

        if (query.OrderDateFrom.HasValue)
        {
            queryable = queryable.Where(o => o.CreatedAt >= query.OrderDateFrom.Value);
        }

        if (query.OrderDateTo.HasValue)
        {
            var endDate = query.OrderDateTo.Value.Date.AddDays(1); // Include entire day
            queryable = queryable.Where(o => o.CreatedAt < endDate);
        }

        // Apply sorting
        queryable = query.SortBy switch
        {
            OrderSortBy.OrderDateAsc => queryable.OrderBy(o => o.CreatedAt),
            OrderSortBy.OrderDateDesc => queryable.OrderByDescending(o => o.CreatedAt),
            OrderSortBy.TotalAmountAsc => queryable.OrderBy(o => o.TotalAmount),
            OrderSortBy.TotalAmountDesc => queryable.OrderByDescending(o => o.TotalAmount),
            OrderSortBy.StatusAsc => queryable.OrderBy(o => o.Status),
            OrderSortBy.StatusDesc => queryable.OrderByDescending(o => o.Status),
            _ => queryable.OrderByDescending(o => o.CreatedAt)
        };

        // Get total count before pagination
        var totalCount = await queryable.CountAsync();

        // Apply pagination
        var orders = await queryable
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync();

        _logger.LogDebug("Retrieved {Count} orders out of {Total} total", orders.Count, totalCount);

        return (orders, totalCount);
    }

    /// <summary>
    /// Get orders by customer ID with pagination
    /// </summary>
    public async Task<(IEnumerable<Order> Orders, int TotalCount)> GetOrdersByCustomerIdPagedAsync(string customerId, PagedRequestDto pageRequest)
    {
        _logger.LogDebug("Fetching paged orders for customer: {CustomerId} - Page: {Page}, PageSize: {PageSize}", 
            customerId, pageRequest.Page, pageRequest.PageSize);

        var queryable = _context.Orders
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt);

        // Get total count before pagination
        var totalCount = await queryable.CountAsync();

        // Apply pagination
        var orders = await queryable
            .Skip(pageRequest.Skip)
            .Take(pageRequest.PageSize)
            .ToListAsync();

        _logger.LogDebug("Retrieved {Count} orders for customer {CustomerId} out of {Total} total", 
            orders.Count, customerId, totalCount);

        return (orders, totalCount);
    }
}
