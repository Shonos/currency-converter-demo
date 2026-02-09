namespace CurrencyConverterDemo.Application.Exceptions;

/// <summary>
/// Exception thrown when an unsupported or excluded currency is requested.
/// </summary>
public class CurrencyNotSupportedException : Exception
{
    /// <summary>
    /// The currency code that is not supported.
    /// </summary>
    public string CurrencyCode { get; }

    public CurrencyNotSupportedException(string currencyCode)
        : base($"Currency '{currencyCode}' is not supported for conversion.")
    {
        CurrencyCode = currencyCode;
    }

    public CurrencyNotSupportedException(string currencyCode, string message)
        : base(message)
    {
        CurrencyCode = currencyCode;
    }

    public CurrencyNotSupportedException(string currencyCode, string message, Exception innerException)
        : base(message, innerException)
    {
        CurrencyCode = currencyCode;
    }
}
