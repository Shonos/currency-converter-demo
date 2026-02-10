namespace CurrencyConverterDemo.Infrastructure.Caching;

/// <summary>
/// Abstraction for caching operations supporting both in-memory and distributed caching.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Tries to get a value from the cache.
    /// </summary>
    /// <typeparam name="T">The type of the cached item.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The cached value if found.</param>
    /// <returns>True if the value was found in cache; otherwise, false.</returns>
    Task<(bool Found, T? Value)> TryGetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a value in the cache with the specified expiration.
    /// </summary>
    /// <typeparam name="T">The type of the item to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="absoluteExpiration">Absolute expiration time relative to now.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetAsync<T>(string key, T value, TimeSpan absoluteExpiration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a value from the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
