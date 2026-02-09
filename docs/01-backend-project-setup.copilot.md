# Sub-Task 01: Backend Project Setup & Architecture

> **Context**: Use with `master.copilot.md`. This task has **no dependencies** and should be executed first.

---

## Objective

Set up the Clean Architecture solution structure, define domain abstractions, configure dependency injection, and implement the factory pattern for currency providers.

---

## 1. Solution Structure

The existing solution has `CurrencyConverterDemo.Api`. You must create additional class library projects:

```
CurrencyConverterDemo/
├── CurrencyConverterDemo.slnx
├── CurrencyConverterDemo.Api/              # EXISTS – Web API host
├── CurrencyConverterDemo.Application/      # NEW – Business logic
├── CurrencyConverterDemo.Domain/           # NEW – Domain models & interfaces
├── CurrencyConverterDemo.Infrastructure/   # NEW – External services
└── CurrencyConverterDemo.Tests/            # NEW – All tests
```

### Commands to Create Projects

```bash
cd currency-converter-api/CurrencyConverterDemo

# Create class libraries
dotnet new classlib -n CurrencyConverterDemo.Domain -f net10.0
dotnet new classlib -n CurrencyConverterDemo.Application -f net10.0
dotnet new classlib -n CurrencyConverterDemo.Infrastructure -f net10.0
dotnet new xunit -n CurrencyConverterDemo.Tests -f net10.0

# Add to solution
dotnet sln CurrencyConverterDemo.slnx add CurrencyConverterDemo.Domain
dotnet sln CurrencyConverterDemo.slnx add CurrencyConverterDemo.Application
dotnet sln CurrencyConverterDemo.slnx add CurrencyConverterDemo.Infrastructure
dotnet sln CurrencyConverterDemo.slnx add CurrencyConverterDemo.Tests

# Set up project references (dependency rule: inner layers don't reference outer)
cd CurrencyConverterDemo.Application
dotnet add reference ../CurrencyConverterDemo.Domain

cd ../CurrencyConverterDemo.Infrastructure
dotnet add reference ../CurrencyConverterDemo.Domain
dotnet add reference ../CurrencyConverterDemo.Application

cd ../CurrencyConverterDemo.Api
dotnet add reference ../CurrencyConverterDemo.Application
dotnet add reference ../CurrencyConverterDemo.Infrastructure

cd ../CurrencyConverterDemo.Tests
dotnet add reference ../CurrencyConverterDemo.Api
dotnet add reference ../CurrencyConverterDemo.Application
dotnet add reference ../CurrencyConverterDemo.Domain
dotnet add reference ../CurrencyConverterDemo.Infrastructure
```

---

## 2. Domain Layer (`CurrencyConverterDemo.Domain`)

### 2.1 Models

```
Domain/
├── Models/
│   ├── Currency.cs                 # Currency value object (Code, Name)
│   ├── ExchangeRate.cs             # Single rate: BaseCurrency, TargetCurrency, Rate, Date
│   ├── ExchangeRateResult.cs       # API result: Base, Date, Rates dictionary
│   ├── ConversionResult.cs         # From, To, Amount, ConvertedAmount, Rate, Date
│   ├── HistoricalRateResult.cs     # StartDate, EndDate, Base, Rates by date
│   └── PagedResult.cs              # Generic: Items<T>, Page, PageSize, TotalCount, TotalPages
├── Enums/
│   └── CurrencyProviderType.cs     # Enum: Frankfurter, (future providers)
├── Constants/
│   └── ExcludedCurrencies.cs       # Static list: TRY, PLN, THB, MXN
└── Interfaces/
    ├── ICurrencyProvider.cs         # Core provider interface
    └── ICurrencyProviderFactory.cs  # Factory interface
```

### 2.2 `ICurrencyProvider` Interface

```csharp
namespace CurrencyConverterDemo.Domain.Interfaces;

public interface ICurrencyProvider
{
    /// <summary>Provider identifier for factory selection.</summary>
    string ProviderName { get; }

    /// <summary>Get latest exchange rates for a base currency.</summary>
    Task<ExchangeRateResult> GetLatestRatesAsync(
        string baseCurrency,
        CancellationToken cancellationToken = default);

    /// <summary>Convert amount between two currencies.</summary>
    Task<ConversionResult> ConvertCurrencyAsync(
        string fromCurrency,
        string toCurrency,
        decimal amount,
        CancellationToken cancellationToken = default);

    /// <summary>Get historical rates for a date range.</summary>
    Task<HistoricalRateResult> GetHistoricalRatesAsync(
        string baseCurrency,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);

    /// <summary>Get list of all supported currencies.</summary>
    Task<IReadOnlyDictionary<string, string>> GetCurrenciesAsync(
        CancellationToken cancellationToken = default);
}
```

