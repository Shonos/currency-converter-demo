namespace CurrencyConverterDemo.Infrastructure.Caching;

/// <summary>
/// Generates consistent cache keys for currency data.
/// </summary>
public static class CacheKeyGenerator
{
    private const string LatestRatesPrefix = "rates:latest";
    private const string ConversionPrefix = "rates:convert";
    private const string HistoricalRatesPrefix = "rates:history";
    private const string CurrenciesListKey = "currencies:list";

    /// <summary>
    /// Generates a cache key for latest rates.
    /// </summary>
    /// <param name="baseCurrency">The base currency code.</param>
    /// <returns>A cache key.</returns>
    public static string ForLatestRates(string baseCurrency)
    {
        return $"{LatestRatesPrefix}:{baseCurrency.ToUpperInvariant()}";
    }

    /// <summary>
    /// Generates a cache key for currency conversion.
    /// </summary>
    /// <param name="fromCurrency">The source currency code.</param>
    /// <param name="toCurrency">The target currency code.</param>
    /// <param name="amount">The amount to convert.</param>
    /// <returns>A cache key.</returns>
    public static string ForConversion(string fromCurrency, string toCurrency, decimal amount)
    {
        return $"{ConversionPrefix}:{fromCurrency.ToUpperInvariant()}:{toCurrency.ToUpperInvariant()}:{amount}";
    }

    /// <summary>
    /// Generates a cache key for historical rates.
    /// </summary>
    /// <param name="baseCurrency">The base currency code.</param>
    /// <param name="startDate">The start date.</param>
    /// <param name="endDate">The end date.</param>
    /// <returns>A cache key.</returns>
    public static string ForHistoricalRates(string baseCurrency, DateOnly startDate, DateOnly endDate)
    {
        return $"{HistoricalRatesPrefix}:{baseCurrency.ToUpperInvariant()}:{startDate:yyyy-MM-dd}:{endDate:yyyy-MM-dd}";
    }

    /// <summary>
    /// Gets the cache key for the currencies list.
    /// </summary>
    /// <returns>A cache key.</returns>
    public static string ForCurrenciesList()
    {
        return CurrenciesListKey;
    }
}
