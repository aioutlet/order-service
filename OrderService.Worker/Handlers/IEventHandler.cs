namespace OrderService.Worker.Handlers;

/// <summary>
/// Interface for event handlers
/// </summary>
/// <typeparam name="TEvent">The type of event to handle</typeparam>
public interface IEventHandler<TEvent>
{
    /// <summary>
    /// Handle the event asynchronously
    /// </summary>
    /// <param name="event">The event to handle</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
