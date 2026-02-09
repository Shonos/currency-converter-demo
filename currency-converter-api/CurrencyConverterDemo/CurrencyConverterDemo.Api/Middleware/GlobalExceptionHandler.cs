using CurrencyConverterDemo.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverterDemo.Api.Middleware;

/// <summary>
/// Global exception handler that converts exceptions to ProblemDetails responses.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "An error occurred: {Message}", exception.Message);

        var (statusCode, title, detail) = exception switch
        {
            CurrencyNotSupportedException ex => (
                StatusCodes.Status400BadRequest,
                "Bad Request",
                $"Currency '{ex.CurrencyCode}' is not supported for conversion. Excluded currencies: TRY, PLN, THB, MXN."
            ),
            ExternalApiException ex => (
                StatusCodes.Status502BadGateway,
                "Bad Gateway",
                "External service is currently unavailable. Please try again later."
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred. Please try again later."
            )
        };

        context.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = $"https://tools.ietf.org/html/rfc9110#section-15.{statusCode / 100}.{statusCode % 100}",
            Instance = context.Request.Path
        };

        // Add trace ID if available
        if (context.TraceIdentifier != null)
        {
            problemDetails.Extensions["traceId"] = context.TraceIdentifier;
        }

        await context.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
