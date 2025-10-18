using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderService.Core.Configuration;
using OrderService.Core.Services.Messaging.Adapters;

namespace OrderService.Core.Services.Messaging;

/// <summary>
/// Factory for creating message broker adapters based on configuration
/// </summary>
public class MessageBrokerAdapterFactory
{
    private readonly IOptions<MessageBrokerSettings> _settings;
    private readonly ILoggerFactory _loggerFactory;

    public MessageBrokerAdapterFactory(
        IOptions<MessageBrokerSettings> settings,
        ILoggerFactory loggerFactory)
    {
        _settings = settings;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Create a message broker adapter based on the configured provider
    /// </summary>
    public IMessageBrokerAdapter CreateAdapter()
    {
        var provider = _settings.Value.Provider?.ToLowerInvariant() ?? "rabbitmq";

        return provider switch
        {
            "rabbitmq" => new RabbitMQAdapter(
                _settings,
                _loggerFactory.CreateLogger<RabbitMQAdapter>()),

            "kafka" => new KafkaAdapter(
                _settings,
                _loggerFactory.CreateLogger<KafkaAdapter>()),

            "azureservicebus" or "azure-servicebus" => new AzureServiceBusAdapter(
                _settings,
                _loggerFactory.CreateLogger<AzureServiceBusAdapter>()),

            _ => throw new NotSupportedException(
                $"Message broker provider '{provider}' is not supported. " +
                $"Supported providers: rabbitmq, kafka, azureservicebus")
        };
    }
}
