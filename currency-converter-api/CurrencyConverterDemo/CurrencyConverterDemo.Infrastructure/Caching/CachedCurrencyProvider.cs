using CurrencyConverterDemo.Domain.Interfaces;
using CurrencyConverterDemo.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;

namespace CurrencyConverterDemo.Infrastructure.Caching;

/// <summary>
/// Decorator that adds caching capabilities to an ICurrencyProvider implementation.
/// </summary>
public class CachedCurrencyProvider : ICurrencyProvider
{
    private readonly ICurrencyProvider _innerProvider;
    private readonly ICacheService _cache;
    private readonly CacheSettings _settings;
    private readonly ILogger<CachedCurrencyProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachedCurrencyProvider"/> class.
    /// </summary>
    /// <param name="innerProvider">The inner provider to wrap with caching.</param>
    /// <param name="cache">The cache service (supports both in-memory and distributed caching).</param>
    /// <param name="settings">Cache configuration settings.</param>
    /// <param name="logger">The logger.</param>
    public CachedCurrencyProvider(
        ICurrencyProvider innerProvider,
        ICacheService cache,
        IOptions<CacheSettings> settings,
        ILogger<CachedCurrencyProvider> logger)
    {
        _innerProvider = innerProvider ?? throw new ArgumentNullException(nameof(innerProvider));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string ProviderName => _innerProvider.ProviderName;

    /// <inheritdoc />
    public async Task<ExchangeRateResult> GetLatestRatesAsync(
        string baseCurrency,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeyGenerator.ForLatestRates(baseCurrency);

        var (found, cachedResult) = await _cache.TryGetAsync<ExchangeRateResult>(cacheKey, cancellationToken);
        if (found && cachedResult != null)
        {
            _logger.LogDebug("Cache hit for latest rates: {CacheKey}", cacheKey);
            return cachedResult;
        }

        _logger.LogDebug("Cache miss for latest rates: {CacheKey}", cacheKey);

        try
        {
            var result = await _innerProvider.GetLatestRatesAsync(baseCurrency, cancellationToken);

            await _cache.SetAsync(
                cacheKey,
                result,
                TimeSpan.FromMinutes(_settings.LatestRatesMinutes),
                cancellationToken);

            _logger.LogDebug("Cached latest rates: {CacheKey} for {Minutes} minutes", cacheKey, _settings.LatestRatesMinutes);

            return result;
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "Circuit breaker is open. Attempting to return stale cached data for {CacheKey}", cacheKey);
            
            // Try to get stale cached data (ignore expiration)
            var (staleFound, staleResult) = await _cache.TryGetAsync<ExchangeRateResult>(cacheKey, cancellationToken);
            if (staleFound && staleResult != null)
            {
                _logger.LogWarning("Returning stale cached data for {CacheKey} due to circuit breaker", cacheKey);
                return staleResult;
            }

            _logger.LogError("No stale cache available for {CacheKey} and circuit breaker is open", cacheKey);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ConversionResult> ConvertCurrencyAsync(
        string fromCurrency,
        string toCurrency,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeyGenerator.ForConversion(fromCurrency, toCurrency, amount);

        var (found, cachedResult) = await _cache.TryGetAsync<ConversionResult>(cacheKey, cancellationToken);
        if (found && cachedResult != null)
        {
            _logger.LogDebug("Cache hit for conversion: {CacheKey}", cacheKey);
            return cachedResult;
        }

        _logger.LogDebug("Cache miss for conversion: {CacheKey}", cacheKey);

        try
        {
            var result = await _innerProvider.ConvertCurrencyAsync(fromCurrency, toCurrency, amount, cancellationToken);

            await _cache.SetAsync(
                cacheKey,
                result,
                TimeSpan.FromMinutes(_settings.ConversionMinutes),
                cancellationToken);

            _logger.LogDebug("Cached conversion result: {CacheKey} for {Minutes} minutes", cacheKey, _settings.ConversionMinutes);

            return result;
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "Circuit breaker is open. Attempting to return stale cached data for {CacheKey}", cacheKey);
            
            var (staleFound, staleResult) = await _cache.TryGetAsync<ConversionResult>(cacheKey, cancellationToken);
            if (staleFound && staleResult != null)
            {
                _logger.LogWarning("Returning stale cached data for {CacheKey} due to circuit breaker", cacheKey);
                return staleResult;
            }

            _logger.LogError("No stale cache available for {CacheKey} and circuit breaker is open", cacheKey);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<HistoricalRateResult> GetHistoricalRatesAsync(
        string baseCurrency,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeyGenerator.ForHistoricalRates(baseCurrency, startDate, endDate);

        var (found, cachedResult) = await _cache.TryGetAsync<HistoricalRateResult>(cacheKey, cancellationToken);
        if (found && cachedResult != null)
        {
            _logger.LogDebug("Cache hit for historical rates: {CacheKey}", cacheKey);
            return cachedResult;
        }

        _logger.LogDebug("Cache miss for historical rates: {CacheKey}", cacheKey);

        try
        {
            var result = await _innerProvider.GetHistoricalRatesAsync(baseCurrency, startDate, endDate, cancellationToken);

            await _cache.SetAsync(
                cacheKey,
                result,
                TimeSpan.FromMinutes(_settings.HistoricalRatesMinutes),
                cancellationToken);

            _logger.LogDebug("Cached historical rates: {CacheKey} for {Minutes} minutes", cacheKey, _settings.HistoricalRatesMinutes);

            return result;
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "Circuit breaker is open. Attempting to return stale cached data for {CacheKey}", cacheKey);
            
            var (staleFound, staleResult) = await _cache.TryGetAsync<HistoricalRateResult>(cacheKey, cancellationToken);
            if (staleFound && staleResult != null)
            {
                _logger.LogWarning("Returning stale cached data for {CacheKey} due to circuit breaker", cacheKey);
                return staleResult;
            }

            _logger.LogError("No stale cache available for {CacheKey} and circuit breaker is open", cacheKey);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, string>> GetCurrenciesAsync(
        CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeyGenerator.ForCurrenciesList();

        var (found, cachedResult) = await _cache.TryGetAsync<IReadOnlyDictionary<string, string>>(cacheKey, cancellationToken);
        if (found && cachedResult != null)
        {
            _logger.LogDebug("Cache hit for currencies list: {CacheKey}", cacheKey);
            return cachedResult;
        }

        _logger.LogDebug("Cache miss for currencies list: {CacheKey}", cacheKey);

        try
        {
            var result = await _innerProvider.GetCurrenciesAsync(cancellationToken);

            await _cache.SetAsync(
                cacheKey,
                result,
                TimeSpan.FromHours(_settings.CurrenciesListHours),
                cancellationToken);

            _logger.LogDebug("Cached currencies list: {CacheKey} for {Hours} hours", cacheKey, _settings.CurrenciesListHours);

            return result;
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "Circuit breaker is open. Attempting to return stale cached data for {CacheKey}", cacheKey);
            
            var (staleFound, staleResult) = await _cache.TryGetAsync<IReadOnlyDictionary<string, string>>(cacheKey, cancellationToken);
            if (staleFound && staleResult != null)
            {
                _logger.LogWarning("Returning stale cached data for {CacheKey} due to circuit breaker", cacheKey);
                return staleResult;
            }

            _logger.LogError("No stale cache available for {CacheKey} and circuit breaker is open", cacheKey);
            throw;
        }
    }
}
