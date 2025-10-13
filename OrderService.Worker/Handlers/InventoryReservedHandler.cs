using OrderService.Core.Models.Enums;
using OrderService.Core.Services;
using OrderService.Worker.Events;

namespace OrderService.Worker.Handlers;

/// <summary>
/// Handler for inventory reserved events
/// </summary>
public class InventoryReservedHandler : IEventHandler<InventoryReservedEvent>
{
    private readonly IOrderService _orderService;
    private readonly ILogger<InventoryReservedHandler> _logger;

    public InventoryReservedHandler(
        IOrderService orderService,
        ILogger<InventoryReservedHandler> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public async Task HandleAsync(InventoryReservedEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Processing inventory reserved event for order: {OrderId} [ReservationId: {ReservationId}]",
                @event.OrderId, @event.ReservationId);

            // Update order status to Processing (inventory reserved)
            await _orderService.UpdateOrderStatusAsync(@event.OrderId, new Core.Models.DTOs.UpdateOrderStatusDto
            {
                Status = OrderStatus.Processing
            });

            _logger.LogInformation(
                "Updated order {OrderId} status to Processing (inventory reserved)",
                @event.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling inventory reserved event for order: {OrderId}", @event.OrderId);
            throw;
        }
    }
}
