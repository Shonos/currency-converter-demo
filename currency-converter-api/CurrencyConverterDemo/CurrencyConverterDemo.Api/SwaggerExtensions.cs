using Asp.Versioning.ApiExplorer;

public static class SwaggerExtensions
{
    public static WebApplication UseSwaggerConfiguration(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwaggerUI(options =>
            {
                var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    options.SwaggerEndpoint($"/openapi/{description.GroupName}.json", $"API {description.GroupName.ToUpper()}");
                }
            });
        }
        return app;
    }
}