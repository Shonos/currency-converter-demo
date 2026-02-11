using CurrencyConverterDemo.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CurrencyConverterDemo.Infrastructure.Caching;

/// <summary>
/// In-memory token blacklist service for development/fallback.
/// Note: This does not scale across multiple instances - use Redis for production.
/// </summary>
public class InMemoryTokenBlacklistService : ITokenBlacklistService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<InMemoryTokenBlacklistService> _logger;
    private const string KeyPrefix = "blacklist:";

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryTokenBlacklistService"/> class.
    /// </summary>
    /// <param name="cache">Memory cache instance.</param>
    /// <param name="logger">Logger instance.</param>
    public InMemoryTokenBlacklistService(
        IMemoryCache cache,
        ILogger<InMemoryTokenBlacklistService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task BlacklistTokenAsync(string jti, TimeSpan remainingLifetime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jti);

        if (remainingLifetime <= TimeSpan.Zero)
        {
            _logger.LogDebug("Token {Jti} is already expired, skipping blacklist", jti);
            return Task.CompletedTask;
        }

        var key = $"{KeyPrefix}{jti}";
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = remainingLifetime
        };

        _cache.Set(key, true, options);

        _logger.LogInformation(
            "Token {Jti} blacklisted in memory with TTL {TTL}",
            jti,
            remainingLifetime);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> IsTokenBlacklistedAsync(string jti)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jti);

        var key = $"{KeyPrefix}{jti}";
        var isBlacklisted = _cache.TryGetValue(key, out _);

        if (isBlacklisted)
        {
            _logger.LogWarning("Token {Jti} is blacklisted (in-memory)", jti);
        }

        return Task.FromResult(isBlacklisted);
    }
}
