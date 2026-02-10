using Serilog.Context;
using System.Diagnostics;

namespace CurrencyConverterDemo.Api.Middleware;

/// <summary>
/// Middleware that generates or extracts a correlation ID for each request
/// and propagates it through the request pipeline and to external services.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Use existing correlation ID from header or generate new one
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Activity.Current?.Id
            ?? Guid.NewGuid().ToString("N");

        // Store in HttpContext for access by other middleware/services
        context.Items["CorrelationId"] = correlationId;
        
        // Add to response headers for client tracking
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // Push to Serilog LogContext so all logs in this request include it
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
