using CurrencyConverterDemo.Domain.Interfaces;
using CurrencyConverterDemo.Infrastructure.Caching;
using CurrencyConverterDemo.Infrastructure.Configuration;
using CurrencyConverterDemo.Infrastructure.Factories;
using CurrencyConverterDemo.Infrastructure.Http;
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

        // Register HttpContextAccessor for delegating handlers
        services.AddHttpContextAccessor();

        // Register delegating handlers
        services.AddTransient<CorrelationIdDelegatingHandler>();
        services.AddTransient<HttpLoggingDelegatingHandler>();

        // Register memory cache
        services.AddMemoryCache();

        // Register cache service based on configuration
        var cacheType = configuration.GetValue<string>("CacheSettings:Type") ?? "Memory";
        
        if (cacheType.Equals("Distributed", StringComparison.OrdinalIgnoreCase))
        {
            // Register distributed cache (Redis/Valkey) if configured
            var redisConnection = configuration.GetConnectionString("Redis");
            if (!string.IsNullOrEmpty(redisConnection))
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConnection;
                    options.InstanceName = "CurrencyConverter:";
                });
                services.AddSingleton<ICacheService, DistributedCacheService>();
            }
            else
            {
                // Fallback to memory cache if Redis not configured
                services.AddSingleton<ICacheService, MemoryCacheService>();
            }
        }
        else
        {
            // Use in-memory cache
            services.AddSingleton<ICacheService, MemoryCacheService>();
        }

        // Get resilience settings for HttpClient configuration
        var resilienceSettings = configuration
            .GetSection("ResilienceSettings")
            .Get<ResilienceSettings>() ?? new ResilienceSettings();

        // Register named Frankfurter HTTP client with resilience policies and delegating handlers
        services.AddHttpClient("FrankfurterApiClient", (serviceProvider, client) =>
        {
            var settings = configuration
                .GetSection("CurrencyProvider:Frankfurter")
                .Get<FrankfurterSettings>() ?? new FrankfurterSettings();

            client.BaseAddress = new Uri(settings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
        })
        .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
        .AddHttpMessageHandler<HttpLoggingDelegatingHandler>()
        .AddResiliencePolicies(resilienceSettings);

        // Register FrankfurterApiClient as a service
        services.AddScoped<FrankfurterApiClient>();

        // Register the base currency provider (without caching)
        services.AddScoped<FrankfurterCurrencyProvider>();

        // Register the cached currency provider as a decorator
        services.AddScoped<ICurrencyProvider>(sp =>
        {
            var innerProvider = sp.GetRequiredService<FrankfurterCurrencyProvider>();
            var cache = sp.GetRequiredService<ICacheService>();
            var cacheSettings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CacheSettings>>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CachedCurrencyProvider>>();

            return new CachedCurrencyProvider(innerProvider, cache, cacheSettings, logger);
        });

        // Register factory
        services.AddScoped<ICurrencyProviderFactory, CurrencyProviderFactory>();

        return services;
    }
}
