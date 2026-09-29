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

        return services;
    }
}
