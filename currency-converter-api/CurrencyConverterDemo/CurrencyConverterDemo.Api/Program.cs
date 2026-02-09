using CurrencyConverterDemo.Application.Extensions;
using CurrencyConverterDemo.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add layer services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add cross-cutting concerns
builder.Services.AddControllers();
builder.Services.AddApiVersioningServices();

var app = builder.Build();

// Configure middleware
app.UseSwaggerConfiguration();
app.MapControllers();

app.Run();