using Microsoft.Extensions.Logging;
using OrderService.Core.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace OrderService.Core.Services.Messaging;

/// <summary>
/// Factory for creating message publishers based on configuration
/// </summary>
public class MessagePublisherFactory : IMessagePublisherFactory
{
    private readonly MessageBrokerSettings _settings;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MessagePublisherFactory> _logger;

    public MessagePublisherFactory(
        IOptions<MessageBrokerSettings> settings,
        IServiceProvider serviceProvider,
        ILogger<MessagePublisherFactory> logger)
    {
        _settings = settings.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Create message publisher based on configured provider
    /// </summary>
    public IMessagePublisher CreatePublisher()
    {
        var provider = _settings.Provider?.ToLowerInvariant();

        _logger.LogInformation("Creating message publisher for provider: {Provider}", provider);

        return provider switch
        {
            "rabbitmq" => _serviceProvider.GetRequiredService<RabbitMQPublisher>(),
            "azureservicebus" => _serviceProvider.GetRequiredService<AzureServiceBusPublisher>(),
            _ => throw new InvalidOperationException($"Unsupported message broker provider: {_settings.Provider}. Supported providers: RabbitMQ, AzureServiceBus")
        };
    }
}

/// <summary>
/// Scoped message publisher service that wraps the factory
/// </summary>
public class MessagePublisherService : IMessagePublisher, IDisposable
{
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<MessagePublisherService> _logger;

    public MessagePublisherService(IMessagePublisherFactory factory, ILogger<MessagePublisherService> logger)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _publisher = _factory.CreatePublisher();
    }

    private readonly IMessagePublisherFactory _factory;

    public Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default) where T : class
    {
        return _publisher.PublishAsync(topic, message, cancellationToken);
    }

    public Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken = default) where T : class
    {
        return _publisher.PublishAsync(exchange, routingKey, message, cancellationToken);
    }

    public void Dispose()
    {
        if (_publisher is IDisposable disposable)
        {
            disposable.Dispose();
        }
        else if (_publisher is IAsyncDisposable asyncDisposable)
        {
            // For async disposables, we'll handle them in a background task
            // In a real-world scenario, you might want to use a more sophisticated approach
            Task.Run(async () =>
            {
                try
                {
                    await asyncDisposable.DisposeAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing message publisher asynchronously");
                }
            });
        }
    }
}
