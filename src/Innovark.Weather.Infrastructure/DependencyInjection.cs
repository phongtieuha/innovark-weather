using Innovark.Weather.Application.Abstractions;
using Innovark.Weather.Infrastructure.OpenMeteo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

namespace Innovark.Weather.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(OpenMeteoOptions.SectionName);

        services.AddOptions<OpenMeteoOptions>()
            .Bind(section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var resilience = services.AddHttpClient<IOpenMeteoClient, OpenMeteoClient>((sp, client) =>
            {
                client.BaseAddress = sp.GetRequiredService<IOptions<OpenMeteoOptions>>().Value.BaseUrl;

                // Timeouts come from the resilience pipeline below; HttpClient's own timeout (100 s by
                // default) must stay longer than the pipeline's total timeout, so it is left as is.
            })
            // Retry with backoff for transient failures, per-attempt and total timeouts, and a circuit
            // breaker.
            .AddStandardResilienceHandler();

        // Override the defaults from OpenMeteo:Resilience. Binding the named options, rather than passing
        // the section to AddStandardResilienceHandler, keeps the defaults when the section is missing
        // instead of failing at startup.
        services.Configure<HttpStandardResilienceOptions>(
            resilience.PipelineName, section.GetSection(OpenMeteoOptions.ResilienceSectionName));

        return services;
    }
}
