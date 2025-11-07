using Dapr.Client;
using Microsoft.Extensions.Logging;

namespace OrderService.Core.Services;

/// <summary>
/// Service for retrieving secrets from Dapr Secret Store
/// </summary>
public class DaprSecretService
{
    private readonly DaprClient _daprClient;
    private readonly ILogger<DaprSecretService> _logger;
    private const string SecretStoreName = "local-secret-store";

    public DaprSecretService(DaprClient daprClient, ILogger<DaprSecretService> logger)
    {
        _daprClient = daprClient;
        _logger = logger;
    }

    /// <summary>
    /// Get a secret value from Dapr Secret Store
    /// </summary>
    /// <param name="secretName">Name of the secret (e.g., "jwt:secret")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Secret value or null if not found</returns>
    public async Task<string?> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        try
        {
            // Split the secret name for nested keys (e.g., "jwt:secret" -> "jwt" with key "secret")
            var parts = secretName.Split(':', 2);
            var secretKey = parts[0];
            var nestedKey = parts.Length > 1 ? parts[1] : null;

            _logger.LogDebug("Retrieving secret: {SecretKey} from store: {StoreName}", secretKey, SecretStoreName);

            var secrets = await _daprClient.GetSecretAsync(
                SecretStoreName,
                secretKey,
                cancellationToken: cancellationToken);

            if (secrets == null || secrets.Count == 0)
            {
                _logger.LogWarning("Secret not found: {SecretKey}", secretKey);
                return null;
            }

            // If nested key specified, look for it
            if (nestedKey != null && secrets.ContainsKey(nestedKey))
            {
                return secrets[nestedKey];
            }

            // Otherwise return the first value
            return secrets.FirstOrDefault().Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve secret: {SecretName}", secretName);
            return null;
        }
    }

    /// <summary>
    /// Get JWT configuration from secrets
    /// </summary>
    public async Task<(string? Secret, string? Issuer, string? Audience)> GetJwtConfigAsync(CancellationToken cancellationToken = default)
    {
        var secret = await GetSecretAsync("jwt:secret", cancellationToken);
        var issuer = await GetSecretAsync("jwt:issuer", cancellationToken);
        var audience = await GetSecretAsync("jwt:audience", cancellationToken);

        return (secret, issuer, audience);
    }

    /// <summary>
    /// Get database connection string from secrets
    /// </summary>
    public async Task<string?> GetDatabaseConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        return await GetSecretAsync("database:connectionString", cancellationToken);
    }

    /// <summary>
    /// Get RabbitMQ connection string from secrets
    /// </summary>
    public async Task<string?> GetRabbitMQConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        return await GetSecretAsync("rabbitmq:connectionString", cancellationToken);
    }
}
