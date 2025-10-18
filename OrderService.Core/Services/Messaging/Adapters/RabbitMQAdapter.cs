using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderService.Core.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace OrderService.Core.Services.Messaging.Adapters;

/// <summary>
/// RabbitMQ implementation of the message broker adapter
/// </summary>
public class RabbitMQAdapter : IMessageBrokerAdapter
{
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<RabbitMQAdapter> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly string _exchange;
    private bool _isDisposed;

    public RabbitMQAdapter(
        IOptions<MessageBrokerSettings> messageBrokerSettings,
        ILogger<RabbitMQAdapter> logger)
    {
        _settings = messageBrokerSettings.Value.RabbitMQ;
        _logger = logger;
        _exchange = _settings.Exchange;
    }

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(_settings.ConnectionString),
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                RequestedHeartbeat = TimeSpan.FromSeconds(60)
            };

            _connection = factory.CreateConnection("OrderService-Worker");
            _channel = _connection.CreateModel();

            // Declare exchange
            _channel.ExchangeDeclare(
                exchange: _exchange,
                type: _settings.ExchangeType,
                durable: true);

            _logger.LogInformation("RabbitMQ connection established successfully");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ");
            throw;
        }
    }

    public Task SubscribeAsync(
        string queueName,
        IEnumerable<string> routingKeys,
        Func<string, string, Task> messageHandler,
        CancellationToken cancellationToken = default)
    {
        if (_channel == null)
        {
            throw new InvalidOperationException("Channel not initialized. Call ConnectAsync first.");
        }

        // Declare queue
        _channel.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        // Bind queue to exchange with routing keys
        foreach (var routingKey in routingKeys)
        {
            _channel.QueueBind(
                queue: queueName,
                exchange: _exchange,
                routingKey: routingKey);
            _logger.LogInformation("Bound queue {Queue} to routing key: {RoutingKey}", queueName, routingKey);
        }

        // Setup consumer
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var routingKey = ea.RoutingKey;

                _logger.LogInformation("Received event with routing key: {RoutingKey}", routingKey);

                // Process message
                await messageHandler(routingKey, message);

                // Acknowledge the message
                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RabbitMQ message");

                // Reject and requeue the message for retry
                _channel.BasicReject(deliveryTag: ea.DeliveryTag, requeue: true);
            }
        };

        _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);
        _logger.LogInformation("Started consuming from queue: {Queue}", queueName);

        return Task.CompletedTask;
    }

    public bool IsHealthy()
    {
        return _connection?.IsOpen == true && _channel?.IsOpen == true;
    }

    public string GetBrokerType()
    {
        return "RabbitMQ";
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _logger.LogInformation("Disposing RabbitMQ adapter");
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();

        _isDisposed = true;
    }
}
