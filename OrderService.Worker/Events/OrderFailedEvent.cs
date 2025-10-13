namespace OrderService.Worker.Events;

/// <summary>
/// Event received when an order fails
/// </summary>
public class OrderFailedEvent
{
    public Guid OrderId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime FailedAt { get; set; }
}
