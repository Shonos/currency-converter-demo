# Sub-Task 05: Backend Observability & Logging

> **Context**: Use with `00-master.copilot.md`. **Depends on**: Sub-tasks 01, 02.

---

## Objective

Implement structured logging with Serilog, add request/response logging middleware with all required observability signals, and correlate internal API requests with Frankfurter API calls.

---

## 1. NuGet Packages

Install in **CurrencyConverterDemo.Api**:

```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Enrichers.Environment
dotnet add package Serilog.Enrichers.Thread
dotnet add package Serilog.Enrichers.ClientInfo
dotnet add package Serilog.Expressions           # For structured JSON output
```

---

## 2. Serilog Configuration

### 2.1 In `Program.cs` (Bootstrap)

```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

try
{
    Log.Information("Starting Currency Converter API");
    builder.Host.UseSerilog();

    // ... rest of builder configuration ...

    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = EnrichFromRequest;
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
```

### 2.2 `appsettings.json` Serilog Config

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/log-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {CorrelationId} {ClientId} {ClientIp} {Message:lj}{NewLine}{Exception}"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId", "WithClientIp"],
    "Properties": {
      "Application": "CurrencyConverterDemo.Api"
    }
  }
}
```

### 2.3 Environment-Specific Overrides

```json
// appsettings.Development.json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug"
    }
  }
}

// appsettings.Production.json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Warning"
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/log-.json",
          "rollingInterval": "Day",
          "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
        }
      }
    ]
  }
}
```

---

## 3. Correlation ID Middleware

### 3.1 Purpose

Every incoming HTTP request gets a correlation ID that follows through all internal calls (including to the Frankfurter API), enabling end-to-end request tracing.

### 3.2 Implementation

```csharp
// Middleware/CorrelationIdMiddleware.cs
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        // Use existing correlation ID from header or generate new one
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Activity.Current?.Id
            ?? Guid.NewGuid().ToString("N");

        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // Push to Serilog LogContext so all logs in this request include it
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
```

### 3.3 Propagate to Frankfurter API

Add a delegating handler that forwards the correlation ID to outgoing HTTP calls:

```csharp
// Infrastructure/Http/CorrelationIdDelegatingHandler.cs
public class CorrelationIdDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdDelegatingHandler(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString();

        if (!string.IsNullOrEmpty(correlationId))
        {
            request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
```

Register on the HttpClient:
```csharp
services.AddTransient<CorrelationIdDelegatingHandler>();

services.AddHttpClient<FrankfurterApiClient>(...)
    .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
    .AddResilienceHandler(...);
```

---

## 4. Request/Response Logging Middleware

### 4.1 Required Observability Signals per Request

Every request log entry must include:

| Signal              | Source                           | Log Property     |
|---------------------|----------------------------------|------------------|
| Client IP           | `HttpContext.Connection.RemoteIpAddress` | `ClientIp`       |
| Client ID           | JWT `client_id` claim            | `ClientId`       |
| HTTP Method         | `HttpContext.Request.Method`     | `RequestMethod`  |
| Endpoint            | `HttpContext.Request.Path`       | `RequestPath`    |
| Response Status     | `HttpContext.Response.StatusCode`| `StatusCode`     |
| Response Time       | Stopwatch measurement            | `ElapsedMs`      |
| Correlation ID      | From middleware                  | `CorrelationId`  |

### 4.2 Implementation

Use Serilog's `UseSerilogRequestLogging` with enrichment:

```csharp
app.UseSerilogRequestLogging(options =>
{
    // Customize the message template
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

    // Enrich the log event with additional properties
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("ClientIp",
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

        diagnosticContext.Set("ClientId",
            httpContext.User?.FindFirst("client_id")?.Value ?? "anonymous");

        diagnosticContext.Set("RequestMethod",
            httpContext.Request.Method);

        diagnosticContext.Set("RequestPath",
            httpContext.Request.Path.ToString());

        diagnosticContext.Set("UserAgent",
            httpContext.Request.Headers.UserAgent.ToString());

        diagnosticContext.Set("CorrelationId",
            httpContext.Items["CorrelationId"]?.ToString() ?? "none");
    };

    // Log level based on status code
    options.GetLevel = (httpContext, elapsed, ex) =>
    {
        if (ex != null) return LogEventLevel.Error;
        if (httpContext.Response.StatusCode >= 500) return LogEventLevel.Error;
        if (httpContext.Response.StatusCode >= 400) return LogEventLevel.Warning;
        if (elapsed > 5000) return LogEventLevel.Warning;  // Slow requests
        return LogEventLevel.Information;
    };
});
```

---

## 5. Frankfurter API Call Logging

### 5.1 HTTP Client Logging Handler

Log outgoing HTTP calls to Frankfurter with timing and correlation:

```csharp
// Infrastructure/Http/HttpLoggingDelegatingHandler.cs
public class HttpLoggingDelegatingHandler : DelegatingHandler
{
    private readonly ILogger<HttpLoggingDelegatingHandler> _logger;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        _logger.LogInformation(
            "Outgoing HTTP {Method} {Url}",
            request.Method, request.RequestUri);

        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            sw.Stop();

            _logger.LogInformation(
                "Frankfurter API responded {StatusCode} in {ElapsedMs}ms for {Method} {Url}",
                (int)response.StatusCode, sw.ElapsedMilliseconds,
                request.Method, request.RequestUri);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "Frankfurter API call failed after {ElapsedMs}ms for {Method} {Url}",
                sw.ElapsedMilliseconds, request.Method, request.RequestUri);
            throw;
        }
    }
}
```

### 5.2 Register Both Delegating Handlers

```csharp
services.AddTransient<CorrelationIdDelegatingHandler>();
services.AddTransient<HttpLoggingDelegatingHandler>();

