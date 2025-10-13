using OrderService.Core.Models.Enums;
using OrderService.Core.Services;
using OrderService.Worker.Events;

namespace OrderService.Worker.Handlers;

/// <summary>
/// Handler for order completed events
/// </summary>
public class OrderCompletedHandler : IEventHandler<OrderCompletedEvent>
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrderCompletedHandler> _logger;

    public OrderCompletedHandler(
        IOrderService orderService,
        ILogger<OrderCompletedHandler> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public async Task HandleAsync(OrderCompletedEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Processing order completed event for order: {OrderId} [CorrelationId: {CorrelationId}]",
                @event.OrderId, @event.CorrelationId);

            // Update order status to Delivered
            await _orderService.UpdateOrderStatusAsync(@event.OrderId, new Core.Models.DTOs.UpdateOrderStatusDto
            {
                Status = OrderStatus.Delivered
            });

            _logger.LogInformation(
                "Updated order {OrderId} status to Delivered [CorrelationId: {CorrelationId}]",
                @event.OrderId, @event.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling order completed event for order: {OrderId}", @event.OrderId);
            throw;
        }
    }
}
