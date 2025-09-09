using Azure.Identity;
using Azure.Messaging.ServiceBus;
using System.Text.Json;
using OrderService.Configuration;
using Microsoft.Extensions.Options;

namespace OrderService.Services.Messaging;

/// <summary>
/// Azure Service Bus implementation of message publisher
/// Following Azure best practices with managed identity and retry policies
/// </summary>
public class AzureServiceBusPublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly AzureServiceBusSettings _settings;
    private readonly ILogger<AzureServiceBusPublisher> _logger;
    private readonly ServiceBusClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public AzureServiceBusPublisher(IOptions<MessageBrokerSettings> messageBrokerSettings, ILogger<AzureServiceBusPublisher> logger)
    {
        _settings = messageBrokerSettings.Value.AzureServiceBus;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        try
        {
            // Use managed identity when available (Azure best practice)
            if (_settings.UseManagedIdentity && !string.IsNullOrEmpty(_settings.Namespace))
            {
                var credential = new DefaultAzureCredential();
                _client = new ServiceBusClient($"{_settings.Namespace}.servicebus.windows.net", credential);
                _logger.LogInformation("Azure Service Bus client initialized with managed identity. Namespace: {Namespace}", _settings.Namespace);
            }
            else if (!string.IsNullOrEmpty(_settings.ConnectionString))
            {
                _client = new ServiceBusClient(_settings.ConnectionString);
                _logger.LogInformation("Azure Service Bus client initialized with connection string");
            }
            else
            {
                throw new InvalidOperationException("Azure Service Bus configuration is invalid. Either provide Namespace for managed identity or ConnectionString.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Azure Service Bus publisher");
            throw;
        }
    }

    /// <summary>
    /// Publish message to Service Bus topic/queue
    /// </summary>
    public async Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default) where T : class
    {
        await PublishAsync(topic, string.Empty, message, cancellationToken);
    }

    /// <summary>
    /// Publish message to Service Bus topic with subject (routing key equivalent)
    /// </summary>
    public async Task PublishAsync<T>(string topicOrQueue, string subject, T message, CancellationToken cancellationToken = default) where T : class
    {
        var attempts = 0;
        var maxAttempts = _settings.RetryAttempts;

        while (attempts < maxAttempts)
        {
            ServiceBusSender? sender = null;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                sender = _client.CreateSender(topicOrQueue);

                var messageJson = JsonSerializer.Serialize(message, _jsonOptions);
                var serviceBusMessage = new ServiceBusMessage(messageJson)
                {
                    MessageId = Guid.NewGuid().ToString(),
                    ContentType = "application/json",
                    Subject = !string.IsNullOrEmpty(subject) ? subject : typeof(T).Name,
                    TimeToLive = TimeSpan.FromHours(24) // Default TTL
                };

                // Add custom properties for better routing and debugging
                serviceBusMessage.ApplicationProperties["MessageType"] = typeof(T).Name;
                serviceBusMessage.ApplicationProperties["Source"] = "OrderService";
                serviceBusMessage.ApplicationProperties["Version"] = "1.0";

                _logger.LogDebug("Publishing message to Service Bus topic: {Topic}, subject: {Subject}, type: {MessageType}",
                    topicOrQueue, subject, typeof(T).Name);

                await sender.SendMessageAsync(serviceBusMessage, cancellationToken);

                _logger.LogInformation("Successfully published message to Service Bus {Topic}. MessageId: {MessageId}, Subject: {Subject}",
                    topicOrQueue, serviceBusMessage.MessageId, serviceBusMessage.Subject);

                return;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Message publishing was cancelled");
                throw;
            }
            catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound)
            {
                _logger.LogError(ex, "Service Bus topic/queue not found: {Topic}. Ensure the topic/queue exists.", topicOrQueue);
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Unauthorized access to Service Bus. Check managed identity permissions or connection string.");
                throw;
            }
            catch (ServiceBusException ex) when (IsTransientError(ex))
            {
                attempts++;
                _logger.LogWarning(ex, "Transient Service Bus error (attempt {Attempt}/{MaxAttempts}). Topic: {Topic}",
                    attempts, maxAttempts, topicOrQueue);

                if (attempts >= maxAttempts)
                {
                    _logger.LogError(ex, "Failed to publish message after {MaxAttempts} attempts", maxAttempts);
                    throw;
                }

                // Exponential backoff with jitter
                var delay = TimeSpan.FromMilliseconds(_settings.RetryDelayMs * Math.Pow(2, attempts - 1));
                var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000));
                await Task.Delay(delay + jitter, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error publishing message to Service Bus topic: {Topic}", topicOrQueue);
                throw;
            }
            finally
            {
                if (sender != null)
                {
                    await sender.DisposeAsync();
                }
            }
        }
    }

    /// <summary>
    /// Check if Service Bus exception is transient and should be retried
    /// </summary>
    private static bool IsTransientError(ServiceBusException ex)
    {
        return ex.Reason == ServiceBusFailureReason.ServiceTimeout ||
               ex.Reason == ServiceBusFailureReason.ServiceBusy ||
               ex.Reason == ServiceBusFailureReason.ServiceCommunicationProblem ||
               ex.Reason == ServiceBusFailureReason.GeneralError;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_client != null)
            {
                await _client.DisposeAsync();
                _logger.LogInformation("Azure Service Bus publisher disposed successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing Azure Service Bus publisher");
        }
    }
}
