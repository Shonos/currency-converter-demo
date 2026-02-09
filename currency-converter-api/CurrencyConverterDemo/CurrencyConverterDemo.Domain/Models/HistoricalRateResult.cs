namespace CurrencyConverterDemo.Domain.Models;

/// <summary>
/// Represents historical exchange rates over a date range.
/// </summary>
public class HistoricalRateResult
{
    /// <summary>
    /// The start date of the historical range.
    /// </summary>
    public required DateOnly StartDate { get; init; }

    /// <summary>
    /// The end date of the historical range.
    /// </summary>
    public required DateOnly EndDate { get; init; }

    /// <summary>
    /// The base currency code.
    /// </summary>
    public required string Base { get; init; }

    /// <summary>
    /// Dictionary of dates to their respective exchange rates.
    /// Each date maps to a dictionary of currency codes and their rates.
    /// </summary>
    public required IReadOnlyDictionary<DateOnly, IReadOnlyDictionary<string, decimal>> Rates { get; init; }
}
