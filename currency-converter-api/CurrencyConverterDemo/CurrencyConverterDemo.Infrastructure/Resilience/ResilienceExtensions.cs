using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace CurrencyConverterDemo.Infrastructure.Resilience;

/// <summary>
/// Extension methods for configuring HTTP resilience policies.
/// </summary>
public static class ResilienceExtensions
{
    /// <summary>
    /// Adds resilience policies (retry, circuit breaker, timeout) to an HTTP client builder.
    /// </summary>
    /// <param name="builder">The HTTP client builder.</param>
    /// <param name="settings">The resilience settings.</param>
    /// <returns>The HTTP client builder for chaining.</returns>
    public static IHttpClientBuilder AddResiliencePolicies(
        this IHttpClientBuilder builder,
        ResilienceSettings settings)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(settings);

        builder.AddStandardResilienceHandler(options =>
        {
            // Configure retry with exponential backoff
            options.Retry.MaxRetryAttempts = settings.Retry.MaxRetryAttempts;
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.Delay = TimeSpan.FromSeconds(settings.Retry.BaseDelaySeconds);
            options.Retry.MaxDelay = TimeSpan.FromSeconds(settings.Retry.MaxDelaySeconds);
            options.Retry.UseJitter = settings.Retry.UseJitter;

            // Configure circuit breaker
            options.CircuitBreaker.FailureRatio = settings.CircuitBreaker.FailureRatio;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(settings.CircuitBreaker.SamplingDurationSeconds);
            options.CircuitBreaker.MinimumThroughput = settings.CircuitBreaker.MinimumThroughput;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(settings.CircuitBreaker.BreakDurationSeconds);

            // Configure total timeout (across all retries)
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(settings.Timeout.TotalTimeoutSeconds);

            // Configure per-attempt timeout
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(settings.Timeout.PerAttemptTimeoutSeconds);
        });

        return builder;
    }
}
