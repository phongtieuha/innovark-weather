using System.Reflection;
using Microsoft.Extensions.Options;

namespace Innovark.Weather.Web.ApiClients;

public static class DependencyInjection
{
    // Longer than the API's own 15-second budget for calling Open-Meteo (retries included), so the
    // API always answers first, with its own 503, instead of this call timing out.
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(20);

    // The build writes the OpenAPI document by starting this app inside GetDocument.Insider, with no
    // environment's configuration. It only reads the endpoints, so the base URL isn't needed then.
    private static bool IsGeneratingOpenApiDocument =>
        Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";

    public static IServiceCollection AddWeatherApiClient(this IServiceCollection services)
    {
        var options = services.AddOptions<WeatherApiOptions>()
            .BindConfiguration(WeatherApiOptions.SectionName)
            .ValidateDataAnnotations();
        if (!IsGeneratingOpenApiDocument)
        {
            options.ValidateOnStart();
        }

        // No retries here: the API already retries Open-Meteo, and retrying on top of it would
        // multiply the calls.
        services.AddHttpClient<WeatherApiClientFactory>((sp, client) =>
        {
            client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<WeatherApiOptions>>().Value.BaseUrl);
            client.Timeout = Timeout;
        });
        services.AddTransient(sp => sp.GetRequiredService<WeatherApiClientFactory>().Create());

        return services;
    }
}
