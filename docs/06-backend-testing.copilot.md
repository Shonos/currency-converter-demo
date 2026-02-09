# Sub-Task 06: Backend Testing

> **Context**: Use with `master.copilot.md`. **Depends on**: Sub-tasks 01–05 (all backend implementation must be complete).

---

## Objective

Achieve ≥90% unit test coverage across the backend, write integration tests for API endpoints and external API interactions, and configure coverage reporting.

---

## 1. Test Project Setup

The test project `CurrencyConverterDemo.Tests` was created in sub-task 01.

### 1.1 NuGet Packages

```bash
cd CurrencyConverterDemo.Tests

dotnet add package xunit
dotnet add package xunit.runner.visualstudio
dotnet add package Microsoft.NET.Test.Sdk
dotnet add package Moq                                # Or NSubstitute
dotnet add package FluentAssertions
dotnet add package Microsoft.AspNetCore.Mvc.Testing   # Integration tests
dotnet add package WireMock.Net                        # Mock Frankfurter API
dotnet add package coverlet.collector                  # Code coverage
dotnet add package coverlet.msbuild                    # Coverage via MSBuild
dotnet add package ReportGenerator                     # HTML coverage reports
```

### 1.2 Project Structure

```
CurrencyConverterDemo.Tests/
├── Unit/
│   ├── Domain/
│   │   ├── ExcludedCurrenciesTests.cs
│   │   └── PagedResultTests.cs
│   ├── Application/
│   │   ├── CurrencyServiceTests.cs
│   │   └── CurrencyValidatorTests.cs
│   ├── Infrastructure/
│   │   ├── FrankfurterCurrencyProviderTests.cs
│   │   ├── FrankfurterApiClientTests.cs
│   │   ├── CurrencyProviderFactoryTests.cs
│   │   └── CachedCurrencyProviderTests.cs
│   └── Api/
│       ├── AuthControllerTests.cs
│       ├── ExchangeRatesControllerTests.cs
│       ├── CurrenciesControllerTests.cs
│       └── TokenServiceTests.cs
├── Integration/
│   ├── ApiIntegrationTestBase.cs
│   ├── ExchangeRatesEndpointTests.cs
│   ├── AuthEndpointTests.cs
│   ├── CurrenciesEndpointTests.cs
│   ├── HistoricalRatesEndpointTests.cs
│   └── FrankfurterApiIntegrationTests.cs
├── Fixtures/
│   ├── CustomWebApplicationFactory.cs
│   ├── FrankfurterMockServer.cs
│   └── TestJwtTokenGenerator.cs
└── TestData/
    ├── FrankfurterLatestResponse.json
    ├── FrankfurterTimeSeriesResponse.json
    └── FrankfurterCurrenciesResponse.json
```

---

## 2. Unit Tests

### 2.1 Domain Tests

#### `ExcludedCurrenciesTests.cs`
```csharp
public class ExcludedCurrenciesTests
{
    [Theory]
    [InlineData("TRY")]
    [InlineData("PLN")]
    [InlineData("THB")]
    [InlineData("MXN")]
    [InlineData("try")]  // Case insensitive
    [InlineData("Pln")]
    public void IsExcluded_ExcludedCurrency_ReturnsTrue(string code)
    {
        ExcludedCurrencies.IsExcluded(code).Should().BeTrue();
    }

    [Theory]
    [InlineData("EUR")]
    [InlineData("USD")]
    [InlineData("GBP")]
    public void IsExcluded_AllowedCurrency_ReturnsFalse(string code)
    {
        ExcludedCurrencies.IsExcluded(code).Should().BeFalse();
    }
}
```

#### `PagedResultTests.cs`
```csharp
// Test TotalPages calculation, HasNextPage, HasPreviousPage for various scenarios
// Edge cases: empty list, single page, exact page boundary
```

### 2.2 Application Tests

#### `CurrencyServiceTests.cs` – Core test class

Test cases to cover:

**GetLatestRates:**
- ✅ Returns rates from provider for valid base currency
- ✅ Returns rates for different base currencies (USD, GBP, etc.)

**ConvertCurrency:**
- ✅ Converts correctly between valid currencies
- ✅ Throws `CurrencyNotSupportedException` for TRY as source
- ✅ Throws `CurrencyNotSupportedException` for PLN as target
- ✅ Throws `CurrencyNotSupportedException` for THB as source
- ✅ Throws `CurrencyNotSupportedException` for MXN as target
- ✅ Throws for amount ≤ 0
- ✅ Throws for same source and target currency
- ✅ Conversion calculation is mathematically correct

