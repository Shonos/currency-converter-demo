using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

public static class ApiVersioningExtensions
{
    public static IServiceCollection AddApiVersioningServices(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        // Dynamically add OpenAPI documents for each discovered API version
        var provider = services.BuildServiceProvider().GetRequiredService<IApiVersionDescriptionProvider>();
        foreach (var description in provider.ApiVersionDescriptions)
        {
            services.AddOpenApi(description.GroupName, options => 
            {
                // Add document metadata
                options.AddDocumentTransformer((document, context, ct) => 
                {
                    document.Info.Title = $"Currency Converter API {description.GroupName.ToUpper()}";
                    document.Info.Version = description.ApiVersion.ToString();
                    document.Info.Description = "A production-ready currency conversion API with exchange rates, " +
                        "historical data, and caching. Authentication required for most endpoints. " +
                        "Use the /api/v1/auth/login endpoint to get a JWT token, then click the 'Authorize' button above to use it.";
                    return Task.CompletedTask;
                });

                // Configure OAuth2 security for the OpenAPI document
                options.AddSchemaTransformer((schema, context, ct) =>
                {
                    // This allows Swagger UI to show the authorize button
                    return Task.CompletedTask;
                });
            });
        }

        return services;
    }
}