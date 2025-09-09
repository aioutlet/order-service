namespace OrderService.Configuration;

/// <summary>
/// Message broker configuration settings
/// </summary>
public class MessageBrokerSettings
{
    public const string SectionName = "MessageBroker";

    /// <summary>
    /// Message broker provider (RabbitMQ, AzureServiceBus)
    /// </summary>
    public string Provider { get; set; } = "RabbitMQ";

    /// <summary>
    /// RabbitMQ configuration
    /// </summary>
    public RabbitMQSettings RabbitMQ { get; set; } = new();

    /// <summary>
    /// Azure Service Bus configuration
    /// </summary>
    public AzureServiceBusSettings AzureServiceBus { get; set; } = new();

    /// <summary>
    /// Topic/Queue names
    /// </summary>
    public MessagingTopics Topics { get; set; } = new();
}

/// <summary>
/// RabbitMQ specific settings
/// </summary>
public class RabbitMQSettings
{
    /// <summary>
    /// RabbitMQ connection string
    /// </summary>
    public string ConnectionString { get; set; } = "amqp://guest:guest@localhost:5672/";

    /// <summary>
    /// Exchange name for order events
    /// </summary>
    public string Exchange { get; set; } = "orders.exchange";

    /// <summary>
    /// Exchange type (topic, direct, fanout)
    /// </summary>
    public string ExchangeType { get; set; } = "topic";

    /// <summary>
    /// Enable publisher confirms
    /// </summary>
    public bool PublisherConfirms { get; set; } = true;

    /// <summary>
    /// Connection retry attempts
    /// </summary>
    public int RetryAttempts { get; set; } = 3;
}

/// <summary>
/// Azure Service Bus specific settings
/// </summary>
public class AzureServiceBusSettings
{
    /// <summary>
    /// Service Bus connection string (fallback if managed identity fails)
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Service Bus namespace (for managed identity)
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// Use managed identity for authentication
    /// </summary>
    public bool UseManagedIdentity { get; set; } = true;

    /// <summary>
    /// Connection retry attempts
    /// </summary>
    public int RetryAttempts { get; set; } = 3;

    /// <summary>
    /// Retry delay in milliseconds
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;
}

/// <summary>
/// Topic/Queue name configurations
/// </summary>
public class MessagingTopics
{
    /// <summary>
    /// Order created event topic/queue
    /// </summary>
    public string OrderCreated { get; set; } = "order.created";

    /// <summary>
    /// Order updated event topic/queue
    /// </summary>
    public string OrderUpdated { get; set; } = "order.updated";

    /// <summary>
    /// Order cancelled event topic/queue
    /// </summary>
    public string OrderCancelled { get; set; } = "order.cancelled";
}
