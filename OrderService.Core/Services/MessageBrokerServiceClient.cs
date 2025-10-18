using Microsoft.Extensions.Logging;
using OrderService.Core.Configuration;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace OrderService.Core.Services;

/// <summary>
/// HTTP client for message-broker-service integration
/// Provides methods to publish events to the message broker service
/// 
/// Configuration:
/// - MessageBroker:Service:Url in appsettings.json (default: http://localhost:4000)
/// - MessageBroker:Service:ApiKey in appsettings.json (optional)
/// - MessageBroker:Service:TimeoutSeconds in appsettings.json (default: 30)
/// 
/// Environment variables can override config:
/// - MESSAGE_BROKER_SERVICE_URL overrides Url
/// - MESSAGE_BROKER_API_KEY overrides ApiKey
/// </summary>
public class MessageBrokerServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MessageBrokerServiceClient> _logger;
    private readonly MessageBrokerSettings _settings;

    public MessageBrokerServiceClient(
        HttpClient httpClient,
        ILogger<MessageBrokerServiceClient> logger,
        IOptions<MessageBrokerSettings> settings)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = settings.Value;

        // Configure HttpClient base address (environment variable overrides config)
        var messageBrokerUrl = Environment.GetEnvironmentVariable("MESSAGE_BROKER_SERVICE_URL") 
            ?? _settings.Service.Url;
        _httpClient.BaseAddress = new Uri(messageBrokerUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.Service.TimeoutSeconds);
        
        // Add API key if configured (environment variable overrides config)
        var apiKey = Environment.GetEnvironmentVariable("MESSAGE_BROKER_API_KEY") 
            ?? _settings.Service.ApiKey;
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        }

        _logger.LogInformation("MessageBrokerServiceClient configured with URL: {Url}, Timeout: {Timeout}s", 
            messageBrokerUrl, _settings.Service.TimeoutSeconds);
    }

    /// <summary>
    /// Publishes an event to the message broker service
    /// </summary>
    /// <typeparam name="T">Message payload type</typeparam>
    /// <param name="exchange">Exchange name (e.g., "aioutlet.events")</param>
    /// <param name="routingKey">Routing key (e.g., "order.created")</param>
    /// <param name="message">Message payload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task PublishEventAsync<T>(
        string exchange, 
        string routingKey, 
        T message, 
        CancellationToken cancellationToken = default) where T : class
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

            _logger.LogInformation(
                "Publishing event to message-broker-service: {Exchange}/{RoutingKey}", 
                exchange, 
                routingKey);

            var response = await _httpClient.PostAsync("/api/events/publish", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Failed to publish event to message-broker-service. Status: {StatusCode}, Error: {Error}", 
                    response.StatusCode, 
                    errorContent);
                throw new HttpRequestException(
                    $"Failed to publish event: {response.StatusCode} - {errorContent}");
            }

            _logger.LogInformation(
                "Successfully published event to message-broker-service: {Exchange}/{RoutingKey}", 
                exchange, 
                routingKey);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex, 
                "HTTP error publishing event to message-broker-service: {Exchange}/{RoutingKey}", 
                exchange, 
                routingKey);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(
                ex, 
                "Timeout publishing event to message-broker-service: {Exchange}/{RoutingKey}", 
                exchange, 
                routingKey);
            throw new TimeoutException(
                $"Request to message-broker-service timed out after {_settings.Service.TimeoutSeconds} seconds", 
                ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, 
                "Unexpected error publishing event to message-broker-service: {Exchange}/{RoutingKey}", 
                exchange, 
                routingKey);
            throw;
        }
    }

    /// <summary>
    /// Health check for message-broker-service connectivity
    /// </summary>
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check failed for message-broker-service");
            return false;
        }
    }
}
