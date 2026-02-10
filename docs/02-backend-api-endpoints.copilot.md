# Sub-Task 02: Backend API Endpoints

> **Context**: Use with `00-master.copilot.md`. **Depends on**: Sub-task 01 (project setup must be complete).

---

## Objective

Implement three versioned API endpoints for currency operations: latest exchange rates, currency conversion, and paginated historical rates. Also implement a currencies list endpoint.

---

## 1. API Route Design

All endpoints are under API version 1, using URL path versioning:

| Method | Route                                  | Description                        | Auth Required |
|--------|----------------------------------------|------------------------------------|---------------|
| GET    | `/api/v1/currencies`                   | List all supported currencies      | No*           |
| GET    | `/api/v1/exchange-rates/latest`        | Latest rates for a base currency   | Yes           |
| GET    | `/api/v1/exchange-rates/convert`       | Convert between two currencies     | Yes           |
| GET    | `/api/v1/exchange-rates/history`       | Historical rates (paginated)       | Yes           |

*Currencies list is public so the frontend can populate dropdowns before login.

---

## 2. Controllers

### 2.1 `CurrenciesController`

```
Controllers/v1/CurrenciesController.cs
```

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/currencies")]
public class CurrenciesController : ControllerBase
{
    // GET /api/v1/currencies
    // Returns: { "AUD": "Australian Dollar", "EUR": "Euro", ... }
    // Excludes: TRY, PLN, THB, MXN from the list
}
```

### 2.2 `ExchangeRatesController`

```
Controllers/v1/ExchangeRatesController.cs
```

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/exchange-rates")]
[Authorize]  // Added in sub-task 04
public class ExchangeRatesController : ControllerBase
{
    private readonly ICurrencyService _currencyService;

    // GET /api/v1/exchange-rates/latest?baseCurrency=EUR
    [HttpGet("latest")]
    public async Task<ActionResult<LatestRatesResponse>> GetLatestRates(
        [FromQuery] string baseCurrency = "EUR",
        CancellationToken cancellationToken = default)

    // GET /api/v1/exchange-rates/convert?from=EUR&to=USD&amount=100
    [HttpGet("convert")]
    public async Task<ActionResult<ConversionResponse>> ConvertCurrency(
        [FromQuery] string from,
        [FromQuery] string to,
        [FromQuery] decimal amount,
        CancellationToken cancellationToken = default)

    // GET /api/v1/exchange-rates/history?baseCurrency=EUR&startDate=2024-01-01&endDate=2024-01-31&page=1&pageSize=10
    [HttpGet("history")]
    public async Task<ActionResult<PagedHistoricalRatesResponse>> GetHistoricalRates(
        [FromQuery] string baseCurrency,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
}
```

---

## 3. Request / Response DTOs

### 3.1 Latest Rates

**Response**: `LatestRatesResponse`
```json
{
  "baseCurrency": "EUR",
  "date": "2024-02-06",
  "rates": {
    "AUD": 1.688,
    "BRL": 6.1767,
    "CAD": 1.6118,
    "USD": 1.1794
  }
}
```

### 3.2 Currency Conversion

**Query Parameters**: `from` (required), `to` (required), `amount` (required, > 0)

**Response**: `ConversionResponse`
```json
{
  "from": "EUR",
  "to": "USD",
  "amount": 100.00,
  "convertedAmount": 117.94,
  "rate": 1.1794,
  "date": "2024-02-06"
}
```

**Error Response (excluded currency)**:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Currency 'TRY' is not supported for conversion. Excluded currencies: TRY, PLN, THB, MXN.",
  "traceId": "00-abc..."
}
```

### 3.3 Historical Rates (Paginated)

**Query Parameters**: `baseCurrency` (required), `startDate` (required), `endDate` (required), `page` (default 1), `pageSize` (default 10, max 50)

**Response**: `PagedHistoricalRatesResponse`
```json
{
  "baseCurrency": "EUR",
  "startDate": "2024-01-01",
  "endDate": "2024-01-31",
  "page": 1,
  "pageSize": 10,
  "totalCount": 23,
  "totalPages": 3,
  "hasNextPage": true,
  "hasPreviousPage": false,
  "rates": [
    {
      "date": "2024-01-02",
      "rates": {
        "AUD": 1.6147,
        "USD": 1.0956
      }
    },
    {
      "date": "2024-01-03",
      "rates": {
        "AUD": 1.6236,
        "USD": 1.0919
      }
    }
  ]
}
```

### 3.4 Currencies List

**Response**: `CurrenciesResponse`
```json
{
  "currencies": [
    { "code": "AUD", "name": "Australian Dollar" },
    { "code": "EUR", "name": "Euro" },
    { "code": "USD", "name": "United States Dollar" }
  ]
}
```

Note: Excluded currencies (TRY, PLN, THB, MXN) should be **filtered out** from this list.

---

## 4. Validation Rules

### 4.1 Common Validation
- Currency codes: must be non-empty, 3-letter uppercase alpha
- If currency code is in `ExcludedCurrencies.Codes`, return **HTTP 400**

### 4.2 Conversion-Specific
- `amount` must be > 0
- `from` and `to` must be different currencies
- Both `from` and `to` must not be in excluded list

### 4.3 Historical-Specific
- `startDate` must be before `endDate`
- Date range must not exceed 365 days (to prevent huge payloads)
- `page` must be ≥ 1
- `pageSize` must be between 1 and 50

### 4.4 Validation Implementation

Use a combination of:
- **Data annotations** on request DTOs (`[Required]`, `[Range]`)
- **FluentValidation** for complex rules (optional but recommended)
- **Manual checks** in the service layer for business rules (excluded currencies)

Return validation errors as `ProblemDetails`:
```csharp
return Problem(
    detail: "Currency 'TRY' is not supported for conversion.",
    statusCode: StatusCodes.Status400BadRequest,
    title: "Bad Request");
