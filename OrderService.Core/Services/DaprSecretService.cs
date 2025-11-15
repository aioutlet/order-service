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
            _logger.LogDebug("Retrieving secret: {SecretName} from store: {StoreName}", secretName, SecretStoreName);

            // With nestedSeparator configured in Dapr, request the full key path directly
            // Dapr will handle the nested structure and return the specific value
            var secrets = await _daprClient.GetSecretAsync(
                SecretStoreName,
                secretName,
                cancellationToken: cancellationToken);

            if (secrets == null || secrets.Count == 0)
            {
                var errorMessage = $"Secret '{secretName}' not found in Dapr secret store '{SecretStoreName}'";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            // Dapr returns a dictionary with a single key-value pair for the requested secret
            var value = secrets.FirstOrDefault().Value;
            if (string.IsNullOrEmpty(value))
            {
                var errorMessage = $"Secret '{secretName}' has no value in Dapr secret store '{SecretStoreName}'";
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
