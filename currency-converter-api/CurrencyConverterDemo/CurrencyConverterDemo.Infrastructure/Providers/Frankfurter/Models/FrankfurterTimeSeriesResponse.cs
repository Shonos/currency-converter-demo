using System.Text.Json.Serialization;

namespace CurrencyConverterDemo.Infrastructure.Providers.Frankfurter.Models;

/// <summary>
/// Raw response from Frankfurter API for time series endpoint.
/// </summary>
public class FrankfurterTimeSeriesResponse
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("base")]
    public string Base { get; set; } = string.Empty;

    [JsonPropertyName("start_date")]
    public string Start_date { get; set; } = string.Empty;

    [JsonPropertyName("end_date")]
    public string End_date { get; set; } = string.Empty;

    [JsonPropertyName("rates")]
    public Dictionary<string, Dictionary<string, decimal>> Rates { get; set; } = new();
}
