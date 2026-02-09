using CurrencyConverterDemo.Domain.Enums;

namespace CurrencyConverterDemo.Domain.Interfaces;

/// <summary>
/// Factory for creating currency provider instances.
/// </summary>
public interface ICurrencyProviderFactory
{
    /// <summary>
    /// Gets a currency provider by type.
    /// </summary>
    /// <param name="providerType">The type of provider to retrieve.</param>
    /// <returns>The currency provider instance.</returns>
    ICurrencyProvider GetProvider(CurrencyProviderType providerType);

    /// <summary>
    /// Gets the default currency provider configured for the application.
    /// </summary>
    /// <returns>The default currency provider instance.</returns>
    ICurrencyProvider GetDefaultProvider();
}
