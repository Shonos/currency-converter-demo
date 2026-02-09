namespace CurrencyConverterDemo.Application.DTOs;

/// <summary>
/// Request for currency conversion.
/// </summary>
public class ConversionRequest
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
    /// The amount to convert.
    /// </summary>
    public required decimal Amount { get; init; }
}
