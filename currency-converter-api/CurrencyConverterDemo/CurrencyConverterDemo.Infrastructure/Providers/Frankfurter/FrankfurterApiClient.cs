using System.Net.Http.Json;
using CurrencyConverterDemo.Application.Exceptions;
using CurrencyConverterDemo.Infrastructure.Providers.Frankfurter.Models;

namespace CurrencyConverterDemo.Infrastructure.Providers.Frankfurter;

/// <summary>
/// Typed HTTP client for the Frankfurter API.
/// </summary>
public class FrankfurterApiClient
{
    private readonly HttpClient _httpClient;

    public FrankfurterApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Gets the latest exchange rates.
    /// </summary>
    public async Task<FrankfurterLatestResponse> GetLatestRatesAsync(
        string baseCurrency,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"latest?base={baseCurrency}",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<FrankfurterLatestResponse>(
                cancellationToken: cancellationToken);

            return result ?? throw new ExternalApiException("Frankfurter", "Failed to deserialize response");
        }
        catch (HttpRequestException ex)
        {
            throw new ExternalApiException("Frankfurter", "API request failed", ex);
        }
    }

    /// <summary>
    /// Gets historical exchange rates for a date range.
    /// </summary>
    public async Task<FrankfurterTimeSeriesResponse> GetTimeSeriesAsync(
        string baseCurrency,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var startStr = startDate.ToString("yyyy-MM-dd");
            var endStr = endDate.ToString("yyyy-MM-dd");

            var response = await _httpClient.GetAsync(
                $"{startStr}..{endStr}?base={baseCurrency}",
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<FrankfurterTimeSeriesResponse>(
                cancellationToken: cancellationToken);

            return result ?? throw new ExternalApiException("Frankfurter", "Failed to deserialize response");
        }
        catch (HttpRequestException ex)
        {
            throw new ExternalApiException("Frankfurter", "API request failed", ex);
        }
    }

    /// <summary>
    /// Gets the list of supported currencies.
    /// </summary>
    public async Task<Dictionary<string, string>> GetCurrenciesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("currencies", cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(
                cancellationToken: cancellationToken);

            return result ?? throw new ExternalApiException("Frankfurter", "Failed to deserialize response");
        }
        catch (HttpRequestException ex)
        {
            throw new ExternalApiException("Frankfurter", "API request failed", ex);
        }
    }
}
