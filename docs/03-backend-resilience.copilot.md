# Sub-Task 03: Backend Resilience & Performance

> **Context**: Use with `00-master.copilot.md`. **Depends on**: Sub-tasks 01, 02.

---

## Objective

Add caching, retry policies with exponential backoff, and circuit breaker patterns to the Frankfurter API integration, ensuring the application handles upstream failures gracefully.

---

## 1. NuGet Packages

Install in **CurrencyConverterDemo.Infrastructure**:

```bash
dotnet add package Microsoft.Extensions.Http.Resilience   # Polly v8 integration
dotnet add package Microsoft.Extensions.Caching.Memory      # IMemoryCache
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis  # Optional: Redis for prod
```

---

## 2. Caching Strategy

### 2.1 What to Cache

| Data                  | Cache Duration | Cache Key Pattern                                    | Rationale                                    |
|-----------------------|----------------|------------------------------------------------------|----------------------------------------------|
| Latest rates          | 5 minutes      | `rates:latest:{baseCurrency}`                        | Rates update ~daily at 16:00 CET             |
| Currencies list       | 24 hours       | `currencies:list`                                    | Almost never changes                         |
| Historical rates      | 1 hour         | `rates:history:{base}:{startDate}:{endDate}`         | Historical data is immutable                 |
| Conversion result     | 5 minutes      | `rates:convert:{from}:{to}:{amount}`                 | Derived from latest rates                    |

### 2.2 Implementation – Caching Decorator

Use a **decorator pattern** around the `ICurrencyProvider`:

```
Infrastructure/
├── Caching/
│   ├── CachedCurrencyProvider.cs      # Decorator wrapping ICurrencyProvider
│   └── CacheKeyGenerator.cs           # Generates consistent cache keys
```

```csharp
public class CachedCurrencyProvider : ICurrencyProvider
{
    private readonly ICurrencyProvider _innerProvider;
    private readonly IMemoryCache _cache;
    private readonly CacheSettings _settings;

    public string ProviderName => _innerProvider.ProviderName;

    public async Task<ExchangeRateResult> GetLatestRatesAsync(
        string baseCurrency, CancellationToken ct)
    {
        var cacheKey = $"rates:latest:{baseCurrency.ToUpperInvariant()}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_settings.LatestRatesMinutes);
            return await _innerProvider.GetLatestRatesAsync(baseCurrency, ct);
        }) ?? await _innerProvider.GetLatestRatesAsync(baseCurrency, ct);
    }

    // Similar pattern for other methods...
}
```

### 2.3 Cache Settings Configuration

```json
// appsettings.json
{
  "CacheSettings": {
    "LatestRatesMinutes": 5,
    "CurrenciesListHours": 24,
    "HistoricalRatesMinutes": 60,
    "ConversionMinutes": 5
  }
}
```

### 2.4 DI Registration (Decorator Pattern)

```csharp
// Register the inner provider
services.AddSingleton<FrankfurterCurrencyProvider>();

// Register the cached decorator
services.AddSingleton<ICurrencyProvider>(sp =>
    new CachedCurrencyProvider(
        sp.GetRequiredService<FrankfurterCurrencyProvider>(),
        sp.GetRequiredService<IMemoryCache>(),
        sp.GetRequiredService<IOptions<CacheSettings>>().Value));
```

Or use Scrutor for automatic decoration:
```csharp
services.AddSingleton<ICurrencyProvider, FrankfurterCurrencyProvider>();
services.Decorate<ICurrencyProvider, CachedCurrencyProvider>();
```

---

## 3. Retry Policy with Exponential Backoff

### 3.1 Configuration

```json
{
  "ResilienceSettings": {
    "Retry": {
      "MaxRetryAttempts": 3,
      "BaseDelaySeconds": 1,
      "MaxDelaySeconds": 30,
      "UseJitter": true
    }
  }
}
```

### 3.2 Implementation with `Microsoft.Extensions.Http.Resilience`

Configure on the typed HttpClient for Frankfurter:

```csharp
services.AddHttpClient<FrankfurterApiClient>(client =>
{
    client.BaseAddress = new Uri(settings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
})
.AddResilienceHandler("frankfurter-resilience", (builder, context) =>
{
    var retrySettings = context.ServiceProvider
        .GetRequiredService<IOptions<ResilienceSettings>>().Value.Retry;

    // Retry with exponential backoff + jitter
    builder.AddRetry(new HttpRetryStrategyOptions
    {
        MaxRetryAttempts = retrySettings.MaxRetryAttempts,
        BackoffType = DelayBackoffType.Exponential,
        Delay = TimeSpan.FromSeconds(retrySettings.BaseDelaySeconds),
        MaxDelay = TimeSpan.FromSeconds(retrySettings.MaxDelaySeconds),
        UseJitter = retrySettings.UseJitter,
        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
            .Handle<HttpRequestException>()
            .Handle<TimeoutRejectedException>()
            .HandleResult(r => r.StatusCode >= System.Net.HttpStatusCode.InternalServerError)
    });

    // Circuit breaker
    builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
    {
        // Configured in section 4
    });

    // Request timeout
    builder.AddTimeout(TimeSpan.FromSeconds(10));
});
```

### 3.3 What Gets Retried

- HTTP 5xx responses from Frankfurter
- `HttpRequestException` (network errors)
- `TimeoutRejectedException`

### 3.4 What Does NOT Get Retried

- HTTP 4xx responses (client errors — our fault)
- `OperationCanceledException` (user cancelled)

---

## 4. Circuit Breaker

