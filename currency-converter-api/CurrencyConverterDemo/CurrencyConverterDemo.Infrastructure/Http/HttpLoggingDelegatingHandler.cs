using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CurrencyConverterDemo.Infrastructure.Http;

/// <summary>
/// Logs outgoing HTTP requests and responses with timing information.
/// </summary>
public class HttpLoggingDelegatingHandler : DelegatingHandler
{
    private readonly ILogger<HttpLoggingDelegatingHandler> _logger;

    public HttpLoggingDelegatingHandler(ILogger<HttpLoggingDelegatingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        _logger.LogInformation(
            "Outgoing HTTP {Method} {Url}",
            request.Method, request.RequestUri);

        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            sw.Stop();

            _logger.LogInformation(
                "Frankfurter API responded {StatusCode} in {ElapsedMs}ms for {Method} {Url}",
                (int)response.StatusCode, sw.ElapsedMilliseconds,
                request.Method, request.RequestUri);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "Frankfurter API call failed after {ElapsedMs}ms for {Method} {Url}",
                sw.ElapsedMilliseconds, request.Method, request.RequestUri);
            throw;
        }
    }
}
