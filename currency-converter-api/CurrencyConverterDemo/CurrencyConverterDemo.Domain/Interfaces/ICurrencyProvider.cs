using CurrencyConverterDemo.Domain.Models;

namespace CurrencyConverterDemo.Domain.Interfaces;

/// <summary>
/// Defines the contract for currency data providers.
/// </summary>
public interface ICurrencyProvider
{
    /// <summary>
    /// Gets the provider identifier for factory selection.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Retrieves the latest exchange rates for a base currency.
    /// </summary>
    /// <param name="baseCurrency">The base currency code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The latest exchange rates.</returns>
    Task<ExchangeRateResult> GetLatestRatesAsync(
        string baseCurrency,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts an amount from one currency to another.
    /// </summary>
    /// <param name="fromCurrency">The source currency code.</param>
    /// <param name="toCurrency">The target currency code.</param>
    /// <param name="amount">The amount to convert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The conversion result.</returns>
    Task<ConversionResult> ConvertCurrencyAsync(
        string fromCurrency,
        string toCurrency,
        decimal amount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves historical exchange rates for a date range.
    /// </summary>
    /// <param name="baseCurrency">The base currency code.</param>
    /// <param name="startDate">The start date of the range.</param>
    /// <param name="endDate">The end date of the range.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Historical exchange rates.</returns>
    Task<HistoricalRateResult> GetHistoricalRatesAsync(
        string baseCurrency,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the list of all supported currencies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A dictionary of currency codes to currency names.</returns>
    Task<IReadOnlyDictionary<string, string>> GetCurrenciesAsync(
        CancellationToken cancellationToken = default);
}
