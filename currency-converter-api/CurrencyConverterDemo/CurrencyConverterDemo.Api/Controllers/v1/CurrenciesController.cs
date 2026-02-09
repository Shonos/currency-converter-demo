using Asp.Versioning;
using CurrencyConverterDemo.Application.DTOs;
using CurrencyConverterDemo.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverterDemo.Api.Controllers.v1;

/// <summary>
/// Controller for managing currency information.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/currencies")]
[AllowAnonymous]
public class CurrenciesController : ControllerBase
{
    private readonly ICurrencyService _currencyService;

    public CurrenciesController(ICurrencyService currencyService)
    {
        _currencyService = currencyService;
    }

    /// <summary>
    /// Gets the list of all supported currencies.
    /// </summary>
    /// <remarks>
    /// Returns all currencies supported by the system, excluding blocked currencies (TRY, PLN, THB, MXN).
    /// This endpoint is public and does not require authentication.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of supported currencies with codes and names.</returns>
    /// <response code="200">Successfully retrieved the currency list.</response>
    /// <response code="502">External service is unavailable.</response>
    [HttpGet]
    [ProducesResponseType(typeof(CurrenciesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<CurrenciesResponse>> GetCurrencies(
        CancellationToken cancellationToken = default)
    {
        var result = await _currencyService.GetCurrenciesAsync(cancellationToken);
        return Ok(result);
    }
}
