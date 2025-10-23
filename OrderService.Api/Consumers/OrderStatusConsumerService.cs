using OrderService.Core.Models.Enums;
using OrderService.Core.Models.Events;
using OrderService.Core.Services;
using OrderService.Core.Services.Messaging;

namespace OrderService.Api.Consumers;

/// <summary>
/// Background service that consumes order status changed events from Order Processor Service
/// This replaces the separate OrderService.Worker process
/// </summary>
public class OrderStatusConsumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderStatusConsumerService> _logger;
    private readonly IMessageBrokerAdapter _brokerAdapter;
    private const string QueueName = "order-service.status-updates";
    private const string RoutingKey = "order.status.changed";

    public OrderStatusConsumerService(
        IServiceProvider serviceProvider,
        ILogger<OrderStatusConsumerService> logger,
        IMessageBrokerAdapter brokerAdapter)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _brokerAdapter = brokerAdapter;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var brokerType = _brokerAdapter.GetBrokerType();
            _logger.LogInformation(
                "Connecting to message broker (Type: {BrokerType}) for order status updates...", 
                brokerType);
            
            await _brokerAdapter.ConnectAsync(stoppingToken);
            
            _logger.LogInformation(
                "Subscribing to order status change events - Queue: {QueueName}, RoutingKey: {RoutingKey}, Broker: {BrokerType}", 
                QueueName, RoutingKey, brokerType);

            // Subscribe to order status changed events
            // This is broker-agnostic - works with RabbitMQ, Kafka, or Azure Service Bus
            await _brokerAdapter.SubscribeAsync(
                QueueName,
                new[] { RoutingKey },
                async (routingKey, message) =>
                {
                    await HandleOrderStatusChangedAsync(message, stoppingToken);
                },
                stoppingToken);

            _logger.LogInformation(
                "✅ Order Status Consumer started successfully (Broker: {BrokerType}, Queue: {QueueName})", 
                brokerType, QueueName);

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Order Status Consumer is stopping due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in Order Status Consumer (Broker: {BrokerType})", 
                _brokerAdapter.GetBrokerType());
            throw;
        }
    }

    private async Task HandleOrderStatusChangedAsync(string message, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<OrderStatusConsumerService>>();

        try
        {
            // Deserialize event
            var statusEvent = System.Text.Json.JsonSerializer.Deserialize<OrderStatusChangedEvent>(
                message,
                new System.Text.Json.JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });

            if (statusEvent == null)
            {
                logger.LogWarning("Received null or invalid order status changed event");
                return;
            }

            logger.LogInformation(
                "Processing order status change: OrderId={OrderId}, Status={PreviousStatus}->{NewStatus}, Reason={Reason} [CorrelationId: {CorrelationId}]",
                statusEvent.OrderId, 
                statusEvent.PreviousStatus, 
                statusEvent.NewStatus,
                statusEvent.Reason,
                statusEvent.CorrelationId);

            // Parse the new status
            if (!Enum.TryParse<OrderStatus>(statusEvent.NewStatus, true, out var newStatus))
            {
                logger.LogWarning("Invalid order status: {Status}", statusEvent.NewStatus);
                return;
            }

            // Update order status using the SAME service that the API uses
            await orderService.UpdateOrderStatusAsync(
                Guid.Parse(statusEvent.OrderId),
                new Core.Models.DTOs.UpdateOrderStatusDto
                {
                    Status = newStatus
                });

            logger.LogInformation(
                "✅ Successfully updated order {OrderId} to status {NewStatus} [CorrelationId: {CorrelationId}]",
                statusEvent.OrderId, 
                statusEvent.NewStatus,
                statusEvent.CorrelationId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling order status changed event: {Message}", message);
            // Don't rethrow - let the message broker handle retry logic
        }
    }

    public override void Dispose()
    {
        _logger.LogInformation("Shutting down Order Status Consumer");
        _brokerAdapter?.Dispose();
        base.Dispose();
    }
}