### 2.3 `ICurrencyProviderFactory` Interface

```csharp
namespace CurrencyConverterDemo.Domain.Interfaces;

public interface ICurrencyProviderFactory
{
    ICurrencyProvider GetProvider(CurrencyProviderType providerType);
    ICurrencyProvider GetDefaultProvider();
}
```

### 2.4 `ExcludedCurrencies` Constants

```csharp
namespace CurrencyConverterDemo.Domain.Constants;

public static class ExcludedCurrencies
{
    public static readonly HashSet<string> Codes = new(StringComparer.OrdinalIgnoreCase)
    {
        "TRY", "PLN", "THB", "MXN"
    };

    public static bool IsExcluded(string currencyCode)
        => Codes.Contains(currencyCode);
}
```

### 2.5 `PagedResult<T>` Generic Model

```csharp
namespace CurrencyConverterDemo.Domain.Models;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
```

---

## 3. Application Layer (`CurrencyConverterDemo.Application`)

### 3.1 Structure

```
Application/
├── Services/
│   ├── ICurrencyService.cs          # Application service interface
│   └── CurrencyService.cs           # Orchestrates provider + business rules
├── DTOs/
│   ├── LatestRatesResponse.cs
│   ├── ConversionRequest.cs
│   ├── ConversionResponse.cs
│   ├── HistoricalRatesRequest.cs
│   └── HistoricalRatesResponse.cs
├── Validators/
│   └── CurrencyValidator.cs         # Validates excluded currencies
├── Exceptions/
│   ├── CurrencyNotSupportedException.cs
│   └── ExternalApiException.cs
└── Mappings/
    └── CurrencyMappings.cs          # Domain ↔ DTO mapping (manual or AutoMapper)
```

### 3.2 `CurrencyService` Responsibilities

- Accept requests from the API layer
- Validate business rules (excluded currencies)
- Delegate to `ICurrencyProvider` via factory
- Handle pagination for historical rates (the Frankfurter API returns all dates; pagination is done in-memory)
- Map domain models to response DTOs

### 3.3 Pagination Logic for Historical Rates

The Frankfurter API returns **all dates** in a time series. We implement server-side pagination:

```csharp
// Pseudocode
var allRates = await provider.GetHistoricalRatesAsync(base, startDate, endDate);
var allDates = allRates.Rates.Keys.OrderBy(d => d).ToList();
var pagedDates = allDates.Skip((page - 1) * pageSize).Take(pageSize);
var pagedRates = allDates.Where(d => pagedDates.Contains(d));
return new PagedResult<DailyRate>
{
    Items = pagedRates,
    Page = page,
    PageSize = pageSize,
    TotalCount = allDates.Count
};
```

---

## 4. Infrastructure Layer (`CurrencyConverterDemo.Infrastructure`)

### 4.1 Structure

```
Infrastructure/
├── Providers/
│   └── Frankfurter/
│       ├── FrankfurterCurrencyProvider.cs    # Implements ICurrencyProvider
│       ├── FrankfurterApiClient.cs           # Typed HttpClient
│       ├── FrankfurterOptions.cs             # Config: BaseUrl, Timeout
│       └── Models/                           # Raw API response models
│           ├── FrankfurterLatestResponse.cs
│           ├── FrankfurterTimeSeriesResponse.cs
│           └── FrankfurterCurrenciesResponse.cs
├── Factories/
│   └── CurrencyProviderFactory.cs            # Implements ICurrencyProviderFactory
├── Extensions/
│   └── InfrastructureServiceExtensions.cs    # DI registration
└── Configuration/
    └── FrankfurterSettings.cs                # Bound from appsettings
```

### 4.2 Frankfurter API Client

Use a **typed HttpClient** registered via `IHttpClientFactory`:

```csharp
// Registration in DI
services.AddHttpClient<FrankfurterApiClient>(client =>
{
    client.BaseAddress = new Uri(settings.BaseUrl);  // https://api.frankfurter.dev/v1/
    client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
});
```

### 4.3 Frankfurter Raw Response Models

Map directly from the Frankfurter API JSON format:

