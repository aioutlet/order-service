using OrderService.Core.Services.Messaging;
using OrderService.Worker.Handlers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace OrderService.Worker;

/// <summary>
/// Background service to listen for order processing events from Order Processor Service
/// Uses a handler registry pattern for clean separation of concerns
/// </summary>
public class OrderEventListenerWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderEventListenerWorker> _logger;
    private readonly IRabbitMQConnectionService _connectionService;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _queueName = "order-service.order-processor-events";
    private readonly EventHandlerRegistry _handlerRegistry;

    public OrderEventListenerWorker(
        IServiceProvider serviceProvider,
        ILogger<OrderEventListenerWorker> logger,
        IRabbitMQConnectionService connectionService)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _connectionService = connectionService;
        _connection = _connectionService.GetConnection();
        _channel = _connection.CreateModel();
        
        // Initialize handler registry
        using var scope = serviceProvider.CreateScope();
        var registryLogger = scope.ServiceProvider.GetRequiredService<ILogger<EventHandlerRegistry>>();
        _handlerRegistry = new EventHandlerRegistry(serviceProvider, registryLogger);
        
        SetupQueue();
    }

    private void SetupQueue()
    {
        // Declare exchange and queue
        _channel.ExchangeDeclare(exchange: "orders.exchange", type: ExchangeType.Topic, durable: true);
        _channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false);
        
        // Bind to all registered routing keys from handler registry
        foreach (var routingKey in _handlerRegistry.GetRoutingKeys())
        {
            _channel.QueueBind(queue: _queueName, exchange: "orders.exchange", routingKey: routingKey);
            _logger.LogInformation("Bound queue to routing key: {RoutingKey}", routingKey);
        }
        
        _logger.LogInformation("Order event listener queue setup completed for {Count} event types", 
            _handlerRegistry.GetRoutingKeys().Count());
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

                // Delegate to handler registry
                await _handlerRegistry.ProcessEventAsync(routingKey, message, stoppingToken);

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
        
        _logger.LogInformation("Order Event Listener Worker started successfully");
        
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _logger.LogInformation("Shutting down Order Event Listener Worker");
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}
