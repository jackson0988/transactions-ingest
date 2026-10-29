namespace TransactionsIngest.Api.Extensions;

internal static class WebApplicationExtensions
{
    public static WebApplication UseApi(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseCors(ServiceCollectionExtensions.FrontendCorsPolicy);

        // built angular app can be dropped into wwwroot and served from here
        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.MapControllers();
        app.MapFallbackToFile("index.html");

        return app;
    }
}
