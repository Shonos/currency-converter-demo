namespace CurrencyConverterDemo.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for the Frankfurter API.
/// </summary>
public class FrankfurterSettings
{
    /// <summary>
    /// The base URL of the Frankfurter API.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.frankfurter.dev/v1/";

    /// <summary>
    /// The timeout in seconds for API requests.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
