namespace CurrencyConverterDemo.Application.DTOs;

/// <summary>
/// Response containing conversion result.
/// </summary>
public class ConversionResponse
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
    /// The original amount.
    /// </summary>
    public required decimal Amount { get; init; }

    /// <summary>
    /// The converted amount.
    /// </summary>
    public required decimal ConvertedAmount { get; init; }

    /// <summary>
    /// The exchange rate used.
    /// </summary>
    public required decimal Rate { get; init; }

    /// <summary>
    /// The date of the exchange rate.
    /// </summary>
    public required DateOnly Date { get; init; }
}
