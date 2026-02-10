using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CurrencyConverterDemo.Tests.Fixtures;

public static class TestJwtTokenGenerator
{
    private const string Secret = "TestSecret-minimum-32-characters-long-key-here-for-testing!!";
    private const string Issuer = "TestIssuer";
    private const string Audience = "TestAudience";

    public static string GenerateToken(
        string username = "testuser", 
        string role = "User",
        string? clientId = null,
        int expirationMinutes = 60)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, role),
            new("client_id", clientId ?? $"test-client-{username}")
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string GenerateAdminToken(string username = "admin")
    {
        return GenerateToken(username, "Admin");
    }

    public static string GenerateExpiredToken(string username = "expireduser")
    {
        return GenerateToken(username, "User", expirationMinutes: -10);
    }
}
