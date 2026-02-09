namespace CurrencyConverterDemo.Application.DTOs;

/// <summary>
/// Response containing the list of supported currencies.
/// </summary>
public class CurrenciesResponse
{
    /// <summary>
    /// List of supported currencies.
    /// </summary>
    public required List<CurrencyDto> Currencies { get; init; }
}
