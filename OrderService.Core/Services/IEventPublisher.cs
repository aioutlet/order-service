namespace OrderService.Core.Services;

/// <summary>
/// Interface for event publishing to message brokers
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes an event to the message broker
    /// </summary>
    /// <typeparam name="T">Message payload type</typeparam>
    /// <param name="routingKey">Topic/routing key (e.g., "order.created")</param>
    /// <param name="message">Message payload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishEventAsync<T>(string routingKey, T message, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Health check for event publisher connectivity
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
