using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CurrencyConverterDemo.Application.DTOs;
using CurrencyConverterDemo.Tests.Fixtures;
using FluentAssertions;

namespace CurrencyConverterDemo.Tests.Integration;

[Collection("Integration Tests")]
public class CurrenciesEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public CurrenciesEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _factory.WireMockServer.Reset(); // Clear any previous mock configurations
    }

    [Fact]
    public async Task GetCurrencies_WithValidToken_Returns200WithCurrencies()
    {
        // Arrange
        FrankfurterMockServer.SetupCurrencies(_factory.WireMockServer);
        var token = TestJwtTokenGenerator.GenerateToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/currencies");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<CurrenciesResponse>();
        content.Should().NotBeNull();
        content!.Currencies.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetCurrencies_ExcludesProhibitedCurrencies()
    {
        // Arrange
        FrankfurterMockServer.SetupCurrencies(_factory.WireMockServer);
        var token = TestJwtTokenGenerator.GenerateToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/currencies");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<CurrenciesResponse>();
        content.Should().NotBeNull();
        
        // Should NOT contain excluded currencies
        content!.Currencies.Should().NotContain(c => c.Code == "TRY");
        content.Currencies.Should().NotContain(c => c.Code == "PLN");
        content.Currencies.Should().NotContain(c => c.Code == "THB");
        content.Currencies.Should().NotContain(c => c.Code == "MXN");
        
        // Should contain allowed currencies
        content.Currencies.Should().Contain(c => c.Code == "USD");
        content.Currencies.Should().Contain(c => c.Code == "EUR");
        content.Currencies.Should().Contain(c => c.Code == "GBP");
    }

    [Fact]
    public async Task GetCurrencies_ReturnsOrderedByCode()
    {
        // Arrange
        FrankfurterMockServer.SetupCurrencies(_factory.WireMockServer);
        var token = TestJwtTokenGenerator.GenerateToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/currencies");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<CurrenciesResponse>();
        content.Should().NotBeNull();
        content!.Currencies.Should().BeInAscendingOrder(c => c.Code);
    }

    [Fact]
    public async Task GetCurrencies_WithoutToken_SucceedsAsAnonymous()
    {
        // Arrange
        FrankfurterMockServer.SetupCurrencies(_factory.WireMockServer);

        // Act
        var response = await _client.GetAsync("/api/v1/currencies");

        // Assert - Currencies endpoint allows anonymous access
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<CurrenciesResponse>();
        content.Should().NotBeNull();
        content!.Currencies.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetCurrencies_WithAdminRole_Returns200()
    {
        // Arrange
        FrankfurterMockServer.SetupCurrencies(_factory.WireMockServer);
        var token = TestJwtTokenGenerator.GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/currencies");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCurrencies_CurrencyNames_AreIncluded()
    {
        // Arrange
        FrankfurterMockServer.SetupCurrencies(_factory.WireMockServer);
        var token = TestJwtTokenGenerator.GenerateToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/currencies");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<CurrenciesResponse>();
        content.Should().NotBeNull();
        
        foreach (var currency in content!.Currencies)
        {
            currency.Code.Should().NotBeNullOrEmpty();
            currency.Name.Should().NotBeNullOrEmpty();
        }
    }
}
