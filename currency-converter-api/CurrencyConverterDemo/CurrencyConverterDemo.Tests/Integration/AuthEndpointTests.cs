using System.Net;
using System.Net.Http.Json;
using CurrencyConverterDemo.Api.Models;
using CurrencyConverterDemo.Tests.Fixtures;
using FluentAssertions;

namespace CurrencyConverterDemo.Tests.Integration;

[Collection("Integration Tests")]
public class AuthEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuthEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _factory.WireMockServer.Reset(); // Clear any previous mock configurations
    }

    [Fact]
    public async Task Login_WithValidDemoCredentials_ReturnsToken()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "demo",
            Password = "Demo@1234"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<LoginResponse>();
        content.Should().NotBeNull();
        content!.Token.Should().NotBeNullOrEmpty();
        content.Username.Should().Be("demo");
        content.Role.Should().Be("User");
        content.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_WithValidAdminCredentials_ReturnsAdminToken()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "admin",
            Password = "Admin@1234"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<LoginResponse>();
        content.Should().NotBeNull();
        content!.Token.Should().NotBeNullOrEmpty();
        content.Username.Should().Be("admin");
        content.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns401()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "invaliduser",
            Password = "WrongPassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithEmptyUsername_Returns400()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "",
            Password = "Demo@1234"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithEmptyPassword_Returns400()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "demo",
            Password = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithMissingFields_Returns400()
    {
        // Arrange
        var loginRequest = new { };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_GeneratedToken_ContainsExpectedClaims()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "demo",
            Password = "Demo@1234"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var content = await response.Content.ReadFromJsonAsync<LoginResponse>();

        // Assert
        content.Should().NotBeNull();
        content!.Token.Should().NotBeNullOrEmpty();
        
        // Token should be a valid JWT (basic format check)
        var parts = content.Token.Split('.');
        parts.Should().HaveCount(3); // Header.Payload.Signature
    }

    [Fact]
    public async Task Login_TokenExpiration_IsValid()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "demo",
            Password = "Demo@1234"
        };
        var beforeLogin = DateTime.UtcNow;

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var content = await response.Content.ReadFromJsonAsync<LoginResponse>();
        var afterLogin = DateTime.UtcNow;

        // Assert
        content.Should().NotBeNull();
        content!.ExpiresAt.Should().BeAfter(beforeLogin);
        content.ExpiresAt.Should().BeAfter(afterLogin);
        content.ExpiresAt.Should().BeCloseTo(afterLogin.AddMinutes(60), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Login_ConsecutiveLogins_GenerateDifferentTokens()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Username = "demo",
            Password = "Demo@1234"
        };

        // Act
        var response1 = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var content1 = await response1.Content.ReadFromJsonAsync<LoginResponse>();

        await Task.Delay(100); // Small delay to ensure different timestamps

        var response2 = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var content2 = await response2.Content.ReadFromJsonAsync<LoginResponse>();

        // Assert
        content1.Should().NotBeNull();
        content2.Should().NotBeNull();
        content1!.Token.Should().NotBe(content2!.Token);
    }
}
