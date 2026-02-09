namespace CurrencyConverterDemo.Application.DTOs;

/// <summary>
/// Request for historical exchange rates.
/// </summary>
public class HistoricalRatesRequest
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
    /// The page number for pagination (1-based).
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// The number of items per page.
    /// </summary>
    public int PageSize { get; init; } = 10;
}
