namespace OrderService.Worker.Events;

/// <summary>
/// Event received when payment is processed
/// </summary>
public class PaymentProcessedEvent
{
    public Guid OrderId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string PaymentId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}
