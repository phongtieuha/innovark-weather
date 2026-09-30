using Innovark.Weather.Application.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Innovark.Weather.IntegrationTests;

/// <summary>
/// Runs the real API in memory (Program.cs, routing, validation, JSON and ProblemDetails), replacing only
/// the clock and the network under the Open-Meteo client.
/// </summary>
internal sealed class WeatherApiFactory(StubOpenMeteoHandler openMeteo, IDictionary<string, string?>? settings = null)
    : WebApplicationFactory<Program>
{
    /// <summary>"Now" for every test: 2026-09-28 14:25 in UTC+7, so the current hour is 14:00.</summary>
    public static readonly DateTimeOffset Now = new(2026, 9, 28, 14, 25, 0, TimeSpan.FromHours(7));

    // Resilience settings short enough for tests: retries after 10 ms, attempts time out after 200 ms.
    // Tests can override any of them, e.g. to open the circuit after two failures.
    private static readonly Dictionary<string, string?> FastResilience = new()
    {
        ["OpenMeteo:Resilience:Retry:Delay"] = "00:00:00.010",
        ["OpenMeteo:Resilience:AttemptTimeout:Timeout"] = "00:00:00.200",
        ["OpenMeteo:Resilience:TotalRequestTimeout:Timeout"] = "00:00:02",
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        foreach (var (key, value) in FastResilience.Concat(settings ?? new Dictionary<string, string?>()))
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureTestServices(services =>
        {
            // Not FakeTimeProvider: see FixedNowTimeProvider for why timers must stay real here.
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(new FixedNowTimeProvider(Now));

            // Typed clients are named after their interface; this swaps only the network handler, so the
            // real OpenMeteoClient still builds the request and parses the response.
            services.AddHttpClient(nameof(IOpenMeteoClient)).ConfigurePrimaryHttpMessageHandler(() => openMeteo);
        });
    }
}
