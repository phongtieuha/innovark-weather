using Microsoft.Extensions.Options;

namespace Innovark.Weather.Web.ApiClients;

public static class DependencyInjection
{
    // Longer than the API's own 15-second budget for calling Open-Meteo (retries included), so the
    // API always answers first, with its own 503, instead of this call timing out.
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(20);

    public static IServiceCollection AddWeatherApiClient(this IServiceCollection services)
    {
        services.AddOptions<WeatherApiOptions>()
            .BindConfiguration(WeatherApiOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

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
