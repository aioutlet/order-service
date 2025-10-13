using Microsoft.Extensions.Logging;
using OrderService.Core.Models.Enums;
using OrderService.Core.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace OrderService.Core.Services.Messaging;

/// <summary>
/// Service to listen for order processing events from Order Processor Service
/// </summary>
public class OrderEventListener : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderEventListener> _logger;
    private readonly IRabbitMQConnectionService _connectionService;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _queueName = "order-service.order-processor-events";

    public OrderEventListener(
        IServiceProvider serviceProvider,
        ILogger<OrderEventListener> logger,
        IRabbitMQConnectionService connectionService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _connectionService = connectionService;
        _connection = _connectionService.GetConnection();
        _channel = _connection.CreateModel();
        
        SetupQueue();
    }

    private void SetupQueue()
    {
        // Declare exchange and queue
        _channel.ExchangeDeclare(exchange: "orders.exchange", type: ExchangeType.Topic, durable: true);
        _channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false);
        
        // Bind to order completion and failure events
        _channel.QueueBind(queue: _queueName, exchange: "orders.exchange", routingKey: "order.completed");
        _channel.QueueBind(queue: _queueName, exchange: "orders.exchange", routingKey: "order.failed");
        _channel.QueueBind(queue: _queueName, exchange: "orders.exchange", routingKey: "payment.processed");
        _channel.QueueBind(queue: _queueName, exchange: "orders.exchange", routingKey: "inventory.reserved");
        _channel.QueueBind(queue: _queueName, exchange: "orders.exchange", routingKey: "shipping.prepared");
        
        _logger.LogInformation("Order event listener queue setup completed");
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var routingKey = ea.RoutingKey;

                _logger.LogInformation("Received event with routing key: {RoutingKey}", routingKey);

                await ProcessEvent(routingKey, message);

                // Acknowledge the message
                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order event");
                
                // Reject and requeue the message for retry
                _channel.BasicReject(deliveryTag: ea.DeliveryTag, requeue: true);
            }
        };

        _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
        
        return Task.CompletedTask;
    }

    private async Task ProcessEvent(string routingKey, string message)
    {
        using var scope = _serviceProvider.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

        switch (routingKey)
        {
            case "order.completed":
                await HandleOrderCompleted(orderService, message);
                break;
                
            case "order.failed":
                await HandleOrderFailed(orderService, message);
                break;
                
            case "payment.processed":
                await HandlePaymentProcessed(orderService, message);
                break;
                
            case "inventory.reserved":
                await HandleInventoryReserved(orderService, message);
                break;
                
            case "shipping.prepared":
                await HandleShippingPrepared(orderService, message);
                break;
                
            default:
                _logger.LogWarning("Unknown routing key: {RoutingKey}", routingKey);
                break;
        }
    }

    private async Task HandleOrderCompleted(IOrderService orderService, string message)
    {
        try
        {
            var completedEvent = JsonSerializer.Deserialize<OrderCompletedEvent>(message);
            if (completedEvent != null)
            {
                _logger.LogInformation("Processing order completed event for order: {OrderId} [CorrelationId: {CorrelationId}]", 
                    completedEvent.OrderId, completedEvent.CorrelationId);
                
                // Update order status to Delivered
                await orderService.UpdateOrderStatusAsync(completedEvent.OrderId, new Models.DTOs.UpdateOrderStatusDto
                {
                    Status = OrderStatus.Delivered
                });
                
                _logger.LogInformation("Updated order {OrderId} status to Delivered [CorrelationId: {CorrelationId}]", 
                    completedEvent.OrderId, completedEvent.CorrelationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling order completed event");
            throw;
        }
    }

    private async Task HandleOrderFailed(IOrderService orderService, string message)
    {
        try
        {
            var failedEvent = JsonSerializer.Deserialize<OrderFailedEvent>(message);
            if (failedEvent != null)
            {
                _logger.LogInformation("Processing order failed event for order: {OrderId}", failedEvent.OrderId);
                
                // Update order status to Cancelled with reason
                await orderService.UpdateOrderStatusAsync(failedEvent.OrderId, new Models.DTOs.UpdateOrderStatusDto
                {
                    Status = OrderStatus.Cancelled
                });
                
                _logger.LogInformation("Updated order {OrderId} status to Cancelled due to: {Reason}", 
                    failedEvent.OrderId, failedEvent.Reason);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling order failed event");
            throw;
        }
    }

    private async Task HandlePaymentProcessed(IOrderService orderService, string message)
    {
        try
        {
            var paymentEvent = JsonSerializer.Deserialize<PaymentProcessedEvent>(message);
            if (paymentEvent != null)
            {
                _logger.LogInformation("Processing payment processed event for order: {OrderId} [CorrelationId: {CorrelationId}]", 
                    paymentEvent.OrderId, paymentEvent.CorrelationId);
                
                // Update order status to Confirmed (payment successful)
                await orderService.UpdateOrderStatusAsync(paymentEvent.OrderId, new Models.DTOs.UpdateOrderStatusDto
                {
                    Status = OrderStatus.Confirmed
                });
                
                _logger.LogInformation("Updated order {OrderId} status to Confirmed (payment processed) [CorrelationId: {CorrelationId}]", 
                    paymentEvent.OrderId, paymentEvent.CorrelationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payment processed event");
            throw;
        }
    }

    private async Task HandleInventoryReserved(IOrderService orderService, string message)
    {
        try
        {
            var inventoryEvent = JsonSerializer.Deserialize<InventoryReservedEvent>(message);
            if (inventoryEvent != null)
            {
                _logger.LogInformation("Processing inventory reserved event for order: {OrderId}", inventoryEvent.OrderId);
                
                // Update order status to Processing (inventory reserved)
                await orderService.UpdateOrderStatusAsync(inventoryEvent.OrderId, new Models.DTOs.UpdateOrderStatusDto
                {
                    Status = OrderStatus.Processing
                });
                
                _logger.LogInformation("Updated order {OrderId} status to Processing (inventory reserved)", inventoryEvent.OrderId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling inventory reserved event");
            throw;
        }
    }

    private async Task HandleShippingPrepared(IOrderService orderService, string message)
    {
        try
        {
            var shippingEvent = JsonSerializer.Deserialize<ShippingPreparedEvent>(message);
            if (shippingEvent != null)
            {
                _logger.LogInformation("Processing shipping prepared event for order: {OrderId}", shippingEvent.OrderId);
                
                // Update order status to Shipped
                await orderService.UpdateOrderStatusAsync(shippingEvent.OrderId, new Models.DTOs.UpdateOrderStatusDto
                {
                    Status = OrderStatus.Shipped
                });
                
                _logger.LogInformation("Updated order {OrderId} status to Shipped", shippingEvent.OrderId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling shipping prepared event");
            throw;
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}

// Event models for deserialization
public class OrderCompletedEvent
{
    public Guid OrderId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }
}

public class OrderFailedEvent
{
    public Guid OrderId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime FailedAt { get; set; }
}

public class PaymentProcessedEvent
{
    public Guid OrderId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string PaymentId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}

public class InventoryReservedEvent
{
    public Guid OrderId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string ReservationId { get; set; } = string.Empty;
    public DateTime ReservedAt { get; set; }
}

public class ShippingPreparedEvent
{
    public Guid OrderId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string ShippingId { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
    public DateTime PreparedAt { get; set; }
}
