# Sub-Task 04: Backend Security

> **Context**: Use with `master.copilot.md`. **Depends on**: Sub-tasks 01, 02.

---

## Objective

Secure the API with JWT Bearer authentication, implement role-based access control (RBAC), and add API rate limiting to protect against abuse.

---

## 1. NuGet Packages

Install in **CurrencyConverterDemo.Api**:

```bash
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package System.IdentityModel.Tokens.Jwt
```

Rate limiting is built into ASP.NET Core (no extra package needed for .NET 7+).

---

## 2. JWT Authentication

### 2.1 Configuration

```json
// appsettings.json
{
  "JwtSettings": {
    "Secret": "CHANGE_ME_use-a-256-bit-key-minimum-32-characters!!",
    "Issuer": "CurrencyConverterDemo",
    "Audience": "CurrencyConverterDemo.Api",
    "ExpirationMinutes": 60
  }
}
```

**Environment-specific overrides**:
```json
// appsettings.Development.json
{
  "JwtSettings": {
    "Secret": "DevSecret-minimum-32-characters-long-key-here!!"
  }
}
```

```json
// appsettings.Production.json
{
  "JwtSettings": {
    "Secret": ""  // MUST be set via environment variable or secret manager
  }
}
```

### 2.2 JWT Service Registration

```csharp
// In a new file: Extensions/AuthenticationExtensions.cs
public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()!;

        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });

        services.AddAuthorization();
        return services;
    }
}
```

### 2.3 JWT Settings Model

```csharp
public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}
```

---

## 3. Auth Controller (Token Generation)

For this demo, implement a simple auth endpoint (no real user store — use hardcoded demo users or in-memory):

```
Controllers/v1/AuthController.cs
```

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController : ControllerBase
{
    // POST /api/v1/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        // Validate credentials (hardcoded demo users)
        // Generate and return JWT token
    }
}
```

### 3.1 Demo Users

```csharp
// In-memory user store for demo purposes
private static readonly Dictionary<string, (string Password, string Role)> DemoUsers = new()
{
    ["admin"] = ("Admin123!", "Admin"),
    ["user"] = ("User123!", "User"),
    ["viewer"] = ("Viewer123!", "Viewer")
};
```

### 3.2 Login Request / Response

```csharp
public class LoginRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string Role { get; set; } = string.Empty;
}
```

### 3.3 Token Generation Service

```csharp
public interface ITokenService
{
    LoginResponse GenerateToken(string username, string role);
}

public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;

    public LoginResponse GenerateToken(string username, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role),
            new Claim("client_id", username),  // Used for observability
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiration,
            signingCredentials: credentials);

        return new LoginResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expiration,
            Role = role
        };
    }
}
```

---

## 4. Role-Based Access Control (RBAC)

### 4.1 Roles

| Role    | Access                                                    |
|---------|-----------------------------------------------------------|
| Admin   | All endpoints + future admin operations                   |
| User    | All currency endpoints (latest, convert, history)         |
| Viewer  | Latest rates only (read-only, no conversion/history)      |

### 4.2 Authorization Policies

```csharp
services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole",
        policy => policy.RequireRole("Admin"));

    options.AddPolicy("RequireUserRole",
        policy => policy.RequireRole("Admin", "User"));

    options.AddPolicy("RequireViewerRole",
        policy => policy.RequireRole("Admin", "User", "Viewer"));
});
```

### 4.3 Apply to Controllers

```csharp
[Authorize(Policy = "RequireViewerRole")]
[HttpGet("latest")]
public async Task<ActionResult<LatestRatesResponse>> GetLatestRates(...)

[Authorize(Policy = "RequireUserRole")]
[HttpGet("convert")]
public async Task<ActionResult<ConversionResponse>> ConvertCurrency(...)

[Authorize(Policy = "RequireUserRole")]
[HttpGet("history")]
public async Task<ActionResult<PagedHistoricalRatesResponse>> GetHistoricalRates(...)
```

The `CurrenciesController` remains `[AllowAnonymous]`.

---

## 5. API Rate Limiting

### 5.1 Configuration

```json
{
  "RateLimiting": {
    "Fixed": {
      "PermitLimit": 100,
      "WindowSeconds": 60
    },
    "Sliding": {
      "PermitLimit": 30,
      "WindowSeconds": 60,
      "SegmentsPerWindow": 6
    }
  }
}
```

### 5.2 Implementation

Use ASP.NET Core built-in rate limiting:

```csharp
// Extensions/RateLimitingExtensions.cs
public static class RateLimitingExtensions
{
    public static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Global fixed window rate limit
            options.AddFixedWindowLimiter("fixed", opt =>
            {
                opt.PermitLimit = 100;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 5;
            });

            // Per-user sliding window (based on JWT client_id)
            options.AddSlidingWindowLimiter("per-user", opt =>
            {
                opt.PermitLimit = 30;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.SegmentsPerWindow = 6;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 2;
            });

