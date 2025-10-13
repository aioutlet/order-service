namespace OrderService.Worker.Events;

/// <summary>
/// Event received when inventory is reserved
/// </summary>
public class InventoryReservedEvent
{
    public Guid OrderId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string ReservationId { get; set; } = string.Empty;
    public DateTime ReservedAt { get; set; }
}
