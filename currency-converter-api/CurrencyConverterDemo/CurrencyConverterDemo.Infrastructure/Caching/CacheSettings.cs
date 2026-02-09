namespace CurrencyConverterDemo.Infrastructure.Caching;

/// <summary>
/// Configuration settings for caching behavior.
/// </summary>
public class CacheSettings
{
    /// <summary>
    /// Gets or sets the cache duration for latest rates in minutes.
    /// </summary>
    public int LatestRatesMinutes { get; set; } = 5;

    /// <summary>
    /// Gets or sets the cache duration for the currencies list in hours.
    /// </summary>
    public int CurrenciesListHours { get; set; } = 24;

    /// <summary>
    /// Gets or sets the cache duration for historical rates in minutes.
    /// </summary>
    public int HistoricalRatesMinutes { get; set; } = 60;

    /// <summary>
    /// Gets or sets the cache duration for conversion results in minutes.
    /// </summary>
    public int ConversionMinutes { get; set; } = 5;
}
