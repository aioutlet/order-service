namespace OrderService.Core.Configuration;

/// <summary>
/// Message broker configuration settings
/// Supports both HTTP integration (via message-broker-service) and direct broker connections
/// </summary>
public class MessageBrokerSettings
{
    public const string SectionName = "MessageBroker";

    /// <summary>
    /// Message broker provider (RabbitMQ, Kafka, AzureServiceBus)
    /// </summary>
    public string Provider { get; set; } = "RabbitMQ";

    /// <summary>
    /// Message broker service HTTP integration settings
    /// Used by API to publish events via message-broker-service
    /// </summary>
    public MessageBrokerServiceSettings Service { get; set; } = new();

    /// <summary>
    /// RabbitMQ configuration
    /// Used by Worker for direct connection
    /// </summary>
    public RabbitMQSettings RabbitMQ { get; set; } = new();

    /// <summary>
    /// Kafka configuration
    /// Used by Worker for direct connection
    /// </summary>
    public KafkaSettings Kafka { get; set; } = new();

    /// <summary>
    /// Azure Service Bus configuration
    /// Used by Worker for direct connection
    /// </summary>
    public AzureServiceBusSettings AzureServiceBus { get; set; } = new();

    /// <summary>
    /// Topic/Queue names
    /// </summary>
    public MessagingTopics Topics { get; set; } = new();
}

/// <summary>
/// Message broker service HTTP integration settings
/// For API process to publish events via message-broker-service
/// </summary>
public class MessageBrokerServiceSettings
{
    /// <summary>
    /// Message broker service base URL
    /// Default: http://localhost:4000
    /// </summary>
    public string Url { get; set; } = "http://localhost:4000";

    /// <summary>
    /// Optional API key for authentication
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// HTTP request timeout in seconds
    /// Default: 30 seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
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
    public string Exchange { get; set; } = "aioutlet.events";

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
/// Kafka specific settings
/// </summary>
public class KafkaSettings
{
    /// <summary>
    /// Kafka broker addresses (comma-separated)
    /// </summary>
    public string Brokers { get; set; } = "localhost:9092";

    /// <summary>
    /// Topic name for order events
    /// </summary>
    public string Topic { get; set; } = "aioutlet.events";

    /// <summary>
    /// Consumer group ID
    /// </summary>
    public string GroupId { get; set; } = "order-service-worker";

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

    /// <summary>
    /// Order shipped event topic/queue
    /// </summary>
    public string OrderShipped { get; set; } = "order.shipped";

    /// <summary>
    /// Order delivered event topic/queue
    /// </summary>
    public string OrderDelivered { get; set; } = "order.delivered";

    /// <summary>
    /// Order deleted event topic/queue
    /// </summary>
    public string OrderDeleted { get; set; } = "order.deleted";
}
