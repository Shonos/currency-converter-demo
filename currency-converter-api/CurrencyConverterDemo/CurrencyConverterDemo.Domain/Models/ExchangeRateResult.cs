namespace CurrencyConverterDemo.Domain.Models;

/// <summary>
/// Represents the result of a latest exchange rates query.
/// </summary>
public class ExchangeRateResult
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
    /// Dictionary of currency codes to their exchange rates relative to the base currency.
    /// </summary>
    public required IReadOnlyDictionary<string, decimal> Rates { get; init; }
}
