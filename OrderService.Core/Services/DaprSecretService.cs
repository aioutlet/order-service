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
    /// <returns>Secret value</returns>
    /// <exception cref="InvalidOperationException">Thrown when secret is not found</exception>
    public async Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
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
                var errorMessage = $"Secret '{secretKey}' not found in Dapr secret store '{SecretStoreName}'";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            // If nested key specified, look for it
            if (nestedKey != null)
            {
                if (secrets.ContainsKey(nestedKey))
                {
                    return secrets[nestedKey];
                }
                
                var errorMessage = $"Nested key '{nestedKey}' not found in secret '{secretKey}'";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            // Otherwise return the first value
            var value = secrets.FirstOrDefault().Value;
            if (string.IsNullOrEmpty(value))
            {
                var errorMessage = $"Secret '{secretKey}' has no value in Dapr secret store '{SecretStoreName}'";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            return value;
        }
        catch (InvalidOperationException)
        {
            // Re-throw our custom exceptions
            throw;
        }
        catch (Exception ex)
        {
            var errorMessage = $"Failed to retrieve secret '{secretName}' from Dapr: {ex.Message}";
            _logger.LogError(ex, errorMessage);
            throw new InvalidOperationException(errorMessage, ex);
        }
    }

    /// <summary>
    /// Get JWT configuration from secrets
    /// </summary>
    public async Task<(string Secret, string Issuer, string Audience)> GetJwtConfigAsync(CancellationToken cancellationToken = default)
    {
        var secret = await GetSecretAsync("jwt:secret", cancellationToken);
        var issuer = await GetSecretAsync("jwt:issuer", cancellationToken);
        var audience = await GetSecretAsync("jwt:audience", cancellationToken);

        return (secret, issuer, audience);
    }

    /// <summary>
    /// Get database connection string from secrets
    /// </summary>
    public async Task<string> GetDatabaseConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        return await GetSecretAsync("database:connectionString", cancellationToken);
    }
}