**GetHistoricalRates:**
- ✅ Returns paginated results correctly (page 1 of 3)
- ✅ Returns last page with fewer items
- ✅ Pagination metadata is correct (totalCount, totalPages, hasNext, hasPrevious)
- ✅ Throws for invalid date range (start > end)
- ✅ Throws for date range exceeding 365 days
- ✅ Empty result for date range with no data

**GetCurrencies:**
- ✅ Returns currencies from provider
- ✅ Excludes TRY, PLN, THB, MXN from results

```csharp
public class CurrencyServiceTests
{
    private readonly Mock<ICurrencyProviderFactory> _factoryMock;
    private readonly Mock<ICurrencyProvider> _providerMock;
    private readonly CurrencyService _sut;

    public CurrencyServiceTests()
    {
        _providerMock = new Mock<ICurrencyProvider>();
        _factoryMock = new Mock<ICurrencyProviderFactory>();
        _factoryMock.Setup(f => f.GetDefaultProvider()).Returns(_providerMock.Object);
        _sut = new CurrencyService(_factoryMock.Object);
    }

    [Fact]
    public async Task ConvertCurrency_WithExcludedSourceCurrency_ThrowsCurrencyNotSupportedException()
    {
        // Arrange & Act
        var act = () => _sut.ConvertCurrencyAsync("TRY", "USD", 100, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<CurrencyNotSupportedException>()
            .WithMessage("*TRY*not supported*");
    }

    // ... more tests
}
```

### 2.3 Infrastructure Tests

#### `FrankfurterCurrencyProviderTests.cs`
- Mock `HttpMessageHandler` to return predefined JSON responses
- Test JSON deserialization of Frankfurter response format
- Test handling of HTTP errors (404, 500)
- Test timeout handling

#### `CachedCurrencyProviderTests.cs`
- ✅ Cache miss → calls inner provider and caches result
- ✅ Cache hit → returns cached result without calling inner provider
- ✅ Cache expiration → calls inner provider again
- ✅ Circuit open → returns stale cache if available
- ✅ Circuit open + no cache → throws ExternalApiException

#### `CurrencyProviderFactoryTests.cs`
- ✅ Returns correct provider for known type
- ✅ Returns default provider
- ✅ Throws for unknown provider type

### 2.4 API / Controller Tests

#### `ExchangeRatesControllerTests.cs`
- Mock `ICurrencyService`
- Test HTTP status codes (200, 400, 401, 403, 500)
- Test response format matches DTO schema
- Test query parameter binding

#### `AuthControllerTests.cs`
- ✅ Login with valid credentials returns token
- ✅ Login with invalid credentials returns 401
- ✅ Login with missing fields returns 400
- ✅ Generated token contains expected claims

#### `TokenServiceTests.cs`
- ✅ Generated token is valid JWT
- ✅ Token contains correct claims (name, role, client_id)
- ✅ Token has correct expiration
- ✅ Token signature is valid

---

## 3. Integration Tests

### 3.1 `CustomWebApplicationFactory`

```csharp
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private WireMockServer? _wireMockServer;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _wireMockServer = WireMockServer.Start();

        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CurrencyProvider:Frankfurter:BaseUrl"] = _wireMockServer.Url + "/v1/",
                ["JwtSettings:Secret"] = "TestSecret-minimum-32-characters-long-key-here!!",
                ["JwtSettings:Issuer"] = "TestIssuer",
                ["JwtSettings:Audience"] = "TestAudience",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace any services needed for testing
        });
    }

    public WireMockServer WireMockServer => _wireMockServer!;

    protected override void Dispose(bool disposing)
    {
        _wireMockServer?.Stop();
        base.Dispose(disposing);
    }
}
```

### 3.2 `TestJwtTokenGenerator`

```csharp
public static class TestJwtTokenGenerator
{
    public static string GenerateToken(string username = "testuser", string role = "User")
    {
        // Generate a valid JWT for integration test authentication
        // Use the same secret as CustomWebApplicationFactory
    }
}
```

### 3.3 `FrankfurterMockServer`

