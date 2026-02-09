namespace CurrencyConverterDemo.Domain.Constants;

/// <summary>
/// Business rule: currencies that are not supported for conversion operations.
/// </summary>
public static class ExcludedCurrencies
{
    /// <summary>
    /// Set of currency codes that are excluded from conversion operations.
    /// </summary>
    public static readonly HashSet<string> Codes = new(StringComparer.OrdinalIgnoreCase)
    {
        "TRY", // Turkish Lira
        "PLN", // Polish Złoty
        "THB", // Thai Baht
        "MXN"  // Mexican Peso
    };

    /// <summary>
    /// Checks if a currency code is in the excluded list.
    /// </summary>
    /// <param name="currencyCode">The currency code to check.</param>
    /// <returns>True if the currency is excluded, otherwise false.</returns>
    public static bool IsExcluded(string currencyCode)
        => Codes.Contains(currencyCode);
}
