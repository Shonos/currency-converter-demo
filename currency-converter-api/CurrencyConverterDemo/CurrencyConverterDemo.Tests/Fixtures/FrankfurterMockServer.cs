using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace CurrencyConverterDemo.Tests.Fixtures;

public static class FrankfurterMockServer
{
    public static void SetupLatestRates(WireMockServer server, string baseCurrency = "EUR")
    {
        var jsonResponse = $$"""
        {
          "amount": 1.0,
          "base": "{{baseCurrency}}",
          "date": "2024-02-06",
          "rates": {
            "AUD": 1.688,
            "BRL": 6.1767,
            "CAD": 1.6118,
            "CHF": 0.9175,
            "CNY": 8.1838,
            "EUR": 1.0,
            "GBP": 0.8679,
            "JPY": 185.27,
            "USD": 1.1794
          }
        }
        """;

        server.Given(
            Request.Create()
                .WithPath("/latest")
                .WithParam("base", baseCurrency)
                .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(jsonResponse));
    }

    public static void SetupConversion(WireMockServer server, string from, string to, decimal amount = 100)
    {
        var rate = to switch
        {
            "USD" => 1.1794m,
            "GBP" => 0.8679m,
            "JPY" => 185.27m,
            "AUD" => 1.688m,
            _ => 1.0m
        };

        var jsonResponse = $$"""
        {
          "amount": 1.0,
          "base": "{{from}}",
          "date": "2024-02-06",
          "rates": {
            "{{to}}": {{rate}}
          }
        }
        """;

        // Conversion uses GetLatestRatesAsync, so we mock /latest?base=fromCurrency
        server.Given(
            Request.Create()
                .WithPath("/latest")
                .WithParam("base", from)
                .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(jsonResponse));
    }

    public static void SetupTimeSeries(WireMockServer server, string startDate, string endDate, string baseCurrency = "EUR")
    {
        var jsonResponse = $$"""
        {
          "amount": 1.0,
          "base": "{{baseCurrency}}",
          "start_date": "{{startDate}}",
          "end_date": "{{endDate}}",
          "rates": {
            "2024-01-02": { "AUD": 1.6147, "USD": 1.0956 },
            "2024-01-03": { "AUD": 1.6236, "USD": 1.0919 },
            "2024-01-04": { "AUD": 1.628, "USD": 1.0953 },
            "2024-01-05": { "AUD": 1.6301, "USD": 1.0945 }
          }
        }
        """;

        server.Given(
            Request.Create()
                .WithPath($"/{startDate}..{endDate}")
                .WithParam("base", baseCurrency)
                .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(jsonResponse));
    }

    public static void SetupCurrencies(WireMockServer server)
    {
        var jsonResponse = """
        {
          "AUD": "Australian Dollar",
          "BRL": "Brazilian Real",
          "CAD": "Canadian Dollar",
          "CHF": "Swiss Franc",
          "CNY": "Chinese Renminbi Yuan",
          "EUR": "Euro",
          "GBP": "British Pound",
          "JPY": "Japanese Yen",
          "MXN": "Mexican Peso",
          "PLN": "Polish Zloty",
          "THB": "Thai Baht",
          "TRY": "Turkish Lira",
          "USD": "United States Dollar"
        }
        """;

        server.Given(
            Request.Create()
                .WithPath("/currencies")
                .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(jsonResponse));
    }

    public static void SetupServerError(WireMockServer server)
    {
        server.Given(Request.Create().UsingGet())
            .RespondWith(Response.Create().WithStatusCode(500).WithBody("Internal Server Error"));
    }

    public static void SetupNotFound(WireMockServer server, string path)
    {
        server.Given(
            Request.Create()
                .WithPath(path)
                .UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(404)
                    .WithBody("Not Found"));
    }

    public static void ResetMappings(WireMockServer server)
    {
        server.Reset();
    }
}
