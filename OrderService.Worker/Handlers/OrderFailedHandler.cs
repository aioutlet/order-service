using OrderService.Core.Models.Enums;
using OrderService.Core.Services;
using OrderService.Worker.Events;

namespace OrderService.Worker.Handlers;

/// <summary>
/// Handler for order failed events
/// </summary>
public class OrderFailedHandler : IEventHandler<OrderFailedEvent>
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrderFailedHandler> _logger;

    public OrderFailedHandler(
        IOrderService orderService,
        ILogger<OrderFailedHandler> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public async Task HandleAsync(OrderFailedEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Processing order failed event for order: {OrderId} [Reason: {Reason}]",
                @event.OrderId, @event.Reason);

            // Update order status to Cancelled with reason
            await _orderService.UpdateOrderStatusAsync(@event.OrderId, new Core.Models.DTOs.UpdateOrderStatusDto
            {
                Status = OrderStatus.Cancelled
            });

            _logger.LogInformation(
                "Updated order {OrderId} status to Cancelled due to: {Reason}",
                @event.OrderId, @event.Reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling order failed event for order: {OrderId}", @event.OrderId);
            throw;
        }
    }
}