```csharp
// GET /latest?base=EUR
public class FrankfurterLatestResponse
{
    public decimal Amount { get; set; }
    public string Base { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public Dictionary<string, decimal> Rates { get; set; } = new();
}

// GET /2024-01-01..2024-01-31?base=EUR
public class FrankfurterTimeSeriesResponse
{
    public decimal Amount { get; set; }
    public string Base { get; set; } = string.Empty;
    public string Start_date { get; set; } = string.Empty;
    public string End_date { get; set; } = string.Empty;
    public Dictionary<string, Dictionary<string, decimal>> Rates { get; set; } = new();
}
```

### 4.4 Factory Pattern Implementation

```csharp
public class CurrencyProviderFactory : ICurrencyProviderFactory
{
    private readonly IEnumerable<ICurrencyProvider> _providers;
    private readonly CurrencyProviderType _defaultProvider;

    public CurrencyProviderFactory(
        IEnumerable<ICurrencyProvider> providers,
        IOptions<CurrencyProviderSettings> options)
    {
        _providers = providers;
        _defaultProvider = options.Value.DefaultProvider;
    }

    public ICurrencyProvider GetProvider(CurrencyProviderType providerType)
    {
        return _providers.FirstOrDefault(p =>
            p.ProviderName == providerType.ToString())
            ?? throw new InvalidOperationException($"Provider '{providerType}' not registered.");
    }

    public ICurrencyProvider GetDefaultProvider() => GetProvider(_defaultProvider);
}
```

---

## 5. API Layer Updates (`CurrencyConverterDemo.Api`)

### 5.1 Clean Up Existing Scaffolding

- Remove `WeatherForecast.cs` and `WeatherForecastController.cs` (and V2 variant)
- Keep `ApiVersioningExtensions.cs` and `SwaggerExtensions.cs`

### 5.2 Update `Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

// Layer registrations
builder.Services.AddDomainServices();           // If any
builder.Services.AddApplicationServices();       // CurrencyService, validators
builder.Services.AddInfrastructureServices(builder.Configuration);  // HttpClient, providers, factory

// Cross-cutting (later sub-tasks will add more)
builder.Services.AddControllers();
builder.Services.AddApiVersioningServices();

var app = builder.Build();
app.UseSwaggerConfiguration();
app.MapControllers();
app.Run();
```

---

## 6. Configuration (`appsettings.json`)

Add the following section:

```json
{
  "CurrencyProvider": {
    "DefaultProvider": "Frankfurter",
    "Frankfurter": {
      "BaseUrl": "https://api.frankfurter.dev/v1/",
      "TimeoutSeconds": 30
    }
  }
}
```

---

## 7. NuGet Packages to Install

### Domain
- (none – keep it dependency-free)

### Application
- `FluentValidation` (optional, for request validation)

### Infrastructure
- `Microsoft.Extensions.Http` (for IHttpClientFactory)
- `Microsoft.Extensions.Options.ConfigurationExtensions`

### Api (already has some)
- Keep existing packages

### Tests (created later in sub-task 06)
- `xunit`, `Moq` or `NSubstitute`, `FluentAssertions`, `Microsoft.AspNetCore.Mvc.Testing`

---

## 8. Acceptance Criteria

- [ ] Solution builds with `dotnet build` from solution root
- [ ] All 4 projects created and referenced correctly
- [ ] `ICurrencyProvider` interface defined in Domain
- [ ] `ICurrencyProviderFactory` interface defined in Domain
- [ ] `FrankfurterCurrencyProvider` implements `ICurrencyProvider`
- [ ] `CurrencyProviderFactory` implements `ICurrencyProviderFactory`
- [ ] `CurrencyService` in Application layer orchestrates business logic
- [ ] DI is wired up — provider factory resolves correctly
- [ ] `ExcludedCurrencies` constant class exists with TRY, PLN, THB, MXN
- [ ] `PagedResult<T>` generic model exists
- [ ] Weather forecast scaffolding removed
- [ ] Solution compiles with zero warnings

---

## 9. Notes for Agent

- **Do NOT implement** resilience (caching, retry, circuit breaker) — that's sub-task 03.
- **Do NOT implement** authentication/security — that's sub-task 04.
- **Do NOT implement** logging/observability — that's sub-task 05.
- **Do NOT write tests** — that's sub-task 06.
- Focus on **clean interfaces**, **proper DI**, and **correct project references**.
- Use `CancellationToken` on all async methods.
- Enable nullable reference types on all new projects.
