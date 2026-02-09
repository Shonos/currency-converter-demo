using CurrencyConverterDemo.Api.Middleware;
using CurrencyConverterDemo.Application.Extensions;
using CurrencyConverterDemo.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add layer services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

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
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure middleware
app.UseExceptionHandler();
app.UseCors("AllowFrontend");
app.UseSwaggerConfiguration();
app.MapControllers();

app.Run();