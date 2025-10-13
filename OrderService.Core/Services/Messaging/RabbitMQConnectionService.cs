using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using OrderService.Core.Configuration;
using Microsoft.Extensions.Options;

namespace OrderService.Core.Services.Messaging;

/// <summary>
/// Service to provide shared RabbitMQ connections
/// </summary>
public interface IRabbitMQConnectionService : IDisposable
{
    IConnection GetConnection();
}

public class RabbitMQConnectionService : IRabbitMQConnectionService
{
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<RabbitMQConnectionService> _logger;
    private readonly Lazy<IConnection> _connection;

    public RabbitMQConnectionService(IOptions<MessageBrokerSettings> messageBrokerSettings, ILogger<RabbitMQConnectionService> logger)
    {
        _settings = messageBrokerSettings.Value.RabbitMQ;
        _logger = logger;

        _connection = new Lazy<IConnection>(CreateConnection);
    }

    public IConnection GetConnection()
    {
        return _connection.Value;
    }

    private IConnection CreateConnection()
    {
        try
        {
            var factory = new ConnectionFactory();
            factory.Uri = new Uri(_settings.ConnectionString);
            
            // Configure connection settings for reliability
            factory.AutomaticRecoveryEnabled = true;
            factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(10);
            factory.RequestedHeartbeat = TimeSpan.FromSeconds(60);

            var connection = factory.CreateConnection("OrderService-EventListener");
            
            _logger.LogInformation("RabbitMQ connection established successfully");
            
            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create RabbitMQ connection");
            throw;
        }
    }

    public void Dispose()
    {
        if (_connection.IsValueCreated)
        {
            _connection.Value?.Close();
            _connection.Value?.Dispose();
        }
    }
}
