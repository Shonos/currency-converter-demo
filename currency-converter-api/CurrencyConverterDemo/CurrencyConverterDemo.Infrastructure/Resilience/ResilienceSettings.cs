namespace CurrencyConverterDemo.Infrastructure.Resilience;

/// <summary>
/// Configuration settings for resilience policies (retry, circuit breaker, timeout).
/// </summary>
public class ResilienceSettings
{
    /// <summary>
    /// Gets or sets the retry policy settings.
    /// </summary>
    public RetrySettings Retry { get; set; } = new();

    /// <summary>
    /// Gets or sets the circuit breaker policy settings.
    /// </summary>
    public CircuitBreakerSettings CircuitBreaker { get; set; } = new();

    /// <summary>
    /// Gets or sets the timeout policy settings.
    /// </summary>
    public TimeoutSettings Timeout { get; set; } = new();
}

/// <summary>
/// Retry policy settings.
/// </summary>
public class RetrySettings
{
    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the base delay in seconds for exponential backoff.
    /// </summary>
    public double BaseDelaySeconds { get; set; } = 1;

    /// <summary>
    /// Gets or sets the maximum delay in seconds between retries.
    /// </summary>
    public double MaxDelaySeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets a value indicating whether to use jitter in the retry delay.
    /// </summary>
    public bool UseJitter { get; set; } = true;
}

/// <summary>
/// Circuit breaker policy settings.
/// </summary>
public class CircuitBreakerSettings
{
    /// <summary>
    /// Gets or sets the failure ratio threshold (0.0 to 1.0) that will trip the circuit breaker.
    /// </summary>
    public double FailureRatio { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets the sampling duration in seconds for calculating the failure ratio.
    /// </summary>
    public int SamplingDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the minimum throughput (number of requests) required before evaluating the failure ratio.
    /// </summary>
    public int MinimumThroughput { get; set; } = 5;

    /// <summary>
    /// Gets or sets the duration in seconds the circuit breaker stays open before attempting to close again.
    /// </summary>
    public int BreakDurationSeconds { get; set; } = 60;
}

/// <summary>
/// Timeout policy settings.
/// </summary>
public class TimeoutSettings
{
    /// <summary>
    /// Gets or sets the timeout in seconds for each individual attempt.
    /// </summary>
    public int PerAttemptTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Gets or sets the total timeout in seconds for all attempts (including retries).
    /// </summary>
    public int TotalTimeoutSeconds { get; set; } = 60;
}
