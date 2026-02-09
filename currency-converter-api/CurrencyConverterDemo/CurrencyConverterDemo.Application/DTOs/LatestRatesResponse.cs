namespace CurrencyConverterDemo.Application.DTOs;

/// <summary>
/// Response containing the latest exchange rates.
/// </summary>
public class LatestRatesResponse
{
    /// <summary>
    /// The base currency code.
    /// </summary>
    public required string Base { get; init; }

    /// <summary>
    /// The date for which the rates are valid.
    /// </summary>
    public required DateOnly Date { get; init; }

    /// <summary>
    /// Dictionary of currency codes to their exchange rates.
    /// </summary>
    public required Dictionary<string, decimal> Rates { get; init; }
}
