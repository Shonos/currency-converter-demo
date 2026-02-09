using CurrencyConverterDemo.Domain.Interfaces;
using CurrencyConverterDemo.Domain.Models;

namespace CurrencyConverterDemo.Infrastructure.Providers.Frankfurter;

/// <summary>
/// Frankfurter API implementation of the currency provider.
/// </summary>
public class FrankfurterCurrencyProvider : ICurrencyProvider
{
    private readonly FrankfurterApiClient _apiClient;

    public string ProviderName => "Frankfurter";

    public FrankfurterCurrencyProvider(FrankfurterApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<ExchangeRateResult> GetLatestRatesAsync(
        string baseCurrency,
        CancellationToken cancellationToken = default)
    {
        var response = await _apiClient.GetLatestRatesAsync(baseCurrency, cancellationToken);

        return new ExchangeRateResult
        {
            Base = response.Base,
            Date = DateOnly.Parse(response.Date),
            Rates = response.Rates
        };
    }

    public async Task<ConversionResult> ConvertCurrencyAsync(
        string fromCurrency,
        string toCurrency,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        // Frankfurter doesn't have a direct conversion endpoint, so we get latest rates
        var response = await _apiClient.GetLatestRatesAsync(fromCurrency, cancellationToken);

        if (!response.Rates.TryGetValue(toCurrency, out var rate))
        {
            throw new InvalidOperationException($"Currency '{toCurrency}' not found in rates");
        }

        var convertedAmount = amount * rate;

        return new ConversionResult
        {
            From = fromCurrency,
            To = toCurrency,
            Amount = amount,
            ConvertedAmount = convertedAmount,
            Rate = rate,
            Date = DateOnly.Parse(response.Date)
        };
    }

    public async Task<HistoricalRateResult> GetHistoricalRatesAsync(
        string baseCurrency,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var response = await _apiClient.GetTimeSeriesAsync(
            baseCurrency,
            startDate,
            endDate,
            cancellationToken);

        // Convert string dates to DateOnly
        var rates = response.Rates.ToDictionary(
            kvp => DateOnly.Parse(kvp.Key),
            kvp => (IReadOnlyDictionary<string, decimal>)kvp.Value);

        return new HistoricalRateResult
        {
            StartDate = DateOnly.Parse(response.Start_date),
            EndDate = DateOnly.Parse(response.End_date),
            Base = response.Base,
            Rates = rates
        };
    }

    public async Task<IReadOnlyDictionary<string, string>> GetCurrenciesAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _apiClient.GetCurrenciesAsync(cancellationToken);
        return response;
    }
}
