using RabbitMQ.Client;

namespace OrderService.Core.Services.Messaging;

/// <summary>
/// Message broker adapter interface for consuming events
/// Provides abstraction over different broker implementations (RabbitMQ, Kafka, Azure Service Bus)
/// </summary>
public interface IMessageBrokerAdapter : IDisposable
{
    /// <summary>
    /// Connect to the message broker
    /// </summary>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribe to events with a message handler
    /// </summary>
    /// <param name="queueName">Queue or consumer group name</param>
    /// <param name="routingKeys">Routing keys/topics to subscribe to</param>
    /// <param name="messageHandler">Handler for processing messages</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SubscribeAsync(
        string queueName,
        IEnumerable<string> routingKeys,
        Func<string, string, Task> messageHandler,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the broker connection is healthy
    /// </summary>
    bool IsHealthy();

    /// <summary>
    /// Get broker type (e.g., "RabbitMQ", "Kafka", "AzureServiceBus")
    /// </summary>
    string GetBrokerType();
}
