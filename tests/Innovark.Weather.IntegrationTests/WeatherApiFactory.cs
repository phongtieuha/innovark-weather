using Innovark.Weather.Application.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        foreach (var (key, value) in settings ?? new Dictionary<string, string?>())
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(new FakeTimeProvider(Now));

            // Typed clients are named after their interface; this swaps only the network handler, so the
            // real OpenMeteoClient still builds the request and parses the response.
            services.AddHttpClient(nameof(IOpenMeteoClient)).ConfigurePrimaryHttpMessageHandler(() => openMeteo);
        });
    }
}
