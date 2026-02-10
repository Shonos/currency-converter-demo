using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CurrencyConverterDemo.Api.Models;
using CurrencyConverterDemo.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace CurrencyConverterDemo.Tests.Unit.Api;

public class TokenServiceTests
{
    private readonly JwtSettings _validSettings = new()
    {
        Secret = "TestSecret-minimum-32-characters-long-key-here-for-testing!!",
        Issuer = "TestIssuer",
        Audience = "TestAudience",
        ExpirationMinutes = 60
    };

    [Fact]
    public void GenerateToken_WithValidCredentials_ReturnsValidToken()
    {
        // Arrange
        var options = Options.Create(_validSettings);
        var service = new TokenService(options);

        // Act
        var result = service.GenerateToken("testuser", "User");

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrEmpty();
        result.Username.Should().Be("testuser");
        result.Role.Should().Be("User");
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void GenerateToken_TokenIsValidJwt()
    {
        // Arrange
        var options = Options.Create(_validSettings);
        var service = new TokenService(options);

        // Act
        var result = service.GenerateToken("testuser", "Admin");

        // Assert
        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(result.Token).Should().BeTrue();
        
        var token = handler.ReadJwtToken(result.Token);
        token.Should().NotBeNull();
    }

    [Fact]
    public void GenerateToken_ContainsCorrectClaims()
    {
        // Arrange
        var options = Options.Create(_validSettings);
        var service = new TokenService(options);

        // Act
        var result = service.GenerateToken("johndoe", "Admin");

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.Token);
        
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == "johndoe");
        token.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        token.Claims.Should().Contain(c => c.Type == "client_id" && c.Value == "johndoe");
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
        token.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Iat);
    }

    [Fact]
    public void GenerateToken_HasCorrectExpiration()
    {
        // Arrange
        var options = Options.Create(_validSettings);
        var service = new TokenService(options);
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var result = service.GenerateToken("testuser", "User");
        var afterGeneration = DateTime.UtcNow;

        // Assert
        var expectedExpiration = beforeGeneration.AddMinutes(_validSettings.ExpirationMinutes);
        result.ExpiresAt.Should().BeCloseTo(expectedExpiration, TimeSpan.FromSeconds(5));
        result.ExpiresAt.Should().BeAfter(afterGeneration);
    }

    [Fact]
    public void GenerateToken_TokenSignatureIsValid()
    {
        // Arrange
        var options = Options.Create(_validSettings);
        var service = new TokenService(options);

        // Act
        var result = service.GenerateToken("testuser", "User");

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(result.Token);
        
        token.Issuer.Should().Be(_validSettings.Issuer);
        token.Audiences.Should().Contain(_validSettings.Audience);
        token.SignatureAlgorithm.Should().Be("HS256");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GenerateToken_WithEmptyUsername_ThrowsArgumentException(string username)
    {
        // Arrange
        var options = Options.Create(_validSettings);
        var service = new TokenService(options);

        // Act
        var act = () => service.GenerateToken(username, "User");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GenerateToken_WithEmptyRole_ThrowsArgumentException(string role)
    {
        // Arrange
        var options = Options.Create(_validSettings);
        var service = new TokenService(options);

        // Act
        var act = () => service.GenerateToken("testuser", role);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithShortSecret_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidSettings = new JwtSettings
        {
            Secret = "short", // Less than 32 characters
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpirationMinutes = 60
        };
        var options = Options.Create(invalidSettings);

        // Act
        var act = () => new TokenService(options);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*at least 32 characters*");
    }

    [Fact]
    public void GenerateToken_EachTokenHasUniqueJti()
    {
        // Arrange
        var options = Options.Create(_validSettings);
        var service = new TokenService(options);

        // Act
        var token1 = service.GenerateToken("user1", "User");
        var token2 = service.GenerateToken("user1", "User");

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwt1 = handler.ReadJwtToken(token1.Token);
        var jwt2 = handler.ReadJwtToken(token2.Token);

        var jti1 = jwt1.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jti2 = jwt2.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        jti1.Should().NotBe(jti2);
    }
}
