namespace CurrencyConverterDemo.Domain.Models;

/// <summary>
/// Represents the result of a currency conversion operation.
/// </summary>
public class ConversionResult
{
    /// <summary>
    /// The source currency code.
    /// </summary>
    public required string From { get; init; }

    /// <summary>
    /// The target currency code.
    /// </summary>
    public required string To { get; init; }

    /// <summary>
    /// The original amount to convert.
    /// </summary>
    public required decimal Amount { get; init; }

    /// <summary>
    /// The converted amount in the target currency.
    /// </summary>
    public required decimal ConvertedAmount { get; init; }

    /// <summary>
    /// The exchange rate used for conversion.
    /// </summary>
    public required decimal Rate { get; init; }

    /// <summary>
    /// The date for which this conversion rate is valid.
    /// </summary>
    public required DateOnly Date { get; init; }
}