services.AddHttpClient<FrankfurterApiClient>(...)
    .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
    .AddHttpMessageHandler<HttpLoggingDelegatingHandler>()
    .AddResilienceHandler(...);
```

---

## 6. Service-Level Logging

Add structured log statements at key points in the application:

### 6.1 `CurrencyService`

```csharp
_logger.LogInformation(
    "Getting latest rates for {BaseCurrency}", baseCurrency);

_logger.LogInformation(
    "Converting {Amount} {FromCurrency} to {ToCurrency}", amount, from, to);

_logger.LogWarning(
    "Excluded currency rejected: {CurrencyCode}", currencyCode);

_logger.LogInformation(
    "Fetching historical rates for {BaseCurrency} from {StartDate} to {EndDate}, page {Page}",
    baseCurrency, startDate, endDate, page);
```

### 6.2 `CachedCurrencyProvider`

```csharp
_logger.LogDebug("Cache hit for key {CacheKey}", cacheKey);
_logger.LogDebug("Cache miss for key {CacheKey}. Fetching from provider.", cacheKey);
_logger.LogWarning("Circuit open. Returning stale cache for {CacheKey}", cacheKey);
```

### 6.3 Resilience Events

```csharp
// In resilience handler configuration
builder.AddRetry(new HttpRetryStrategyOptions
{
    OnRetry = args =>
    {
        var logger = args.Context.ServiceProvider.GetService<ILogger<Program>>();
        logger?.LogWarning(
            "Retry attempt {AttemptNumber} for Frankfurter API after {RetryDelay}ms. Outcome: {Outcome}",
            args.AttemptNumber, args.RetryDelay.TotalMilliseconds, args.Outcome?.Result?.StatusCode);
        return default;
    }
});

builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
{
    OnOpened = args =>
    {
        var logger = args.Context.ServiceProvider.GetService<ILogger<Program>>();
        logger?.LogError(
            "Circuit breaker OPENED for Frankfurter API. Break duration: {BreakDuration}s",
            args.BreakDuration.TotalSeconds);
        return default;
    },
    OnClosed = args =>
    {
        var logger = args.Context.ServiceProvider.GetService<ILogger<Program>>();
        logger?.LogInformation("Circuit breaker CLOSED for Frankfurter API.");
        return default;
    },
    OnHalfOpened = args =>
    {
        var logger = args.Context.ServiceProvider.GetService<ILogger<Program>>();
        logger?.LogInformation("Circuit breaker HALF-OPENED for Frankfurter API. Testing...");
        return default;
    }
});
```

---

## 7. Health Check Endpoint (Bonus)

Add a health check that verifies Frankfurter API connectivity:

```csharp
builder.Services.AddHealthChecks()
    .AddUrlGroup(
        new Uri("https://api.frankfurter.dev/v1/currencies"),
        name: "frankfurter-api",
        failureStatus: HealthStatus.Degraded,
        timeout: TimeSpan.FromSeconds(5));

