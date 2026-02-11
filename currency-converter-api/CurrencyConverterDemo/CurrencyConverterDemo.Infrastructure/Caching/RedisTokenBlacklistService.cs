using CurrencyConverterDemo.Domain.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace CurrencyConverterDemo.Infrastructure.Caching;

/// <summary>
/// Redis-backed token blacklist service using IDistributedCache.
/// </summary>
public class RedisTokenBlacklistService : ITokenBlacklistService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisTokenBlacklistService> _logger;
    private const string KeyPrefix = "blacklist:";

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisTokenBlacklistService"/> class.
    /// </summary>
    /// <param name="cache">Distributed cache (Redis).</param>
    /// <param name="logger">Logger instance.</param>
    public RedisTokenBlacklistService(
        IDistributedCache cache,
        ILogger<RedisTokenBlacklistService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task BlacklistTokenAsync(string jti, TimeSpan remainingLifetime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jti);

        if (remainingLifetime <= TimeSpan.Zero)
        {
            _logger.LogDebug("Token {Jti} is already expired, skipping blacklist", jti);
            return;
        }

        try
        {
            var key = $"{KeyPrefix}{jti}";
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = remainingLifetime
            };

            await _cache.SetStringAsync(key, "1", options);

            _logger.LogInformation(
                "Token {Jti} blacklisted successfully with TTL {TTL}",
                jti,
                remainingLifetime);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to blacklist token {Jti}. Redis may be unavailable",
                jti);
            // Don't throw - fail gracefully
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsTokenBlacklistedAsync(string jti)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jti);

        try
        {
            var key = $"{KeyPrefix}{jti}";
            var value = await _cache.GetStringAsync(key);

            var isBlacklisted = !string.IsNullOrEmpty(value);

            if (isBlacklisted)
            {
                _logger.LogWarning("Token {Jti} is blacklisted", jti);
            }

            return isBlacklisted;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to check blacklist for token {Jti}. Redis may be unavailable. Failing open (allowing request)",
                jti);
            
            // Fail-open: if Redis is down, allow the request
            // This prioritizes availability over security for this demo
            return false;
        }
    }
}
