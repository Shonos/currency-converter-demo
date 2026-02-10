using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CurrencyConverterDemo.Application.DTOs;
using CurrencyConverterDemo.Tests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverterDemo.Tests.Integration;

[Collection("Integration Tests")]
public class ExchangeRatesEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ExchangeRatesEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _factory.WireMockServer.Reset(); // Clear any previous mock configurations
    }

    [Fact]
    public async Task GetLatestRates_WithValidToken_Returns200WithRates()
    {
        // Arrange
        FrankfurterMockServer.SetupLatestRates(_factory.WireMockServer, "EUR");
        var token = TestJwtTokenGenerator.GenerateToken(role: "User");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/exchange-rates/latest?baseCurrency=EUR");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<LatestRatesResponse>();
        content.Should().NotBeNull();
        content!.Base.Should().Be("EUR");
        content.Rates.Should().NotBeEmpty();
        content.Rates.Should().ContainKey("USD");
    }

    [Fact]
    public async Task GetLatestRates_WithoutToken_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/exchange-rates/latest?baseCurrency=EUR");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ConvertCurrency_WithValidRequest_Returns200WithConversion()
    {
        // Arrange
        FrankfurterMockServer.SetupConversion(_factory.WireMockServer, "EUR", "USD", 100);
        var token = TestJwtTokenGenerator.GenerateToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/exchange-rates/convert?from=EUR&to=USD&amount=100");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<ConversionResponse>();
        content.Should().NotBeNull();
        content!.From.Should().Be("EUR");
        content.To.Should().Be("USD");
        content.Amount.Should().Be(100m);
    }

    [Fact]
    public async Task ConvertCurrency_WithExcludedCurrency_Returns400()
    {
        // Arrange
        var token = TestJwtTokenGenerator.GenerateToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/exchange-rates/convert?from=EUR&to=TRY&amount=100");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Detail.Should().Contain("TRY");
    }

    [Theory]
    [InlineData("PLN")]
    [InlineData("THB")]
    [InlineData("MXN")]
    public async Task ConvertCurrency_WithOtherExcludedCurrencies_Returns400(string excludedCurrency)
    {
        // Arrange
        var token = TestJwtTokenGenerator.GenerateToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/v1/exchange-rates/convert?from=USD&to={excludedCurrency}&amount=100");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetHistoricalRates_WithValidRequest_Returns200WithPaginatedData()
    {
        // Arrange
        FrankfurterMockServer.SetupTimeSeries(_factory.WireMockServer, "2024-01-01", "2024-01-10", "EUR");
        var token = TestJwtTokenGenerator.GenerateToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync(
            "/api/v1/exchange-rates/history?baseCurrency=EUR&startDate=2024-01-01&endDate=2024-01-10&page=1&pageSize=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<HistoricalRatesResponse>();
        content.Should().NotBeNull();
        content!.Base.Should().Be("EUR");
        content.Page.Should().Be(1);
        content.PageSize.Should().Be(2);
        content.Rates.Should().HaveCountLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetHistoricalRates_PaginationMetadata_IsCorrect()
    {
        // Arrange
        FrankfurterMockServer.SetupTimeSeries(_factory.WireMockServer, "2024-01-01", "2024-01-10", "EUR");
        var token = TestJwtTokenGenerator.GenerateToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync(
            "/api/v1/exchange-rates/history?baseCurrency=EUR&startDate=2024-01-01&endDate=2024-01-10&page=1&pageSize=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<HistoricalRatesResponse>();
        content.Should().NotBeNull();
        content!.TotalCount.Should().BeGreaterThan(0);
        content.TotalPages.Should().BeGreaterThan(0);
        content.HasNextPage.Should().Be(content.Page < content.TotalPages);
        content.HasPreviousPage.Should().Be(content.Page > 1);
    }

    [Fact]
    public async Task GetLatestRates_WithAdminRole_Returns200()
    {
        // Arrange
        FrankfurterMockServer.SetupLatestRates(_factory.WireMockServer, "USD");
        var token = TestJwtTokenGenerator.GenerateAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/exchange-rates/latest?baseCurrency=USD");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ConvertCurrency_WithMissingAmount_Returns400()
    {
        // Arrange
        var token = TestJwtTokenGenerator.GenerateToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/exchange-rates/convert?from=EUR&to=USD");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConvertCurrency_WithZeroAmount_Returns400()
    {
        // Arrange
        var token = TestJwtTokenGenerator.GenerateToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/exchange-rates/convert?from=EUR&to=USD&amount=0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ConvertCurrency_WithNegativeAmount_Returns400()
    {
        // Arrange
        var token = TestJwtTokenGenerator.GenerateToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/exchange-rates/convert?from=EUR&to=USD&amount=-100");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
