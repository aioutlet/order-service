namespace OrderService.Core.Models.Events;

/// <summary>
/// Event published when an order is deleted
/// </summary>
public class OrderDeletedEvent
{
    /// <summary>
    /// Order unique identifier
    /// </summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>
    /// Order number (user-friendly identifier)
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Customer unique identifier
    /// </summary>
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>
    /// When the order was deleted
    /// </summary>
    public DateTime DeletedAt { get; set; }

    /// <summary>
    /// Who deleted the order (user ID or system)
    /// </summary>
    public string DeletedBy { get; set; } = string.Empty;

    /// <summary>
    /// Optional reason for deletion
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Correlation ID for tracing
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;
}