```csharp
public static class FrankfurterMockServer
{
    public static void SetupLatestRates(WireMockServer server, string baseCurrency = "EUR")
    {
        server.Given(
            Request.Create()
                .WithPath("/v1/latest")
                .WithParam("base", baseCurrency)
                .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBodyFromFile("TestData/FrankfurterLatestResponse.json"));
    }

    public static void SetupTimeSeries(WireMockServer server, string startDate, string endDate)
    {
        // Similar setup for time series endpoint
    }

    public static void SetupServerError(WireMockServer server)
    {
        server.Given(Request.Create().UsingGet())
            .RespondWith(Response.Create().WithStatusCode(500));
    }
}
```

### 3.4 Integration Test Examples

#### `ExchangeRatesEndpointTests.cs`

```csharp
public class ExchangeRatesEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    [Fact]
    public async Task GetLatestRates_WithValidToken_Returns200WithRates()
    {
        // Arrange
        FrankfurterMockServer.SetupLatestRates(_factory.WireMockServer);
        var token = TestJwtTokenGenerator.GenerateToken(role: "User");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/exchange-rates/latest?baseCurrency=EUR");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<LatestRatesResponse>();
        content.Should().NotBeNull();
        content!.BaseCurrency.Should().Be("EUR");
        content.Rates.Should().NotBeEmpty();
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
        problem!.Detail.Should().Contain("TRY");
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
    public async Task GetHistoricalRates_Paginated_ReturnsCorrectPageMetadata()
    {
        // Arrange
        FrankfurterMockServer.SetupTimeSeries(_factory.WireMockServer, "2024-01-01", "2024-01-31");
        var token = TestJwtTokenGenerator.GenerateToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync(
            "/api/v1/exchange-rates/history?baseCurrency=EUR&startDate=2024-01-01&endDate=2024-01-31&page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<PagedHistoricalRatesResponse>();
        content!.Page.Should().Be(1);
        content.PageSize.Should().Be(10);
        content.TotalCount.Should().BeGreaterThan(0);
        content.HasNextPage.Should().BeTrue();
    }
}
```

#### `FrankfurterApiIntegrationTests.cs`

Optional tests that hit the **real** Frankfurter API (marked with a trait to skip in CI):

```csharp
[Trait("Category", "ExternalApi")]
public class FrankfurterApiIntegrationTests
{
    [Fact]
    public async Task RealApi_GetLatestRates_ReturnsValidResponse()
    {
        // Hits the actual Frankfurter API to verify our deserialization
        var client = new HttpClient { BaseAddress = new Uri("https://api.frankfurter.dev/v1/") };
        var response = await client.GetFromJsonAsync<FrankfurterLatestResponse>("latest?base=EUR");
        response.Should().NotBeNull();
        response!.Base.Should().Be("EUR");
        response.Rates.Should().ContainKey("USD");
    }
}
```

---

## 4. Test Data Files

### `TestData/FrankfurterLatestResponse.json`
```json
{
  "amount": 1.0,
  "base": "EUR",
  "date": "2024-02-06",
  "rates": {
    "AUD": 1.688,
    "BRL": 6.1767,
    "CAD": 1.6118,
    "CHF": 0.9175,
    "CNY": 8.1838,
    "GBP": 0.8679,
    "JPY": 185.27,
    "USD": 1.1794
  }
}
```

### `TestData/FrankfurterTimeSeriesResponse.json`
```json
{
  "amount": 1.0,
  "base": "EUR",
  "start_date": "2024-01-02",
  "end_date": "2024-01-31",
  "rates": {
    "2024-01-02": { "AUD": 1.6147, "USD": 1.0956 },
    "2024-01-03": { "AUD": 1.6236, "USD": 1.0919 },
    "2024-01-04": { "AUD": 1.628, "USD": 1.0953 },
    "2024-01-05": { "AUD": 1.6301, "USD": 1.0945 },
    "2024-01-08": { "AUD": 1.6339, "USD": 1.0951 },
    "2024-01-09": { "AUD": 1.6389, "USD": 1.0962 },
    "2024-01-10": { "AUD": 1.6455, "USD": 1.0972 },
    "2024-01-11": { "AUD": 1.6489, "USD": 1.0976 },
    "2024-01-12": { "AUD": 1.6512, "USD": 1.0978 },
    "2024-01-15": { "AUD": 1.6545, "USD": 1.098 },
    "2024-01-16": { "AUD": 1.6578, "USD": 1.0886 },
    "2024-01-17": { "AUD": 1.6612, "USD": 1.0875 },
    "2024-01-18": { "AUD": 1.6645, "USD": 1.0886 },
    "2024-01-19": { "AUD": 1.6678, "USD": 1.089 },
    "2024-01-22": { "AUD": 1.671, "USD": 1.0893 },
    "2024-01-23": { "AUD": 1.6743, "USD": 1.0879 },
    "2024-01-24": { "AUD": 1.6776, "USD": 1.0888 },
    "2024-01-25": { "AUD": 1.681, "USD": 1.0853 },
    "2024-01-26": { "AUD": 1.6843, "USD": 1.0849 },
    "2024-01-29": { "AUD": 1.6876, "USD": 1.0838 },
    "2024-01-30": { "AUD": 1.691, "USD": 1.0842 },
    "2024-01-31": { "AUD": 1.6943, "USD": 1.0812 }
  }
}
```

