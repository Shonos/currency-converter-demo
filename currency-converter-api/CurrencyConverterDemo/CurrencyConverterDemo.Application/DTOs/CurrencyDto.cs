namespace CurrencyConverterDemo.Application.DTOs;

/// <summary>
/// Represents a single currency with code and name.
/// </summary>
public class CurrencyDto
{
    /// <summary>
    /// ISO 4217 currency code (e.g., USD, EUR).
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Full name of the currency.
    /// </summary>
    public required string Name { get; init; }
}
