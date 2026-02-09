using Asp.Versioning;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// 1. Setup API Versioning
builder.Services.AddApiVersioningServices();

var app = builder.Build();

// 3. Map OpenAPI and Swagger UI
app.UseSwaggerConfiguration();

app.MapControllers();
app.Run();