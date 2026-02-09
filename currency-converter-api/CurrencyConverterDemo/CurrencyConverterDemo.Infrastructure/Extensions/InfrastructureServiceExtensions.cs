using CurrencyConverterDemo.Domain.Interfaces;
using CurrencyConverterDemo.Infrastructure.Configuration;
using CurrencyConverterDemo.Infrastructure.Factories;
using CurrencyConverterDemo.Infrastructure.Providers.Frankfurter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyConverterDemo.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering Infrastructure layer services.
/// </summary>
public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Adds Infrastructure layer services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration settings
        services.Configure<FrankfurterSettings>(
            configuration.GetSection("CurrencyProvider:Frankfurter"));
        services.Configure<CurrencyProviderSettings>(
            configuration.GetSection("CurrencyProvider"));

        // Register Frankfurter HTTP client
        services.AddHttpClient<FrankfurterApiClient>((serviceProvider, client) =>
        {
            var settings = configuration
                .GetSection("CurrencyProvider:Frankfurter")
                .Get<FrankfurterSettings>() ?? new FrankfurterSettings();

            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        });

        // Register currency providers
        services.AddScoped<ICurrencyProvider, FrankfurterCurrencyProvider>();

        // Register factory
        services.AddScoped<ICurrencyProviderFactory, CurrencyProviderFactory>();

        return services;
    }
}
