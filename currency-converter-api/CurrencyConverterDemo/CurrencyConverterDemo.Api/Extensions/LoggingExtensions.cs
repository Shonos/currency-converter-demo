using Serilog;

namespace CurrencyConverterDemo.Api.Extensions;

/// <summary>
/// Extension methods for configuring logging services.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Configures Serilog as the logging provider.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The web application builder for chaining.</returns>
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, configuration) => 
            configuration.ReadFrom.Configuration(context.Configuration));

        return builder;
    }
}
