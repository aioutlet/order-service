using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using OrderService.Core.Configuration;
using Microsoft.Extensions.Options;

namespace OrderService.Core.Services.Messaging.Publishers;

/// <summary>
/// RabbitMQ implementation of message publisher
/// </summary>
public class RabbitMQPublisher : IMessagePublisher, IDisposable
{
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<RabbitMQPublisher> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly JsonSerializerOptions _jsonOptions;

    public RabbitMQPublisher(IOptions<MessageBrokerSettings> messageBrokerSettings, ILogger<RabbitMQPublisher> logger)
    {
        _settings = messageBrokerSettings.Value.RabbitMQ;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        try
        {
            var factory = new ConnectionFactory();
            factory.Uri = new Uri(_settings.ConnectionString);
            
            // Configure connection settings for reliability
            factory.AutomaticRecoveryEnabled = true;
            factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(10);
            factory.RequestedHeartbeat = TimeSpan.FromSeconds(60);

            _connection = factory.CreateConnection("OrderService");
            _channel = _connection.CreateModel();

            // Declare exchange
            _channel.ExchangeDeclare(
                exchange: _settings.Exchange,
                type: _settings.ExchangeType,
                durable: true,
                autoDelete: false);

            // Enable publisher confirms for reliability
            if (_settings.PublisherConfirms)
            {
                _channel.ConfirmSelect();
            }

            _logger.LogInformation("RabbitMQ publisher initialized successfully. Exchange: {Exchange}", _settings.Exchange);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ publisher");
            throw;
        }
    }

    /// <summary>
    /// Publish message to topic (uses topic as routing key)
    /// </summary>
    public async Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default) where T : class
    {
        await PublishAsync(_settings.Exchange, topic, message, cancellationToken);
    }

    /// <summary>
    /// Publish message to exchange with routing key
    /// </summary>
    public async Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken = default) where T : class
    {
        var attempts = 0;
        var maxAttempts = _settings.RetryAttempts;

        while (attempts < maxAttempts)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var messageJson = JsonSerializer.Serialize(message, _jsonOptions);
                var body = Encoding.UTF8.GetBytes(messageJson);

                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true; // Make message persistent
                properties.MessageId = Guid.NewGuid().ToString();
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                properties.ContentType = "application/json";
                properties.Type = typeof(T).Name;

                _logger.LogDebug("Publishing message to exchange: {Exchange}, routing key: {RoutingKey}, type: {MessageType}",
                    exchange, routingKey, typeof(T).Name);

                _channel.BasicPublish(
                    exchange: exchange,
                    routingKey: routingKey,
                    basicProperties: properties,
                    body: body);

                // Wait for publisher confirmation if enabled
                if (_settings.PublisherConfirms)
                {
                    if (!_channel.WaitForConfirms(TimeSpan.FromSeconds(10)))
                    {
                        throw new InvalidOperationException("Message was not confirmed by RabbitMQ");
                    }
                }

                _logger.LogInformation("Successfully published message to {Exchange}/{RoutingKey}. MessageId: {MessageId}",
                    exchange, routingKey, properties.MessageId);

                return;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Message publishing was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                attempts++;
                _logger.LogWarning(ex, "Failed to publish message (attempt {Attempt}/{MaxAttempts}). Exchange: {Exchange}, RoutingKey: {RoutingKey}",
                    attempts, maxAttempts, exchange, routingKey);

                if (attempts >= maxAttempts)
                {
                    _logger.LogError(ex, "Failed to publish message after {MaxAttempts} attempts", maxAttempts);
                    throw;
                }

                // Exponential backoff
                var delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempts) * 1000);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    public void Dispose()
    {
        try
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
            _logger.LogInformation("RabbitMQ publisher disposed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ publisher");
        }
    }
}
