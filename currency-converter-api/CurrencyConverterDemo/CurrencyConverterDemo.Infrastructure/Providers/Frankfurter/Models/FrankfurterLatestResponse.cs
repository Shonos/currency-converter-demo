using System.Text.Json.Serialization;

namespace CurrencyConverterDemo.Infrastructure.Providers.Frankfurter.Models;

/// <summary>
/// Raw response from Frankfurter API for latest rates endpoint.
/// </summary>
public class FrankfurterLatestResponse
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("base")]
    public string Base { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    [JsonPropertyName("rates")]
    public Dictionary<string, decimal> Rates { get; set; } = new();
}