### 4.1 Configuration

```json
{
  "ResilienceSettings": {
    "CircuitBreaker": {
      "FailureRatio": 0.5,
      "SamplingDurationSeconds": 30,
      "MinimumThroughput": 5,
      "BreakDurationSeconds": 60
    }
  }
}
```

### 4.2 Implementation

```csharp
builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
{
    FailureRatio = cbSettings.FailureRatio,          // 50% failure threshold
    SamplingDuration = TimeSpan.FromSeconds(cbSettings.SamplingDurationSeconds),
    MinimumThroughput = cbSettings.MinimumThroughput, // At least 5 requests before evaluating
    BreakDuration = TimeSpan.FromSeconds(cbSettings.BreakDurationSeconds),
    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
        .Handle<HttpRequestException>()
        .Handle<TimeoutRejectedException>()
        .HandleResult(r => r.StatusCode >= System.Net.HttpStatusCode.InternalServerError)
});
```

### 4.3 Circuit Breaker States

```
Closed (normal) → Half-Open (testing) → Open (blocking)
       ↑                                      │
       └──────────────────────────────────────┘
                  (after break duration)
```

- **Closed**: All requests pass through normally
- **Open**: Requests fail immediately with `BrokenCircuitException`; returns cached data if available, else 503
- **Half-Open**: One test request allowed; if it succeeds, circuit closes

### 4.4 Graceful Degradation

When the circuit is open, the API should:
1. Return **cached data** if available (stale cache)
2. If no cache: return **HTTP 503 Service Unavailable** with a `Retry-After` header
3. Log the circuit breaker state transition

```csharp
// In CachedCurrencyProvider or service layer:
try
{
    return await _innerProvider.GetLatestRatesAsync(baseCurrency, ct);
}
catch (BrokenCircuitException)
{
    // Try stale cache
    if (_cache.TryGetValue(cacheKey, out ExchangeRateResult? staleResult))
    {
        _logger.LogWarning("Circuit open. Returning stale cached data for {CacheKey}", cacheKey);
        return staleResult;
    }
    throw new ExternalApiException("Exchange rate service is temporarily unavailable.");
}
```

---

## 5. Request Timeout

Add an outer timeout to prevent requests from hanging:

```csharp
// Already added in the resilience pipeline above
builder.AddTimeout(TimeSpan.FromSeconds(10));
```

The pipeline order matters: **Retry → Circuit Breaker → Timeout** (inner to outer):
```
Request → [Timeout] → [CircuitBreaker] → [Retry] → HttpClient
```

Wait, the correct Polly v8 order is outermost first:
```
Pipeline order (outermost → innermost):
1. Total request timeout (e.g., 30s)
2. Retry (with exponential backoff)
3. Circuit Breaker
4. Per-attempt timeout (e.g., 10s)
```

---

## 6. Resilience Settings – Full Configuration

```json
{
  "ResilienceSettings": {
    "Retry": {
      "MaxRetryAttempts": 3,
      "BaseDelaySeconds": 1,
      "MaxDelaySeconds": 30,
      "UseJitter": true
    },
    "CircuitBreaker": {
      "FailureRatio": 0.5,
      "SamplingDurationSeconds": 30,
      "MinimumThroughput": 5,
      "BreakDurationSeconds": 60
    },
    "Timeout": {
      "PerAttemptTimeoutSeconds": 10,
      "TotalTimeoutSeconds": 60
    }
  },
  "CacheSettings": {
    "LatestRatesMinutes": 5,
    "CurrenciesListHours": 24,
    "HistoricalRatesMinutes": 60,
    "ConversionMinutes": 5
  }
}
```

---

## 7. File Structure Summary

```
Infrastructure/
├── Caching/
│   ├── CachedCurrencyProvider.cs
│   ├── CacheKeyGenerator.cs
│   └── CacheSettings.cs
├── Resilience/
│   ├── ResilienceSettings.cs
│   └── ResilienceExtensions.cs          # Extension method for HttpClient resilience
├── Providers/
│   └── Frankfurter/
│       └── (existing files from sub-task 01)
└── Extensions/
    └── InfrastructureServiceExtensions.cs   # Updated with caching + resilience
```

---

## 8. Acceptance Criteria

- [ ] `IMemoryCache` is registered and used to cache all Frankfurter API responses
- [ ] Cache keys follow the defined patterns
- [ ] Cache durations match configuration
- [ ] Cache settings are configurable via `appsettings.json`
- [ ] Retry policy retries up to 3 times with exponential backoff + jitter
- [ ] Retry does NOT retry 4xx errors
- [ ] Circuit breaker opens after 50% failure rate in 30s window
- [ ] When circuit is open, stale cached data is returned if available
- [ ] When circuit is open and no cache, HTTP 503 is returned
- [ ] Per-attempt timeout of 10s is enforced
- [ ] Total request timeout of 60s is enforced
- [ ] All resilience settings are configurable (not hardcoded)
- [ ] Solution builds and existing endpoints still work

---

## 9. Notes for Agent

- Use `Microsoft.Extensions.Http.Resilience` (Polly v8 integration), NOT raw Polly v7 syntax.
- The resilience handler is registered on the `HttpClient`, not on individual service methods.
- The `CachedCurrencyProvider` decorator wraps the real provider — ensure DI registration order is correct.
- **Do NOT modify** controller logic — caching is transparent via the decorator.
- **Do NOT add** logging here — sub-task 05 will add resilience event logging.
- Test resilience by temporarily pointing to an invalid URL and verifying retry/circuit breaker behavior.
