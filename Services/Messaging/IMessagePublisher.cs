namespace OrderService.Services.Messaging;

/// <summary>
/// Message broker interface for publishing events
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publish a message to a topic/queue
    /// </summary>
    Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Publish a message with routing key (for RabbitMQ)
    /// </summary>
    Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken = default) where T : class;
}

/// <summary>
/// Message broker factory interface
/// </summary>
public interface IMessagePublisherFactory
{
    /// <summary>
    /// Create a message publisher based on configuration
    /// </summary>
    IMessagePublisher CreatePublisher();
}
