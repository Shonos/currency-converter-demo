using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using WireMock.Server;

namespace CurrencyConverterDemo.Tests.Fixtures;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private WireMockServer? _wireMockServer;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _wireMockServer = WireMockServer.Start();

        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CurrencyProvider:Frankfurter:BaseUrl"] = _wireMockServer.Url + "/",
                ["JwtSettings:Secret"] = "TestSecret-minimum-32-characters-long-key-here-for-testing!!",
                ["JwtSettings:Issuer"] = "TestIssuer",
                ["JwtSettings:Audience"] = "TestAudience",
                ["JwtSettings:ExpirationMinutes"] = "60",
                ["ConnectionStrings:Redis"] = "", // Disable Redis for tests
                ["DemoUsers"] = "demo:Demo@1234:User|admin:Admin@1234:Admin", // Test users
                
                // Make rate limiting very permissive for tests
                ["RateLimiting:Fixed:PermitLimit"] = "10000",
                ["RateLimiting:Fixed:WindowSeconds"] = "3600",
                ["RateLimiting:Sliding:PermitLimit"] = "10000",
                ["RateLimiting:Sliding:WindowSeconds"] = "3600",
                ["RateLimiting:Auth:PermitLimit"] = "10000",
                ["RateLimiting:Auth:WindowMinutes"] = "60"
            });
        });
    }

    public WireMockServer WireMockServer => _wireMockServer 
        ?? throw new InvalidOperationException("WireMock server not initialized");

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _wireMockServer?.Stop();
            _wireMockServer?.Dispose();
        }
        base.Dispose(disposing);
    }
}
