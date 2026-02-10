using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CurrencyConverterDemo.Api.Extensions;

/// <summary>
/// Extension methods for configuring health check services.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Adds health checks to the service collection, including Frankfurter API and Redis health checks.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApiHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var healthChecksBuilder = services.AddHealthChecks()
            .AddUrlGroup(
                new Uri("https://api.frankfurter.dev/v1/currencies"),
                name: "frankfurter-api",
                failureStatus: HealthStatus.Degraded,
                timeout: TimeSpan.FromSeconds(5));

        // Add Redis health check if connection string is configured (non-critical)
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            healthChecksBuilder.AddRedis(
                redisConnection,
                name: "redis-cache",
                failureStatus: HealthStatus.Degraded,  // Non-critical: won't fail overall health
                tags: new[] { "cache", "redis" });
        }

        return services;
    }
}
