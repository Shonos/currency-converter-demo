using CurrencyConverterDemo.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CurrencyConverterDemo.Application.Extensions;

/// <summary>
/// Extension methods for registering Application layer services.
/// </summary>
public static class ApplicationServiceExtensions
{
    /// <summary>
    /// Adds Application layer services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICurrencyService, CurrencyService>();

        return services;
    }
}
