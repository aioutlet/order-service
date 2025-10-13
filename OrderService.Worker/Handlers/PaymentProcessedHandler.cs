using OrderService.Core.Models.Enums;
using OrderService.Core.Services;
using OrderService.Worker.Events;

namespace OrderService.Worker.Handlers;

/// <summary>
/// Handler for payment processed events
/// </summary>
public class PaymentProcessedHandler : IEventHandler<PaymentProcessedEvent>
{
    private readonly IOrderService _orderService;
    private readonly ILogger<PaymentProcessedHandler> _logger;

    public PaymentProcessedHandler(
        IOrderService orderService,
        ILogger<PaymentProcessedHandler> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public async Task HandleAsync(PaymentProcessedEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Processing payment processed event for order: {OrderId} [CorrelationId: {CorrelationId}, Amount: {Amount} {Currency}]",
                @event.OrderId, @event.CorrelationId, @event.Amount, @event.Currency);

            // Update order status to Confirmed (payment successful)
            await _orderService.UpdateOrderStatusAsync(@event.OrderId, new Core.Models.DTOs.UpdateOrderStatusDto
            {
                Status = OrderStatus.Confirmed
            });

            _logger.LogInformation(
                "Updated order {OrderId} status to Confirmed (payment processed) [CorrelationId: {CorrelationId}]",
                @event.OrderId, @event.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payment processed event for order: {OrderId}", @event.OrderId);
            throw;
        }
    }
}
