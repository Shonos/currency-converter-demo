using CurrencyConverterDemo.Api.Extensions;
using CurrencyConverterDemo.Api.Middleware;
using CurrencyConverterDemo.Api.Models;
using CurrencyConverterDemo.Api.Services;
using CurrencyConverterDemo.Application.Extensions;
using CurrencyConverterDemo.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add layer services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add security services
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddApiRateLimiting(builder.Configuration);

// Configure demo users from environment/configuration
builder.Services.Configure<DemoUserSettings>(options =>
{
    options.Users = builder.Configuration.GetValue<string>("DemoUsers") ?? string.Empty;
});

// Add application services
builder.Services.AddScoped<ITokenService, TokenService>();

// Add cross-cutting concerns
builder.Services.AddControllers();
builder.Services.AddApiVersioningServices();

// Add exception handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigin = builder.Configuration.GetValue<string>("Cors:AllowedOrigin") 
            ?? "http://localhost:5173";
        
        policy.WithOrigins(allowedOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure middleware pipeline (order is critical!)
app.UseExceptionHandler();           // 1. Global exception handling
app.UseSwaggerConfiguration();       // 2. Swagger/OpenAPI (dev only)
app.UseCors("AllowFrontend");        // 3. CORS
app.UseRateLimiter();                // 4. Rate limiting
app.UseAuthentication();             // 5. Authentication (JWT validation)
app.UseAuthorization();              // 6. Authorization (RBAC)

app.MapControllers();

app.Run();