namespace CurrencyConverterDemo.Domain.Models;

/// <summary>
/// Represents a currency with its code and full name.
/// </summary>
public class Currency
{
    /// <summary>
    /// ISO 4217 currency code (e.g., USD, EUR, GBP).
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Full name of the currency (e.g., US Dollar, Euro, British Pound).
    /// </summary>
    public required string Name { get; init; }
}