```

---

## 5. Application Service Methods

In `ICurrencyService` / `CurrencyService`:

```csharp
public interface ICurrencyService
{
    Task<LatestRatesResponse> GetLatestRatesAsync(
        string baseCurrency, CancellationToken ct);

    Task<ConversionResponse> ConvertCurrencyAsync(
        string from, string to, decimal amount, CancellationToken ct);

    Task<PagedHistoricalRatesResponse> GetHistoricalRatesAsync(
        string baseCurrency, DateOnly startDate, DateOnly endDate,
        int page, int pageSize, CancellationToken ct);

    Task<CurrenciesResponse> GetCurrenciesAsync(CancellationToken ct);
}
```

### Service Logic Flow

1. **Validate** inputs (excluded currencies, date ranges)
2. **Call** `ICurrencyProvider` via factory
3. **Paginate** (for historical – in-memory pagination of Frankfurter response)
4. **Map** domain models to response DTOs
5. **Return** response (or throw domain exception caught by middleware)

---

## 6. Exception Handling

### 6.1 Custom Exceptions

```csharp
public class CurrencyNotSupportedException : Exception
{
    public string CurrencyCode { get; }
    public CurrencyNotSupportedException(string currencyCode)
        : base($"Currency '{currencyCode}' is not supported for conversion.")
    {
        CurrencyCode = currencyCode;
    }
}
```

### 6.2 Global Exception Handler

Create middleware or use `IExceptionHandler` (.NET 8+):

```csharp
public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken ct)
    {
        var (statusCode, detail) = exception switch
        {
            CurrencyNotSupportedException ex => (400, ex.Message),
            ExternalApiException ex => (502, "External service is currently unavailable."),
            _ => (500, "An unexpected error occurred.")
        };

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = ReasonPhrases.GetReasonPhrase(statusCode),
            Detail = detail
        }, ct);
        return true;
    }
}
```

Register in `Program.cs`:
```csharp
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// In pipeline:
app.UseExceptionHandler();
```

---

## 7. CORS Configuration

Enable CORS for the React frontend:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetValue<string>("Cors:AllowedOrigin")
                    ?? "http://localhost:5173")  // Vite default
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// In pipeline (before auth):
app.UseCors("AllowFrontend");
```

---

## 8. Acceptance Criteria

- [ ] `GET /api/v1/currencies` returns filtered currency list (no TRY/PLN/THB/MXN)
- [ ] `GET /api/v1/exchange-rates/latest?baseCurrency=EUR` returns rates from Frankfurter
- [ ] `GET /api/v1/exchange-rates/convert?from=EUR&to=USD&amount=100` returns conversion result
- [ ] `GET /api/v1/exchange-rates/convert?from=EUR&to=TRY&amount=100` returns HTTP 400 with clear message
- [ ] `GET /api/v1/exchange-rates/convert?from=PLN&to=USD&amount=100` returns HTTP 400
- [ ] `GET /api/v1/exchange-rates/history?baseCurrency=EUR&startDate=2024-01-01&endDate=2024-01-31&page=1&pageSize=10` returns paginated results
- [ ] Pagination metadata (totalCount, totalPages, hasNext, hasPrevious) is correct
- [ ] Invalid inputs return `ProblemDetails` format errors
- [ ] Global exception handler catches and formats all errors
- [ ] CORS is configured for React frontend origin
- [ ] All endpoints appear in Swagger UI with correct documentation
- [ ] API versioning works (v1 prefix in URLs)

---

## 9. Notes for Agent

- **Do NOT add** `[Authorize]` attributes yet — that's sub-task 04. Leave a comment `// [Authorize] – added in security sub-task` for now.
- **Do NOT add** caching to controllers or services — that's sub-task 03.
- **Do NOT add** logging statements — that's sub-task 05.
- Focus on **correct routing**, **clean DTOs**, **validation**, and **proper HTTP status codes**.
- Use `ProblemDetails` consistently for all error responses.
- Ensure Swagger shows all endpoints with example request/response schemas.
- Test manually with the `.http` file or Swagger UI.