            // Custom response for rate limit exceeded
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = 429;
                context.HttpContext.Response.ContentType = "application/problem+json";

                var problem = new ProblemDetails
                {
                    Status = 429,
                    Title = "Too Many Requests",
                    Detail = "Rate limit exceeded. Please try again later."
                };

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString();
                    problem.Detail += $" Retry after {(int)retryAfter.TotalSeconds} seconds.";
                }

                await context.HttpContext.Response.WriteAsJsonAsync(problem, token);
            };
        });

        return services;
    }
}
```

### 5.3 Apply Rate Limiting

```csharp
// On specific controllers or globally
[EnableRateLimiting("per-user")]
[ApiController]
[Route("api/v{version:apiVersion}/exchange-rates")]
public class ExchangeRatesController : ControllerBase
```

Or globally in pipeline:
```csharp
app.UseRateLimiter();
```

---

## 6. Middleware Pipeline Order

The order in `Program.cs` is critical:

```csharp
var app = builder.Build();

app.UseExceptionHandler();           // 1. Global exception handling
app.UseSwaggerConfiguration();       // 2. Swagger (dev only ideally)
app.UseCors("AllowFrontend");        // 3. CORS
app.UseRateLimiter();                // 4. Rate limiting
app.UseAuthentication();             // 5. Authentication (JWT validation)
app.UseAuthorization();              // 6. Authorization (RBAC)

app.MapControllers();
app.Run();
```

---

## 7. Swagger JWT Support

Update Swagger to support JWT Bearer token input:

```csharp
// In SwaggerExtensions.cs – add security definition
options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
{
    Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
    Name = "Authorization",
    In = ParameterLocation.Header,
    Type = SecuritySchemeType.ApiKey,
    Scheme = "Bearer"
});

options.AddSecurityRequirement(new OpenApiSecurityRequirement
{
    {
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        },
        Array.Empty<string>()
    }
});
```

---

## 8. HTTP File for Testing

Update `api.http` with auth flow:

```http
### Login as admin
POST {{baseUrl}}/api/v1/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "Admin123!"
}

### Login as user
POST {{baseUrl}}/api/v1/auth/login
Content-Type: application/json

{
  "username": "user",
  "password": "User123!"
}

### Get latest rates (with token)
GET {{baseUrl}}/api/v1/exchange-rates/latest?baseCurrency=EUR
Authorization: Bearer {{token}}

### Convert currency (should work for User/Admin)
GET {{baseUrl}}/api/v1/exchange-rates/convert?from=EUR&to=USD&amount=100
Authorization: Bearer {{token}}

### Convert excluded currency (should return 400)
GET {{baseUrl}}/api/v1/exchange-rates/convert?from=EUR&to=TRY&amount=100
Authorization: Bearer {{token}}

### Unauthorized request (should return 401)
GET {{baseUrl}}/api/v1/exchange-rates/latest?baseCurrency=EUR
```

---

## 9. File Structure Summary

```
Api/
├── Extensions/
│   ├── AuthenticationExtensions.cs
│   └── RateLimitingExtensions.cs
├── Controllers/
│   └── v1/
│       ├── AuthController.cs
│       ├── CurrenciesController.cs     (already exists)
│       └── ExchangeRatesController.cs  (update: add [Authorize])
├── Services/
│   ├── ITokenService.cs
│   └── TokenService.cs
└── Models/
    ├── JwtSettings.cs
    ├── LoginRequest.cs
    └── LoginResponse.cs
```

---

## 10. Acceptance Criteria

- [ ] JWT authentication is configured and validates tokens
- [ ] `POST /api/v1/auth/login` generates valid JWT tokens for demo users
- [ ] Token contains `username`, `role`, `client_id`, `jti`, `iat` claims
- [ ] Protected endpoints return 401 without a valid token
- [ ] Protected endpoints return 403 when role is insufficient
- [ ] `Admin` can access all endpoints
- [ ] `User` can access latest, convert, and history
- [ ] `Viewer` can only access latest rates
- [ ] `/api/v1/currencies` is accessible without authentication
- [ ] Rate limiting returns 429 with `Retry-After` header when exceeded
- [ ] Rate limiting uses `ProblemDetails` format for error response
- [ ] Swagger UI has a "Bearer" authorization button
- [ ] JWT secret is configurable per environment
- [ ] Middleware pipeline order is correct
- [ ] Solution compiles and all existing endpoints still work with valid tokens

---

## 11. Notes for Agent

- This is a **demo application** — hardcoded users are acceptable. In production, you'd use Identity/OAuth2.
- The JWT secret in `appsettings.json` is for development only. Production should use env vars or Azure Key Vault.
- **Do NOT add** logging to auth events here — sub-task 05 handles that.
- Ensure the `[Authorize]` and policy attributes are added to the controllers from sub-task 02.
- Rate limiting should be **per-user** (based on JWT `client_id` claim) for authenticated endpoints.
- The `CurrenciesController` must remain public (`[AllowAnonymous]`) for frontend dropdown population.
