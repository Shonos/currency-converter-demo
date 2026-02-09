namespace CurrencyConverterDemo.Application.DTOs;

/// <summary>
/// A single day's exchange rates for historical data.
/// </summary>
public class DailyRate
{
    /// <summary>
    /// The date for this rate.
    /// </summary>
    public required DateOnly Date { get; init; }

    /// <summary>
    /// Dictionary of currency codes to their exchange rates for this date.
    /// </summary>
    public required Dictionary<string, decimal> Rates { get; init; }
}

/// <summary>
/// Response containing historical exchange rates with pagination.
/// </summary>
public class HistoricalRatesResponse
{
    /// <summary>
    /// The base currency code.
    /// </summary>
    public required string Base { get; init; }

    /// <summary>
    /// The start date of the range.
    /// </summary>
    public required DateOnly StartDate { get; init; }

    /// <summary>
    /// The end date of the range.
    /// </summary>
    public required DateOnly EndDate { get; init; }

    /// <summary>
    /// The daily rates for the current page.
    /// </summary>
    public required List<DailyRate> Rates { get; init; }

    /// <summary>
    /// Current page number.
    /// </summary>
    public required int Page { get; init; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public required int PageSize { get; init; }

    /// <summary>
    /// Total number of days in the full date range.
    /// </summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public required int TotalPages { get; init; }

    /// <summary>
    /// Indicates if there is a next page.
    /// </summary>
    public required bool HasNextPage { get; init; }

    /// <summary>
    /// Indicates if there is a previous page.
    /// </summary>
    public required bool HasPreviousPage { get; init; }
}
