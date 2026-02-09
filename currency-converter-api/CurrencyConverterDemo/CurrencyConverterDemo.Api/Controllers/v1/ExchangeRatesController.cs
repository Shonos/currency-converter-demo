using Asp.Versioning;
using CurrencyConverterDemo.Application.DTOs;
using CurrencyConverterDemo.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverterDemo.Api.Controllers.v1;

/// <summary>
/// Controller for currency exchange rate operations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/exchange-rates")]
// [Authorize] – Added in security sub-task
public class ExchangeRatesController : ControllerBase
{
    private readonly ICurrencyService _currencyService;

    public ExchangeRatesController(ICurrencyService currencyService)
    {
        _currencyService = currencyService;
    }

    /// <summary>
    /// Gets the latest exchange rates for a base currency.
    /// </summary>
    /// <remarks>
    /// Retrieves the most recent exchange rates for all supported currencies relative to the specified base currency.
    /// </remarks>
    /// <param name="baseCurrency">The base currency code (default: EUR).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Latest exchange rates.</returns>
    /// <response code="200">Successfully retrieved exchange rates.</response>
    /// <response code="400">Invalid base currency.</response>
    /// <response code="502">External service is unavailable.</response>
    [HttpGet("latest")]
    [ProducesResponseType(typeof(LatestRatesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<LatestRatesResponse>> GetLatestRates(
        [FromQuery] string baseCurrency = "EUR",
        CancellationToken cancellationToken = default)
    {
        // Basic validation
        if (string.IsNullOrWhiteSpace(baseCurrency) || baseCurrency.Length != 3)
        {
            return Problem(
                detail: "Base currency must be a valid 3-letter currency code.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request");
        }

        var result = await _currencyService.GetLatestRatesAsync(
            baseCurrency.ToUpperInvariant(),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Converts an amount from one currency to another.
    /// </summary>
    /// <remarks>
    /// Performs currency conversion using the latest exchange rates.
    /// Excluded currencies (TRY, PLN, THB, MXN) cannot be used for conversion.
    /// </remarks>
    /// <param name="from">Source currency code.</param>
    /// <param name="to">Target currency code.</param>
    /// <param name="amount">Amount to convert (must be greater than 0).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Conversion result with exchange rate and converted amount.</returns>
    /// <response code="200">Successfully converted currency.</response>
    /// <response code="400">Invalid input or excluded currency used.</response>
    /// <response code="502">External service is unavailable.</response>
    [HttpGet("convert")]
    [ProducesResponseType(typeof(ConversionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ConversionResponse>> ConvertCurrency(
        [FromQuery] string from,
        [FromQuery] string to,
        [FromQuery] decimal amount,
        CancellationToken cancellationToken = default)
    {
        // Validate required parameters
        if (string.IsNullOrWhiteSpace(from))
        {
            return Problem(
                detail: "Source currency ('from') is required.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request");
        }

        if (string.IsNullOrWhiteSpace(to))
        {
            return Problem(
                detail: "Target currency ('to') is required.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request");
        }

        // Validate currency code format
        if (from.Length != 3 || to.Length != 3)
        {
            return Problem(
                detail: "Currency codes must be 3-letter codes.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request");
        }

        // Validate amount
        if (amount <= 0)
        {
            return Problem(
                detail: "Amount must be greater than 0.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request");
        }

        // Validate same currency
        if (from.Equals(to, StringComparison.OrdinalIgnoreCase))
        {
            return Problem(
                detail: "Source and target currencies must be different.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request");
        }

        var request = new ConversionRequest
        {
            From = from.ToUpperInvariant(),
            To = to.ToUpperInvariant(),
            Amount = amount
        };

        var result = await _currencyService.ConvertCurrencyAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets historical exchange rates for a date range with pagination.
    /// </summary>
    /// <remarks>
    /// Retrieves exchange rates for a specified date range. Results are paginated.
    /// The date range cannot exceed 365 days.
    /// </remarks>
    /// <param name="baseCurrency">The base currency code.</param>
    /// <param name="startDate">Start date of the range (format: yyyy-MM-dd).</param>
    /// <param name="endDate">End date of the range (format: yyyy-MM-dd).</param>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Number of items per page (default: 10, max: 50).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated historical exchange rates.</returns>
    /// <response code="200">Successfully retrieved historical rates.</response>
    /// <response code="400">Invalid input parameters.</response>
    /// <response code="502">External service is unavailable.</response>
    [HttpGet("history")]
    [ProducesResponseType(typeof(HistoricalRatesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<HistoricalRatesResponse>> GetHistoricalRates(
        [FromQuery] string baseCurrency,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        // Validate required parameters
        if (string.IsNullOrWhiteSpace(baseCurrency))
        {
            return Problem(
                detail: "Base currency is required.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request");
        }

        if (baseCurrency.Length != 3)
        {
            return Problem(
                detail: "Base currency must be a 3-letter currency code.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request");
        }

        // Validate date range
        if (startDate >= endDate)
        {
            return Problem(
                detail: "Start date must be before end date.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request");
        }

        // Validate date range doesn't exceed 365 days
        var daysDifference = endDate.DayNumber - startDate.DayNumber;
        if (daysDifference > 365)
        {
            return Problem(
                detail: "Date range cannot exceed 365 days.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request");
        }

        // Validate pagination parameters
        if (page < 1)
        {
            return Problem(
                detail: "Page number must be at least 1.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request");
        }

        if (pageSize < 1 || pageSize > 50)
        {
            return Problem(
                detail: "Page size must be between 1 and 50.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request");
        }

        var request = new HistoricalRatesRequest
        {
            Base = baseCurrency.ToUpperInvariant(),
            StartDate = startDate,
            EndDate = endDate,
            Page = page,
            PageSize = pageSize
        };

        var result = await _currencyService.GetHistoricalRatesAsync(request, cancellationToken);
        return Ok(result);
    }
}
