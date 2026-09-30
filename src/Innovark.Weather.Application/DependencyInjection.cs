using Innovark.Weather.Application.Services;
using Innovark.Weather.Application.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Innovark.Weather.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // TryAdd, so a TimeProvider registered earlier (e.g. a FakeTimeProvider in tests) is kept.
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<HistoryRequestValidator>();

        // Scoped, not singleton: it depends on IOpenMeteoClient, a typed HttpClient that is transient
        // by design. A singleton would hold one client forever and defeat IHttpClientFactory's
        // handler rotation.
        services.AddScoped<WeatherHistoryService>();

        // In-memory cache with stampede protection. Registering an IDistributedCache (e.g. Redis) adds a
        // shared second level for multiple instances without code changes.
        services.AddHybridCache();

        return services;
    }
}
