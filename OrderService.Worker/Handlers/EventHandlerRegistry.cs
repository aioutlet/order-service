using System.Text.Json;
using OrderService.Worker.Events;

namespace OrderService.Worker.Handlers;

/// <summary>
/// Registry for mapping routing keys to event handlers
/// </summary>
public class EventHandlerRegistry
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventHandlerRegistry> _logger;
    private readonly Dictionary<string, Func<string, CancellationToken, Task>> _handlers;

    public EventHandlerRegistry(IServiceProvider serviceProvider, ILogger<EventHandlerRegistry> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _handlers = new Dictionary<string, Func<string, CancellationToken, Task>>
        {
            ["order.completed"] = HandleOrderCompletedAsync,
            ["order.failed"] = HandleOrderFailedAsync,
            ["payment.processed"] = HandlePaymentProcessedAsync,
            ["inventory.reserved"] = HandleInventoryReservedAsync,
            ["shipping.prepared"] = HandleShippingPreparedAsync
        };
    }

    /// <summary>
    /// Process an event based on its routing key
    /// </summary>
    public async Task ProcessEventAsync(string routingKey, string message, CancellationToken cancellationToken = default)
    {
        if (_handlers.TryGetValue(routingKey, out var handler))
        {
            await handler(message, cancellationToken);
        }
        else
        {
            _logger.LogWarning("No handler registered for routing key: {RoutingKey}", routingKey);
        }
    }

    private async Task HandleOrderCompletedAsync(string message, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IEventHandler<OrderCompletedEvent>>();
        var @event = JsonSerializer.Deserialize<OrderCompletedEvent>(message);
        
        if (@event != null)
        {
            await handler.HandleAsync(@event, cancellationToken);
        }
    }

    private async Task HandleOrderFailedAsync(string message, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IEventHandler<OrderFailedEvent>>();
        var @event = JsonSerializer.Deserialize<OrderFailedEvent>(message);
        
        if (@event != null)
        {
            await handler.HandleAsync(@event, cancellationToken);
        }
    }

    private async Task HandlePaymentProcessedAsync(string message, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IEventHandler<PaymentProcessedEvent>>();
        var @event = JsonSerializer.Deserialize<PaymentProcessedEvent>(message);
        
        if (@event != null)
        {
            await handler.HandleAsync(@event, cancellationToken);
        }
    }

    private async Task HandleInventoryReservedAsync(string message, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IEventHandler<InventoryReservedEvent>>();
        var @event = JsonSerializer.Deserialize<InventoryReservedEvent>(message);
        
        if (@event != null)
        {
            await handler.HandleAsync(@event, cancellationToken);
        }
    }

    private async Task HandleShippingPreparedAsync(string message, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IEventHandler<ShippingPreparedEvent>>();
        var @event = JsonSerializer.Deserialize<ShippingPreparedEvent>(message);
        
        if (@event != null)
        {
            await handler.HandleAsync(@event, cancellationToken);
        }
    }

    /// <summary>
    /// Get all registered routing keys
    /// </summary>
    public IEnumerable<string> GetRoutingKeys() => _handlers.Keys;
}
