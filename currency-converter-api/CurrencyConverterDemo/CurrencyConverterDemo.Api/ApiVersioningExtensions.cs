using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;

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
            services.AddOpenApi(description.GroupName, options => {
                options.AddDocumentTransformer((document, context, ct) => {
                    document.Info.Title = $"Currency Converter API {description.GroupName.ToUpper()}";
                    return Task.CompletedTask;
                });
            });
        }

        return services;
    }
}