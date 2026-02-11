using CurrencyConverterDemo.Api.Extensions;
using CurrencyConverterDemo.Api.Filters;
using CurrencyConverterDemo.Api.Middleware;
using CurrencyConverterDemo.Api.Models;
using CurrencyConverterDemo.Api.Services;
using CurrencyConverterDemo.Application.Extensions;
using CurrencyConverterDemo.Infrastructure.Extensions;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.AddSerilogLogging();

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
builder.Services.AddControllers(options =>
{
    // Add token blacklist filter globally
    options.Filters.Add<TokenBlacklistFilter>();
});
builder.Services.AddApiVersioningServices();

// Add exception handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Add health checks
builder.Services.AddApiHealthChecks(builder.Configuration);

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
app.UseMiddleware<CorrelationIdMiddleware>();  // 1. Correlation ID (must be first!)
app.UseExceptionHandler();                     // 2. Global exception handling

// Add Serilog request logging with enrichment
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

app.UseSwaggerConfiguration();                 // 3. Swagger/OpenAPI (dev only)
app.UseCors("AllowFrontend");                  // 4. CORS
app.UseRateLimiter();                          // 5. Rate limiting
app.UseAuthentication();                       // 6. Authentication (JWT validation)
app.UseAuthorization();                        // 7. Authorization (RBAC)

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/detailed", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds,
                exception = e.Value.Exception?.Message,
                data = e.Value.Data
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        });
        await context.Response.WriteAsync(result);
    }
});

app.Run();
