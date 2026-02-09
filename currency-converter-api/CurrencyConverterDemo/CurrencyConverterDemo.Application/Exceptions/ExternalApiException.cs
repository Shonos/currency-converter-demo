namespace CurrencyConverterDemo.Application.Exceptions;

/// <summary>
/// Exception thrown when an external API call fails.
/// </summary>
public class ExternalApiException : Exception
{
    /// <summary>
    /// The name of the external API that failed.
    /// </summary>
    public string ApiName { get; }

    public ExternalApiException(string apiName)
        : base($"External API '{apiName}' request failed.")
    {
        ApiName = apiName;
    }

    public ExternalApiException(string apiName, string message)
        : base(message)
    {
        ApiName = apiName;
    }

    public ExternalApiException(string apiName, string message, Exception innerException)
        : base(message, innerException)
    {
        ApiName = apiName;
    }
}
