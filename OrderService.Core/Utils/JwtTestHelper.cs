using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace OrderService.Core.Utils;

/// <summary>
/// Helper class for generating JWT tokens for testing purposes
/// </summary>
public static class JwtTestHelper
{
    // Use a consistent test key that matches the test configuration
    private const string TestSecretKey = "test-secret-key-that-is-at-least-32-characters-long-for-security";

    /// <summary>
    /// Generates a test JWT token for the specified user
    /// </summary>
    /// <param name="userId">User ID to include in the token</param>
    /// <param name="role">Role to assign (e.g., "customer", "admin")</param>
    /// <param name="expiryMinutes">Token expiry in minutes (default: 60)</param>
    /// <returns>JWT token string</returns>
    public static string GenerateTestToken(string userId, string role, int expiryMinutes = 60)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role),
            new Claim("sub", userId),
            new Claim("role", role)
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Gets the test secret key used for token generation
    /// </summary>
    public static string GetTestSecretKey() => TestSecretKey;
}
