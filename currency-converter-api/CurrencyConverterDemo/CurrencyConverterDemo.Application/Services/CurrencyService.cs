using CurrencyConverterDemo.Application.DTOs;
using CurrencyConverterDemo.Application.Exceptions;
using CurrencyConverterDemo.Application.Validators;
using CurrencyConverterDemo.Domain.Constants;
using CurrencyConverterDemo.Domain.Interfaces;

namespace CurrencyConverterDemo.Application.Services;

/// <summary>
/// Implementation of the currency service that orchestrates provider operations and business rules.
/// </summary>
public class CurrencyService : ICurrencyService
{
    private readonly ICurrencyProviderFactory _providerFactory;

    public CurrencyService(ICurrencyProviderFactory providerFactory)
    {
        _providerFactory = providerFactory;
    }

    public async Task<LatestRatesResponse> GetLatestRatesAsync(
        string baseCurrency,
        CancellationToken cancellationToken = default)
    {
        var provider = _providerFactory.GetDefaultProvider();
        var result = await provider.GetLatestRatesAsync(baseCurrency, cancellationToken);

        return new LatestRatesResponse
        {
            Base = result.Base,
            Date = result.Date,
            Rates = new Dictionary<string, decimal>(result.Rates)
        };
    }

    public async Task<ConversionResponse> ConvertCurrencyAsync(
        ConversionRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate currencies against business rules
        var invalidCurrency = CurrencyValidator.GetFirstInvalid(request.From, request.To);
        if (invalidCurrency != null)
        {
            throw new CurrencyNotSupportedException(invalidCurrency);
        }

        var provider = _providerFactory.GetDefaultProvider();
        var result = await provider.ConvertCurrencyAsync(
            request.From,
            request.To,
            request.Amount,
            cancellationToken);

        return new ConversionResponse
        {
            From = result.From,
            To = result.To,
            Amount = result.Amount,
            ConvertedAmount = result.ConvertedAmount,
            Rate = result.Rate,
            Date = result.Date
        };
    }

    public async Task<HistoricalRatesResponse> GetHistoricalRatesAsync(
        HistoricalRatesRequest request,
        CancellationToken cancellationToken = default)
    {
        var provider = _providerFactory.GetDefaultProvider();
        var result = await provider.GetHistoricalRatesAsync(
            request.Base,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        // Implement pagination in-memory since Frankfurter returns all dates
        var allDates = result.Rates.Keys.OrderBy(d => d).ToList();
        var totalCount = allDates.Count;
        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);
        var page = Math.Max(1, Math.Min(request.Page, totalPages > 0 ? totalPages : 1));

        var pagedDates = allDates
            .Skip((page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var pagedRates = pagedDates.Select(date => new DailyRate
        {
            Date = date,
            Rates = new Dictionary<string, decimal>(result.Rates[date])
        }).ToList();

        return new HistoricalRatesResponse
        {
            Base = result.Base,
            StartDate = result.StartDate,
            EndDate = result.EndDate,
            Rates = pagedRates,
            Page = page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1
        };
    }

    public async Task<CurrenciesResponse> GetCurrenciesAsync(
        CancellationToken cancellationToken = default)
    {
        var provider = _providerFactory.GetDefaultProvider();
        var result = await provider.GetCurrenciesAsync(cancellationToken);

        // Filter out excluded currencies
        var currencies = result
            .Where(kvp => !ExcludedCurrencies.IsExcluded(kvp.Key))
            .Select(kvp => new CurrencyDto
            {
                Code = kvp.Key,
                Name = kvp.Value
            })
            .OrderBy(c => c.Code)
            .ToList();

        return new CurrenciesResponse
        {
            Currencies = currencies
        };
    }
}
