using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace CurrencyConverterDemo.Infrastructure.Caching;

/// <summary>
/// Distributed cache implementation of <see cref="ICacheService"/> using Redis/Valkey.
/// </summary>
public class DistributedCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public DistributedCacheService(IDistributedCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<(bool Found, T? Value)> TryGetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var bytes = await _cache.GetAsync(key, cancellationToken);
        if (bytes == null || bytes.Length == 0)
        {
            return (false, default);
        }

        try
        {
            var value = JsonSerializer.Deserialize<T>(bytes, JsonOptions);
            return (true, value);
        }
        catch (JsonException)
        {
            // If deserialization fails, treat as cache miss
            return (false, default);
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan absoluteExpiration, CancellationToken cancellationToken = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpiration
        };
        await _cache.SetAsync(key, bytes, options, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(key, cancellationToken);
    }
}