// In pipeline
app.MapHealthChecks("/health");
```

---

## 8. Middleware Pipeline Order (Updated)

```csharp
var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();  // 1. Correlation ID (first!)
app.UseExceptionHandler();                     // 2. Exception handling
app.UseSerilogRequestLogging(...);             // 3. Request logging
app.UseSwaggerConfiguration();                 // 4. Swagger
app.UseCors("AllowFrontend");                  // 5. CORS
app.UseRateLimiter();                          // 6. Rate limiting
app.UseAuthentication();                       // 7. Authentication
app.UseAuthorization();                        // 8. Authorization

app.MapControllers();
app.MapHealthChecks("/health");
app.Run();
```

---

## 9. File Structure Summary

```
Api/
├── Middleware/
│   └── CorrelationIdMiddleware.cs
├── Extensions/
│   └── ObservabilityExtensions.cs         # Serilog + health check registration

Infrastructure/
├── Http/
│   ├── CorrelationIdDelegatingHandler.cs
│   └── HttpLoggingDelegatingHandler.cs
```

---

## 10. Expected Log Output Example

### Request Log (JSON in production)
```json
{
  "Timestamp": "2024-02-06T14:30:00.123Z",
  "Level": "Information",
  "Message": "HTTP GET /api/v1/exchange-rates/latest responded 200 in 45.2 ms",
  "Properties": {
    "CorrelationId": "abc123def456",
    "ClientIp": "192.168.1.100",
    "ClientId": "admin",
    "RequestMethod": "GET",
    "RequestPath": "/api/v1/exchange-rates/latest",
    "StatusCode": 200,
    "ElapsedMs": 45.2,
    "UserAgent": "Mozilla/5.0...",
    "Application": "CurrencyConverterDemo.Api"
  }
}
```

### Correlated Frankfurter Call Log
```json
{
  "Timestamp": "2024-02-06T14:30:00.089Z",
  "Level": "Information",
  "Message": "Frankfurter API responded 200 in 34ms for GET https://api.frankfurter.dev/v1/latest?base=EUR",
  "Properties": {
    "CorrelationId": "abc123def456",
    "StatusCode": 200,
    "ElapsedMs": 34,
    "Method": "GET",
    "Url": "https://api.frankfurter.dev/v1/latest?base=EUR"
  }
}
```

---

## 11. Acceptance Criteria

- [ ] Serilog replaces default .NET logging
- [ ] Console output uses structured template in development
- [ ] File output uses rolling daily logs
- [ ] Every request log includes: ClientIp, ClientId, Method, Path, StatusCode, ElapsedMs
- [ ] CorrelationId is generated for each request and included in all logs
- [ ] CorrelationId is propagated to Frankfurter API calls via `X-Correlation-Id` header
- [ ] CorrelationId is returned in response headers
- [ ] Outgoing Frankfurter API calls are logged with timing
- [ ] Cache hits/misses are logged at Debug level
- [ ] Retry attempts are logged at Warning level
- [ ] Circuit breaker state changes are logged
- [ ] Slow requests (> 5s) are logged at Warning level
- [ ] 4xx responses are logged at Warning level
- [ ] 5xx responses are logged at Error level
- [ ] Health check endpoint at `/health` works
- [ ] Log configuration is environment-specific (verbose in Dev, minimal in Prod)

---

## 12. Notes for Agent

- Serilog must be configured **before** the host is built (two-stage initialization).
- `UseSerilogRequestLogging()` must be placed **after** `CorrelationIdMiddleware` but **before** routing.
- Use `LogContext.PushProperty` for request-scoped properties — requires `Enrich.FromLogContext()`.
- Register `IHttpContextAccessor` for the delegating handlers to access the current request.
- **Do NOT** log sensitive data (JWT tokens, passwords, secrets).
- Use named log properties (`{BaseCurrency}`) not string interpolation (`$"{baseCurrency}"`).
