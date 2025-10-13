namespace OrderService.Worker.Events;

/// <summary>
/// Event received when an order is completed
/// </summary>
public class OrderCompletedEvent
{
    public Guid OrderId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }
}
