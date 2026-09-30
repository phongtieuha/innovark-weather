using System.Net;
using System.Text.Json;
using Shouldly;

namespace Innovark.Weather.IntegrationTests;

public class WeatherHistoryEndpointTests
{
    private const string Endpoint = "/api/v1/weather/history";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // --- 200 ---

    [Fact]
    public async Task Get_ValidRequest_Returns200WithTenRecordsNewestFirst()
    {
        await using var factory = new WeatherApiFactory(StubOpenMeteoHandler.Fixture());

        var response = await factory.CreateClient().GetAsync($"{Endpoint}?date=2026-09-28&hour=14", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/json");

        using var json = await ReadJsonAsync(response);
        var root = json.RootElement;
        root.GetProperty("requested_time").GetString().ShouldBe("2026-09-28T14:00:00+07:00");
        root.GetProperty("records").EnumerateArray()
            .Select(r => r.GetProperty("current_time").GetString())
            .ShouldBe(Enumerable.Range(0, 10).Select(i => $"2026-09-28T{14 - i:00}:00:00+07:00"));
    }

    [Fact]
    public async Task Get_ValidRequest_UsesSnakeCaseFieldsAndUpstreamValues()
    {
        await using var factory = new WeatherApiFactory(StubOpenMeteoHandler.Fixture());

        var response = await factory.CreateClient().GetAsync($"{Endpoint}?date=2026-09-28&hour=14", Ct);

        using var json = await ReadJsonAsync(response);
        var root = json.RootElement;

        var location = root.GetProperty("location");
        location.GetProperty("latitude").GetDouble().ShouldBe(10.762622);
        location.GetProperty("longitude").GetDouble().ShouldBe(106.660172);
        location.GetProperty("utc_offset").GetString().ShouldBe("+07:00");

        var records = root.GetProperty("records");
        var newest = records[0];
        newest.EnumerateObject().Select(p => p.Name)
            .ShouldBe(["current_time", "temperature_c", "temperature_f", "relative_humidity"]);

        // Values from the saved Open-Meteo response: 14:00 was 33.1 °C / 57%, 05:00 was 25.3 °C / 98%.
        newest.GetProperty("temperature_c").GetDouble().ShouldBe(33.1);
        newest.GetProperty("temperature_f").GetDouble().ShouldBe(91.6);
        newest.GetProperty("relative_humidity").GetInt32().ShouldBe(57);

        var oldest = records[9];
        oldest.GetProperty("current_time").GetString().ShouldBe("2026-09-28T05:00:00+07:00");
        oldest.GetProperty("temperature_c").GetDouble().ShouldBe(25.3);
        oldest.GetProperty("temperature_f").GetDouble().ShouldBe(77.5);
        oldest.GetProperty("relative_humidity").GetInt32().ShouldBe(98);
    }

    [Fact]
    public async Task Get_EarlyHour_ReturnsWindowCrossingMidnight()
    {
        await using var factory = new WeatherApiFactory(StubOpenMeteoHandler.Fixture());

        var response = await factory.CreateClient().GetAsync($"{Endpoint}?date=2026-09-28&hour=1", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var json = await ReadJsonAsync(response);
        var times = json.RootElement.GetProperty("records").EnumerateArray()
            .Select(r => r.GetProperty("current_time").GetString())
            .ToList();
        times.Count.ShouldBe(10);
        times[0].ShouldBe("2026-09-28T01:00:00+07:00");
        times[^1].ShouldBe("2026-09-27T16:00:00+07:00");
    }

    // --- 400 ---

    [Theory]
    [InlineData("hour=15", "Requested time 2026-09-28T15:00+07:00 is in the future.")]
    [InlineData("hour=13&date=2026-09-25", "Requested time 2026-09-25T13:00+07:00 is more than 72 hours in the past.")]
    [InlineData("hour=24", "The field hour must be between 0 and 23.")]
    [InlineData("hour=-1", "The field hour must be between 0 and 23.")]
    public async Task Get_InvalidTime_Returns400ValidationProblemForHour(string query, string expectedError)
    {
        await using var factory = new WeatherApiFactory(StubOpenMeteoHandler.Fixture());
        var url = query.Contains("date=", StringComparison.Ordinal)
            ? $"{Endpoint}?{query}"
            : $"{Endpoint}?date=2026-09-28&{query}";

        var response = await factory.CreateClient().GetAsync(url, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        using var json = await ReadJsonAsync(response);
        json.RootElement.GetProperty("status").GetInt32().ShouldBe(400);
        json.RootElement.GetProperty("errors").GetProperty("hour")[0].GetString().ShouldBe(expectedError);
    }

    [Theory]
    [InlineData("date=2026-09-28")]               // missing hour
    [InlineData("hour=14")]                       // missing date
    [InlineData("date=28-09-2026&hour=14")]       // wrong date format
    [InlineData("date=2026-09-28&hour=abc")]      // hour not a number
    public async Task Get_MissingOrMalformedParameter_Returns400ProblemDetails(string query)
    {
        await using var factory = new WeatherApiFactory(StubOpenMeteoHandler.Fixture());

        var response = await factory.CreateClient().GetAsync($"{Endpoint}?{query}", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        using var json = await ReadJsonAsync(response);
        json.RootElement.GetProperty("detail").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Get_InvalidRequest_DoesNotCallOpenMeteo()
    {
        var openMeteo = StubOpenMeteoHandler.Fixture();
        await using var factory = new WeatherApiFactory(openMeteo);

        await factory.CreateClient().GetAsync($"{Endpoint}?date=2026-09-28&hour=15", Ct);

        // The 400 looks the same either way; only the count shows no upstream call was made.
        openMeteo.RequestCount.ShouldBe(0);
    }

    // --- 502 / 503 / 500 ---

    [Fact]
    public async Task Get_UpstreamError_Returns502WithReason()
    {
        var openMeteo = StubOpenMeteoHandler.Json(
            HttpStatusCode.InternalServerError, """{ "error": true, "reason": "Internal error" }""");
        await using var factory = new WeatherApiFactory(openMeteo);

        var response = await factory.CreateClient().GetAsync($"{Endpoint}?date=2026-09-28&hour=14", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
        using var json = await ReadJsonAsync(response);
        json.RootElement.GetProperty("title").GetString().ShouldBe("The weather provider returned an error.");
        json.RootElement.GetProperty("detail").GetString().ShouldBe("Open-Meteo returned HTTP 500: Internal error");
    }

    [Fact]
    public async Task Get_IncompleteUpstreamData_Returns502()
    {
        // 9 of the 10 hours: 05:00 is missing.
        var times = Enumerable.Range(6, 9).Select(h => $"\"2026-09-28T{h:00}:00\"");
        var openMeteo = StubOpenMeteoHandler.Json(HttpStatusCode.OK, $$"""
            {
              "utc_offset_seconds": 25200,
              "hourly": {
                "time": [{{string.Join(",", times)}}],
                "temperature_2m": [25, 26, 27, 28, 29, 30, 31, 32, 33],
                "relative_humidity_2m": [90, 85, 80, 75, 70, 65, 60, 58, 57]
              }
            }
            """);
        await using var factory = new WeatherApiFactory(openMeteo);

        var response = await factory.CreateClient().GetAsync($"{Endpoint}?date=2026-09-28&hour=14", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
        using var json = await ReadJsonAsync(response);
        json.RootElement.GetProperty("detail").GetString()!.ShouldStartWith("Expected the 10 hours");
    }

    [Fact]
    public async Task Get_UpstreamTimeout_Returns503()
    {
        await using var factory = new WeatherApiFactory(
            StubOpenMeteoHandler.NeverResponds(),
            new Dictionary<string, string?> { ["OpenMeteo:TimeoutSeconds"] = "1" });

        var response = await factory.CreateClient().GetAsync($"{Endpoint}?date=2026-09-28&hour=14", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        using var json = await ReadJsonAsync(response);
        json.RootElement.GetProperty("title").GetString().ShouldBe("The weather provider is unavailable.");
    }

    [Fact]
    public async Task Get_UpstreamUnreachable_Returns503()
    {
        await using var factory = new WeatherApiFactory(
            StubOpenMeteoHandler.Throws(new HttpRequestException("Connection refused")));

        var response = await factory.CreateClient().GetAsync($"{Endpoint}?date=2026-09-28&hour=14", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Get_UnexpectedError_Returns500WithoutExceptionDetails()
    {
        await using var factory = new WeatherApiFactory(
            StubOpenMeteoHandler.Throws(new InvalidOperationException("Sensitive internal detail")));

        var response = await factory.CreateClient().GetAsync($"{Endpoint}?date=2026-09-28&hour=14", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        var body = await response.Content.ReadAsStringAsync(Ct);
        body.ShouldContain("An unexpected error occurred.");
        body.ShouldNotContain("Sensitive internal detail");
        body.ShouldNotContain("InvalidOperationException");
        body.ShouldNotContain(" at ");   // no stack trace
    }

    // --- Other ---

    [Fact]
    public async Task Get_UnknownRoute_Returns404ProblemDetails()
    {
        await using var factory = new WeatherApiFactory(StubOpenMeteoHandler.Fixture());

        var response = await factory.CreateClient().GetAsync("/api/v1/weather/unknown", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) =>
        await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(Ct), cancellationToken: Ct);
}
