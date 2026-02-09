using CurrencyConverterDemo.Domain.Enums;
using CurrencyConverterDemo.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace CurrencyConverterDemo.Infrastructure.Factories;

/// <summary>
/// Factory for creating currency provider instances.
/// </summary>
public class CurrencyProviderFactory : ICurrencyProviderFactory
{
    private readonly IEnumerable<ICurrencyProvider> _providers;
    private readonly CurrencyProviderType _defaultProvider;

    public CurrencyProviderFactory(
        IEnumerable<ICurrencyProvider> providers,
        IOptions<Infrastructure.Configuration.CurrencyProviderSettings> options)
    {
        _providers = providers;
        _defaultProvider = options.Value.DefaultProvider;
    }

    public ICurrencyProvider GetProvider(CurrencyProviderType providerType)
    {
        return _providers.FirstOrDefault(p =>
            p.ProviderName.Equals(providerType.ToString(), StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Provider '{providerType}' not registered.");
    }

    public ICurrencyProvider GetDefaultProvider() => GetProvider(_defaultProvider);
}
