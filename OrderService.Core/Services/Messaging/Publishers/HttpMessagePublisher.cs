using Microsoft.Extensions.Logging;
using OrderService.Core.Configuration;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace OrderService.Core.Services.Messaging.Publishers;

/// <summary>
/// HTTP-based message publisher that sends events to message-broker-service
/// Used by API process to publish events without direct RabbitMQ connection
/// 
/// Configuration:
/// - MESSAGE_BROKER_SERVICE_URL environment variable (default: http://localhost:4000)
/// - MESSAGE_BROKER_API_KEY environment variable (optional)
/// - MessageBroker:Topics section in appsettings.json for routing keys
/// </summary>
public class HttpMessagePublisher : IMessagePublisher
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpMessagePublisher> _logger;
    private readonly MessageBrokerSettings _settings;

    public HttpMessagePublisher(
        HttpClient httpClient,
        ILogger<HttpMessagePublisher> logger,
        IOptions<MessageBrokerSettings> settings)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = settings.Value;

        // Configure HttpClient base address from configuration
        var messageBrokerUrl = Environment.GetEnvironmentVariable("MESSAGE_BROKER_SERVICE_URL") 
            ?? "http://localhost:4000";
        _httpClient.BaseAddress = new Uri(messageBrokerUrl);
        
        // Add API key if configured
        var apiKey = Environment.GetEnvironmentVariable("MESSAGE_BROKER_API_KEY");
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        }
    }

    public async Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default) where T : class
    {
        await PublishAsync("orders.exchange", topic, message, cancellationToken);
    }

    public async Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var payload = new
            {
                exchange = exchange,
                routingKey = routingKey,
                message = message,
                timestamp = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Publishing event via HTTP: {Exchange}/{RoutingKey}", exchange, routingKey);

            var response = await _httpClient.PostAsync("/api/events/publish", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to publish event via HTTP. Status: {StatusCode}, Error: {Error}", 
                    response.StatusCode, errorContent);
                throw new Exception($"Failed to publish event: {response.StatusCode}");
            }

            _logger.LogInformation("Successfully published event via HTTP: {Exchange}/{RoutingKey}", exchange, routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing event via HTTP: {Exchange}/{RoutingKey}", exchange, routingKey);
            throw;
        }
    }
}
