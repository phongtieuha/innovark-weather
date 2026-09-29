using Innovark.Weather.Application.Abstractions;
using Innovark.Weather.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Innovark.Weather.UnitTests.Infrastructure.OpenMeteo;

/// <summary>
/// Calls the real Open-Meteo API to check that it still behaves the way the client and the saved
/// fixture assume, for example field names, recent hours in the archive and local dates with
/// <c>timezone</c>. The offline tests cannot catch such changes, because they replay a response
/// captured on 2026-09-29.
/// </summary>
/// <remarks>
/// <para>
/// The test is explicit, so a normal <c>dotnet test</c> (locally and in CI) skips it and fails only
/// when our code is broken:
/// </para>
/// <list type="bullet">
/// <item><description>It needs network access, which CI runners and offline machines may not have.</description></item>
/// <item><description>An Open-Meteo outage, slow response or 5xx would fail the build without a code change.</description></item>
/// <item><description>The free API has fair-use rate limits; running on every push would spend them for nothing.</description></item>
/// <item><description>It asks for "yesterday" and recent hours are provisional, so data differs between runs and failures are hard to reproduce.</description></item>
/// <item><description>A real HTTPS round-trip is far slower than the offline tests.</description></item>
/// </list>
/// <para>
/// Run it when changing the client or when Open-Meteo may have changed:
/// <c>dotnet test --project tests/Innovark.Weather.UnitTests -- --explicit only</c>
/// </para>
/// </remarks>
public class OpenMeteoClientLiveTests
{
    private static readonly TimeSpan Plus7 = TimeSpan.FromHours(7);

    [Fact(Explicit = true)]
    [Trait("Category", "Live")]
    public async Task GetHourlyAsync_RealApi_ReturnsTenCompleteHoursAcrossMidnight()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["OpenMeteo:BaseUrl"] = "https://archive-api.open-meteo.com/",
            ["OpenMeteo:Latitude"] = "10.762622",
            ["OpenMeteo:Longitude"] = "106.660172",
        }).Build();
        using var provider = new ServiceCollection().AddInfrastructure(configuration).BuildServiceProvider();
        var client = provider.GetRequiredService<IOpenMeteoClient>();

        // Yesterday 03:00 back to the day before at 18:00 (UTC+7): a window that spans two local days.
        var today = DateTimeOffset.UtcNow.ToOffset(Plus7).Date;
        var windowEnd = new DateTimeOffset(today.AddDays(-1).AddHours(3), Plus7);
        var windowStart = windowEnd.AddHours(-9);

        var result = await client.GetHourlyAsync(windowStart, windowEnd, TestContext.Current.CancellationToken);

        result.Select(r => r.Time).ShouldBe(Enumerable.Range(0, 10).Select(i => windowStart.AddHours(i)));
        result.ShouldAllBe(r => r.TemperatureC != null && r.RelativeHumidity != null);
    }
}
