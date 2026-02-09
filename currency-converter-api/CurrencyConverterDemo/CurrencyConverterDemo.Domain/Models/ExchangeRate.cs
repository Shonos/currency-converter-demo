namespace CurrencyConverterDemo.Domain.Models;

/// <summary>
/// Represents a single exchange rate between two currencies.
/// </summary>
public class ExchangeRate
{
    /// <summary>
    /// The base currency code.
    /// </summary>
    public required string BaseCurrency { get; init; }

    /// <summary>
    /// The target currency code.
    /// </summary>
    public required string TargetCurrency { get; init; }

    /// <summary>
    /// The exchange rate value.
    /// </summary>
    public required decimal Rate { get; init; }

    /// <summary>
    /// The date for which this rate is valid.
    /// </summary>
    public required DateOnly Date { get; init; }
}
