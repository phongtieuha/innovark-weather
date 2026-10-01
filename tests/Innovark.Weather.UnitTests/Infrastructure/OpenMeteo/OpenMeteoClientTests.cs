using System.Net;
using System.Text.Json;
using System.Web;
using Innovark.Weather.Application.Models;
using Innovark.Weather.Infrastructure.OpenMeteo;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Innovark.Weather.UnitTests.Infrastructure.OpenMeteo;

public class OpenMeteoClientTests
{
    private static readonly TimeSpan Plus7 = TimeSpan.FromHours(7);
    private static readonly Uri BaseUrl = new("https://archive-api.open-meteo.com/");

    // Real archive response for the UTC days 2026-09-27 and 2026-09-28 (48 hours), timezone=GMT.
    private static readonly string TwoDayFixture =
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "open-meteo-archive-2026-09-27_28.json"));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // --- Request ---

    [Fact]
    public async Task GetHourlyAsync_SameUtcDayWindow_RequestsOneUtcDayWithExpectedParameters()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, TwoDayFixture);

        // 08:00 to 17:00 in UTC+7 is 01:00Z to 10:00Z, all on 2026-09-28.
        await GetHourlyAsync(handler, At(28, 8), At(28, 17));

        var request = handler.Requests.ShouldHaveSingleItem();
        request.Method.ShouldBe(HttpMethod.Get);
        request.RequestUri!.GetLeftPart(UriPartial.Path).ShouldBe("https://archive-api.open-meteo.com/v1/archive");

        var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
        query["latitude"].ShouldBe("10.762622");
        query["longitude"].ShouldBe("106.660172");
        query["hourly"].ShouldBe("temperature_2m,relative_humidity_2m");
        query["timezone"].ShouldBe("GMT");
        query["start_date"].ShouldBe("2026-09-28");
        query["end_date"].ShouldBe("2026-09-28");
    }

    [Fact]
    public async Task GetHourlyAsync_WindowCrossingUtcMidnight_RequestsBothUtcDays()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, TwoDayFixture);

        // 05:00 to 14:00 in UTC+7 is 22:00Z on 2026-09-27 to 07:00Z on 2026-09-28.
        await GetHourlyAsync(handler, At(28, 5), At(28, 14));

        var query = HttpUtility.ParseQueryString(handler.Requests.ShouldHaveSingleItem().RequestUri!.Query);
        query["start_date"].ShouldBe("2026-09-27");
        query["end_date"].ShouldBe("2026-09-28");
    }

    // Regression: the window up to 00:00 in UTC+7 on 2026-09-28 ends at 17:00Z on 2026-09-27. Asking
    // for Vietnam's date 2026-09-28 failed with 400 from Open-Meteo until 07:00 UTC+7, because it only
    // accepts dates up to its current UTC date.
    [Fact]
    public async Task GetHourlyAsync_WindowUpToMidnightInUtcPlus7_DoesNotRequestTheUtcPlus7Date()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, TwoDayFixture);

        await GetHourlyAsync(handler, At(27, 15), At(28, 0));

        var query = HttpUtility.ParseQueryString(handler.Requests.ShouldHaveSingleItem().RequestUri!.Query);
        query["start_date"].ShouldBe("2026-09-27");
        query["end_date"].ShouldBe("2026-09-27");
    }

    [Fact]
    public async Task GetHourlyAsync_WindowGivenInAnyOffset_RequestsTheSameUtcDays()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, TwoDayFixture);

        // The same instants as At(28, 5) to At(28, 14), given in UTC.
        await GetHourlyAsync(
            handler,
            new DateTimeOffset(2026, 9, 27, 22, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 28, 7, 0, 0, TimeSpan.Zero));

        var query = HttpUtility.ParseQueryString(handler.Requests.ShouldHaveSingleItem().RequestUri!.Query);
        query["start_date"].ShouldBe("2026-09-27");
        query["end_date"].ShouldBe("2026-09-28");
    }

    [Fact]
    public void Location_ReturnsConfiguredCoordinates()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(HttpStatusCode.OK, TwoDayFixture));

        var client = new OpenMeteoClient(httpClient, CreateOptions());

        client.Location.ShouldBe(new WeatherLocation(10.762622, 106.660172));
    }

    // --- Response mapping ---

    [Fact]
    public async Task GetHourlyAsync_TwoDayResponse_ReturnsOnlyWindowHoursOldestFirst()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, TwoDayFixture);

        var result = await GetHourlyAsync(handler, At(27, 18), At(28, 3));

        result.Select(r => r.Time).ShouldBe(Enumerable.Range(0, 10).Select(i => At(27, 18).AddHours(i)));
        result[0].ShouldBe(new(At(27, 18), 25.4, 93));
        result[^1].ShouldBe(new(At(28, 3), 25.5, 97));
    }

    [Fact]
    public async Task GetHourlyAsync_WindowUpToMidnightInUtcPlus7_ReturnsTheTenHours()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, TwoDayFixture);

        var result = await GetHourlyAsync(handler, At(27, 15), At(28, 0));

        result.Select(r => r.Time).ShouldBe(Enumerable.Range(0, 10).Select(i => At(27, 15).AddHours(i)));
    }

    [Fact]
    public async Task GetHourlyAsync_ParsedTimes_AreShownInUtcPlus7()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, TwoDayFixture);

        var result = await GetHourlyAsync(handler, At(28, 5), At(28, 14));

        result.Count.ShouldBe(10);
        result.ShouldAllBe(r => r.Time.Offset == Plus7);
    }

    [Fact]
    public async Task GetHourlyAsync_NullValues_ArePassedThrough()
    {
        var json = HourlyJson(["2026-09-27T22:00", "2026-09-27T23:00"], "[null, 25.3]", "[98, null]");
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, json);

        var result = await GetHourlyAsync(handler, At(28, 5), At(28, 6));

        result.ShouldBe([new(At(28, 5), null, 98), new(At(28, 6), 25.3, null)]);
    }

    [Theory]
    [InlineData("57", 57)]
    [InlineData("57.0", 57)]
    [InlineData("57.4", 57)]
    [InlineData("57.5", 58)]
    [InlineData("56.5", 57)]
    public async Task GetHourlyAsync_Humidity_IsRoundedHalfAwayFromZero(string humidity, int expected)
    {
        var json = HourlyJson(["2026-09-27T22:00"], "[25.3]", $"[{humidity}]");
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, json);

        var result = await GetHourlyAsync(handler, At(28, 5), At(28, 5));

        result.ShouldHaveSingleItem().RelativeHumidity.ShouldBe(expected);
    }

    // --- Invalid responses ---

    [Fact]
    public async Task GetHourlyAsync_UnexpectedUtcOffset_Throws()
    {
        // What the old request (timezone=Asia/Ho_Chi_Minh) returned: UTC+7 clock times.
        var json = HourlyJson(["2026-09-28T05:00"], "[25.3]", "[98]", utcOffsetSeconds: 25200);
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, json);

        var ex = await Should.ThrowAsync<OpenMeteoException>(() => GetHourlyAsync(handler, At(28, 5), At(28, 5)));

        ex.Message.ShouldContain("UTC offset");
    }

    [Fact]
    public async Task GetHourlyAsync_MismatchedArrayLengths_Throws()
    {
        var json = HourlyJson(["2026-09-27T22:00", "2026-09-27T23:00"], "[25.3]", "[98, 97]");
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, json);

        await Should.ThrowAsync<OpenMeteoException>(() => GetHourlyAsync(handler, At(28, 5), At(28, 6)));
    }

    [Fact]
    public async Task GetHourlyAsync_MissingHourlyData_Throws()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, """{ "utc_offset_seconds": 0 }""");

        await Should.ThrowAsync<OpenMeteoException>(() => GetHourlyAsync(handler, At(28, 5), At(28, 6)));
    }

    [Fact]
    public async Task GetHourlyAsync_UnrecognizedTimeValue_Throws()
    {
        var json = HourlyJson(["28/09/2026 05:00"], "[25.3]", "[98]");
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, json);

        var ex = await Should.ThrowAsync<OpenMeteoException>(() => GetHourlyAsync(handler, At(28, 5), At(28, 5)));

        ex.Message.ShouldContain("28/09/2026 05:00");
    }

    [Fact]
    public async Task GetHourlyAsync_InvalidJson_ThrowsWithJsonExceptionInside()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, "{ not json");

        var ex = await Should.ThrowAsync<OpenMeteoException>(() => GetHourlyAsync(handler, At(28, 5), At(28, 6)));

        ex.InnerException.ShouldBeOfType<JsonException>();
    }

    // --- Upstream errors ---

    [Fact]
    public async Task GetHourlyAsync_UpstreamErrorWithReason_ThrowsWithReasonAndStatus()
    {
        var handler = new StubHttpMessageHandler(
            HttpStatusCode.BadRequest, """{ "error": true, "reason": "Invalid timezone" }""");

        var ex = await Should.ThrowAsync<OpenMeteoException>(() => GetHourlyAsync(handler, At(28, 5), At(28, 6)));

        ex.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ex.Message.ShouldBe("Open-Meteo returned HTTP 400: Invalid timezone");
    }

    [Fact]
    public async Task GetHourlyAsync_UpstreamErrorWithoutJsonBody_ThrowsWithStatusOnly()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.BadGateway, "<html>Bad gateway</html>", "text/html");

        var ex = await Should.ThrowAsync<OpenMeteoException>(() => GetHourlyAsync(handler, At(28, 5), At(28, 6)));

        ex.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
        ex.Message.ShouldBe("Open-Meteo returned HTTP 502.");
    }

    // --- Helpers ---

    private static DateTimeOffset At(int day, int hour) => new(2026, 9, day, hour, 0, 0, Plus7);

    private static async Task<IReadOnlyList<HourlyWeather>> GetHourlyAsync(
        StubHttpMessageHandler handler, DateTimeOffset windowStart, DateTimeOffset windowEnd)
    {
        using var httpClient = new HttpClient(handler) { BaseAddress = BaseUrl };

        return await new OpenMeteoClient(httpClient, CreateOptions()).GetHourlyAsync(windowStart, windowEnd, Ct);
    }

    private static IOptions<OpenMeteoOptions> CreateOptions() =>
        Options.Create(new OpenMeteoOptions
        {
            BaseUrl = BaseUrl,
            Latitude = 10.762622,
            Longitude = 106.660172,
        });

    // Times are UTC clock readings, as Open-Meteo returns them for timezone=GMT.
    private static string HourlyJson(string[] times, string temperatures, string humidities, int utcOffsetSeconds = 0) =>
        $$"""
        {
          "utc_offset_seconds": {{utcOffsetSeconds}},
          "hourly": {
            "time": {{JsonSerializer.Serialize(times)}},
            "temperature_2m": {{temperatures}},
            "relative_humidity_2m": {{humidities}}
          }
        }
        """;
}
