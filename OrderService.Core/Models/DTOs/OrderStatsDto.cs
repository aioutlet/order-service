namespace OrderService.Core.Models.DTOs;

/// <summary>
/// DTO for order statistics (admin dashboard)
/// </summary>
public class OrderStatsDto
{
    /// <summary>
    /// Total number of orders
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// Number of pending orders
    /// </summary>
    public int Pending { get; set; }

    /// <summary>
    /// Number of completed orders
    /// </summary>
    public int Completed { get; set; }

    /// <summary>
    /// Number of orders this month
    /// </summary>
    public int NewThisMonth { get; set; }

    /// <summary>
    /// Growth percentage compared to last month
    /// </summary>
    public decimal Growth { get; set; }

    /// <summary>
    /// Total revenue across all orders
    /// </summary>
    public decimal Revenue { get; set; }

    /// <summary>
    /// Recent orders (if requested)
    /// </summary>
    public IEnumerable<RecentOrderDto>? RecentOrders { get; set; }
}

/// <summary>
/// DTO for recent order summary
/// </summary>
public class RecentOrderDto
{
    /// <summary>
    /// Order ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Order number
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Customer ID
    /// </summary>
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>
    /// Customer name
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Order status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Total amount
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Number of items in order
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
