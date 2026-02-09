using CurrencyConverterDemo.Application.DTOs;

namespace CurrencyConverterDemo.Application.Services;

/// <summary>
/// Application service for currency operations.
/// </summary>
public interface ICurrencyService
{
    /// <summary>
    /// Gets the latest exchange rates for a base currency.
    /// </summary>
    /// <param name="baseCurrency">The base currency code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The latest exchange rates.</returns>
    Task<LatestRatesResponse> GetLatestRatesAsync(
        string baseCurrency,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts an amount between two currencies.
    /// </summary>
    /// <param name="request">The conversion request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The conversion result.</returns>
    Task<ConversionResponse> ConvertCurrencyAsync(
        ConversionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets historical exchange rates for a date range with pagination.
    /// </summary>
    /// <param name="request">The historical rates request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated historical exchange rates.</returns>
    Task<HistoricalRatesResponse> GetHistoricalRatesAsync(
        HistoricalRatesRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of all supported currencies (excluding blocked currencies).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of supported currencies.</returns>
    Task<CurrenciesResponse> GetCurrenciesAsync(
        CancellationToken cancellationToken = default);
}