---

## 5. Running Tests & Coverage

### 5.1 Run All Tests

```bash
dotnet test CurrencyConverterDemo.Tests --logger "console;verbosity=detailed"
```

### 5.2 Run with Coverage

```bash
dotnet test CurrencyConverterDemo.Tests \
    /p:CollectCoverage=true \
    /p:CoverletOutputFormat=cobertura \
    /p:CoverletOutput=./TestResults/coverage.cobertura.xml \
    /p:Exclude="[*.Tests]*"
```

### 5.3 Generate HTML Report

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool

reportgenerator \
    -reports:./TestResults/coverage.cobertura.xml \
    -targetdir:./TestResults/CoverageReport \
    -reporttypes:Html
```

### 5.4 Exclude from Coverage

Exclude auto-generated and non-testable code:
```xml
<!-- In CurrencyConverterDemo.Tests.csproj -->
<PropertyGroup>
  <CollectCoverage>true</CollectCoverage>
  <CoverletOutputFormat>cobertura</CoverletOutputFormat>
  <Exclude>
    [CurrencyConverterDemo.Api]CurrencyConverterDemo.Api.Program,
    [*]*.Migrations.*,
    [*.Tests]*
  </Exclude>
</PropertyGroup>
```

---

## 6. Coverage Targets

| Project              | Target | Key Classes to Cover                                          |
|----------------------|--------|---------------------------------------------------------------|
| Domain               | 100%   | ExcludedCurrencies, PagedResult, all models                   |
| Application          | 95%+   | CurrencyService, validators, exceptions                      |
| Infrastructure       | 90%+   | FrankfurterCurrencyProvider, CachedCurrencyProvider, Factory  |
| Api (Controllers)    | 85%+   | All controllers, TokenService                                 |
| **Overall**          | **≥90%** |                                                             |

---

## 7. Acceptance Criteria

- [ ] All unit tests pass with `dotnet test`
- [ ] All integration tests pass (using WireMock, not real API)
- [ ] External API tests are tagged with `[Trait("Category", "ExternalApi")]` and can be excluded
- [ ] Overall code coverage is ≥ 90%
- [ ] Coverage report (HTML) can be generated via `reportgenerator`
- [ ] Test project structure follows the defined folder layout
- [ ] Mocks are clean and don't over-specify (test behavior, not implementation)
- [ ] Edge cases are covered (empty results, boundary values, excluded currencies)
- [ ] Integration tests verify HTTP status codes, response format, and content
- [ ] Auth integration tests verify 401/403 scenarios
- [ ] Pagination integration tests verify metadata correctness

---

## 8. Notes for Agent

- Use `FluentAssertions` for readable assertions.
- Prefer `Moq` (or `NSubstitute`) for mocking interfaces.
- Use `WireMock.Net` for integration tests — do NOT hit the real Frankfurter API in CI.
- Mark real API tests with `[Trait("Category", "ExternalApi")]` so they can be filtered out.
- Test data JSON files should match the real Frankfurter API response format exactly.
- Ensure `CustomWebApplicationFactory` uses the `Test` environment.
- The `TestJwtTokenGenerator` must produce tokens compatible with the test JWT configuration.
- Test the **behavior**, not the implementation. If a method is refactored, tests should still pass.
- Focus on covering the business-critical code paths first: excluded currencies, conversion logic, pagination.
