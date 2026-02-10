using CurrencyConverterDemo.Domain.Interfaces;
using CurrencyConverterDemo.Infrastructure.Caching;
using CurrencyConverterDemo.Infrastructure.Configuration;
using CurrencyConverterDemo.Infrastructure.Factories;
using CurrencyConverterDemo.Infrastructure.Providers.Frankfurter;
using CurrencyConverterDemo.Infrastructure.Resilience;
using Microsoft.Extensions.Caching.Memory;
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
        services.Configure<CacheSettings>(
            configuration.GetSection("CacheSettings"));
        services.Configure<ResilienceSettings>(
            configuration.GetSection("ResilienceSettings"));

        // Register memory cache
        services.AddMemoryCache();

        // Optional: Register distributed cache (Redis/Valkey) if configured
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "CurrencyConverter:";
            });
        }

        // Get resilience settings for HttpClient configuration
        var resilienceSettings = configuration
            .GetSection("ResilienceSettings")
            .Get<ResilienceSettings>() ?? new ResilienceSettings();

        // Register named Frankfurter HTTP client with resilience policies
        services.AddHttpClient("FrankfurterApiClient", (serviceProvider, client) =>
        {
            var settings = configuration
                .GetSection("CurrencyProvider:Frankfurter")
                .Get<FrankfurterSettings>() ?? new FrankfurterSettings();

            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        })
        .AddResiliencePolicies(resilienceSettings);

        // Register FrankfurterApiClient as a service
        services.AddScoped<FrankfurterApiClient>();

        // Register the base currency provider (without caching)
        services.AddScoped<FrankfurterCurrencyProvider>();

        // Register the cached currency provider as a decorator
        services.AddScoped<ICurrencyProvider>(sp =>
        {
            var innerProvider = sp.GetRequiredService<FrankfurterCurrencyProvider>();
            var cache = sp.GetRequiredService<IMemoryCache>();
            var cacheSettings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CacheSettings>>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CachedCurrencyProvider>>();

            return new CachedCurrencyProvider(innerProvider, cache, cacheSettings, logger);
        });

        // Register factory
        services.AddScoped<ICurrencyProviderFactory, CurrencyProviderFactory>();

        return services;
    }
}
