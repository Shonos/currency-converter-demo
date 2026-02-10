using Microsoft.Extensions.Caching.Memory;

namespace CurrencyConverterDemo.Infrastructure.Caching;

/// <summary>
/// In-memory cache implementation of <see cref="ICacheService"/>.
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    public MemoryCacheService(IMemoryCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public Task<(bool Found, T? Value)> TryGetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var found = _cache.TryGetValue(key, out T? value);
        return Task.FromResult((found, value));
    }

    public Task SetAsync<T>(string key, T value, TimeSpan absoluteExpiration, CancellationToken cancellationToken = default)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpiration
        };
        _cache.Set(key, value, options);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }
}
