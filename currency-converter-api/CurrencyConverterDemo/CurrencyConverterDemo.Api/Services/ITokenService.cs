using CurrencyConverterDemo.Api.Models;

namespace CurrencyConverterDemo.Api.Services;

/// <summary>
/// Service for generating JWT tokens.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a JWT token for the specified user and role.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="role">The user's role.</param>
    /// <returns>A login response containing the token and expiration.</returns>
    LoginResponse GenerateToken(string username, string role);
}
