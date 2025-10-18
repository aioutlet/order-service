using OrderService.Core.Services.Messaging;
using OrderService.Worker.Handlers;

namespace OrderService.Worker;

/// <summary>
/// Background service to listen for order processing events
/// Uses adapter pattern for broker abstraction and handler registry for event processing
/// </summary>
public class OrderEventListenerWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderEventListenerWorker> _logger;
    private readonly IMessageBrokerAdapter _brokerAdapter;
    private readonly string _queueName = "order-service.order-processor-events";
    private readonly EventHandlerRegistry _handlerRegistry;

    public OrderEventListenerWorker(
        IServiceProvider serviceProvider,
        ILogger<OrderEventListenerWorker> logger,
        IMessageBrokerAdapter brokerAdapter)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _brokerAdapter = brokerAdapter;
        
        // Initialize handler registry
        using var scope = serviceProvider.CreateScope();
        var registryLogger = scope.ServiceProvider.GetRequiredService<ILogger<EventHandlerRegistry>>();
        _handlerRegistry = new EventHandlerRegistry(serviceProvider, registryLogger);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Connect to broker
            _logger.LogInformation("Connecting to message broker ({BrokerType})...", _brokerAdapter.GetBrokerType());
            await _brokerAdapter.ConnectAsync(stoppingToken);
            
            // Get routing keys from handler registry
            var routingKeys = _handlerRegistry.GetRoutingKeys().ToList();
            _logger.LogInformation("Subscribing to {Count} event types", routingKeys.Count);

            // Subscribe to events
            await _brokerAdapter.SubscribeAsync(
                _queueName,
                routingKeys,
                async (routingKey, message) =>
                {
                    _logger.LogInformation("Received event with routing key: {RoutingKey}", routingKey);
                    await _handlerRegistry.ProcessEventAsync(routingKey, message, stoppingToken);
                },
                stoppingToken);

            _logger.LogInformation("Order Event Listener Worker started successfully using {BrokerType}", 
                _brokerAdapter.GetBrokerType());

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Order Event Listener Worker is stopping due to cancellation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in Order Event Listener Worker");
            throw;
        }
    }

    public override void Dispose()
    {
        _logger.LogInformation("Shutting down Order Event Listener Worker");
        _brokerAdapter?.Dispose();
        base.Dispose();
    }
}
