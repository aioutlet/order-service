using OrderService.Core.Models.Enums;
using OrderService.Core.Services;
using OrderService.Worker.Events;

namespace OrderService.Worker.Handlers;

/// <summary>
/// Handler for shipping prepared events
/// </summary>
public class ShippingPreparedHandler : IEventHandler<ShippingPreparedEvent>
{
    private readonly IOrderService _orderService;
    private readonly ILogger<ShippingPreparedHandler> _logger;

    public ShippingPreparedHandler(
        IOrderService orderService,
        ILogger<ShippingPreparedHandler> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public async Task HandleAsync(ShippingPreparedEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Processing shipping prepared event for order: {OrderId} [ShippingId: {ShippingId}, TrackingNumber: {TrackingNumber}]",
                @event.OrderId, @event.ShippingId, @event.TrackingNumber);

            // Update order status to Shipped
            await _orderService.UpdateOrderStatusAsync(@event.OrderId, new Core.Models.DTOs.UpdateOrderStatusDto
            {
                Status = OrderStatus.Shipped
            });

            _logger.LogInformation(
                "Updated order {OrderId} status to Shipped (tracking: {TrackingNumber})",
                @event.OrderId, @event.TrackingNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling shipping prepared event for order: {OrderId}", @event.OrderId);
            throw;
        }
    }
}
