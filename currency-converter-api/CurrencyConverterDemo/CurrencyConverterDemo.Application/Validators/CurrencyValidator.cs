using CurrencyConverterDemo.Domain.Constants;

namespace CurrencyConverterDemo.Application.Validators;

/// <summary>
/// Validates currency codes against business rules.
/// </summary>
public static class CurrencyValidator
{
    /// <summary>
    /// Validates that a currency code is not excluded.
    /// </summary>
    /// <param name="currencyCode">The currency code to validate.</param>
    /// <returns>True if valid, false if excluded.</returns>
    public static bool IsValid(string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
            return false;

        return !ExcludedCurrencies.IsExcluded(currencyCode);
    }

    /// <summary>
    /// Validates multiple currency codes.
    /// </summary>
    /// <param name="currencyCodes">The currency codes to validate.</param>
    /// <returns>True if all are valid, false if any are excluded.</returns>
    public static bool AreValid(params string[] currencyCodes)
    {
        return currencyCodes.All(IsValid);
    }

    /// <summary>
    /// Gets the first invalid currency code from a list.
    /// </summary>
    /// <param name="currencyCodes">The currency codes to check.</param>
    /// <returns>The first invalid code, or null if all are valid.</returns>
    public static string? GetFirstInvalid(params string[] currencyCodes)
    {
        return currencyCodes.FirstOrDefault(code => !IsValid(code));
    }
}
