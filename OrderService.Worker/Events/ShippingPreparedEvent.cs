namespace OrderService.Worker.Events;

/// <summary>
/// Event received when shipping is prepared
/// </summary>
public class ShippingPreparedEvent
{
    public Guid OrderId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string ShippingId { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
    public DateTime PreparedAt { get; set; }
}
