using CurrencyConverterDemo.Domain.Enums;

namespace CurrencyConverterDemo.Infrastructure.Configuration;

/// <summary>
/// Configuration for currency provider selection.
/// </summary>
public class CurrencyProviderSettings
{
    /// <summary>
    /// The default provider type to use.
    /// </summary>
    public CurrencyProviderType DefaultProvider { get; set; } = CurrencyProviderType.Frankfurter;
}
