using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;

namespace Innovark.Weather.Web.IntegrationTests;

public class WeatherEndpointsTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static readonly object ValidRequest = new { date = "2026-09-28", hour = 14 };
    private static readonly DateTimeOffset RequestedTime = new(2026, 9, 28, 14, 0, 0, TimeSpan.FromHours(7));

    // The API's snake_case contract, shortened to two records.
    private const string HistoryJson = """
        {
          "location": { "latitude": 10.762622, "longitude": 106.660172, "utc_offset": "+07:00" },
          "requested_time": "2026-09-28T14:00:00+07:00",
          "records": [
            { "current_time": "2026-09-28T14:00:00+07:00", "temperature_c": 33.1, "temperature_f": 91.6, "relative_humidity": 57 },
            { "current_time": "2026-09-28T13:00:00+07:00", "temperature_c": 32.4, "temperature_f": 90.3, "relative_humidity": 60 }
          ]
        }
        """;

    private static async Task<(HttpResponseMessage Response, JsonElement Body)> PostAsync(
        StubWeatherApiHandler weatherApi, object request)
    {
        await using var factory = new WebAppFactory(weatherApi);
        var response = await factory.CreateClient().PostAsJsonAsync("/api/weather/history", request, Ct);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        return (response, body);
    }

    [Fact]
    public async Task PostHistory_CallsTheApiWithTheDateAndHour()
    {
        var weatherApi = StubWeatherApiHandler.Json(HttpStatusCode.OK, HistoryJson);

        await PostAsync(weatherApi, ValidRequest);

        weatherApi.LastRequest.ShouldNotBeNull();
        weatherApi.LastRequest.Method.ShouldBe(HttpMethod.Get);
        weatherApi.LastRequest.RequestUri.ShouldBe(
            new Uri("http://localhost:5122/api/v1/weather/history?date=2026-09-28&hour=14"));
    }

    [Fact]
    public async Task PostHistory_ReturnsTheRecordsInCamelCase()
    {
        var (response, body) = await PostAsync(StubWeatherApiHandler.Json(HttpStatusCode.OK, HistoryJson), ValidRequest);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("requestedTime").GetDateTimeOffset().ShouldBe(RequestedTime);
        var records = body.GetProperty("records").EnumerateArray().ToList();
        records.Count.ShouldBe(2);
        records[0].GetProperty("time").GetDateTimeOffset().ShouldBe(RequestedTime);
        records[0].GetProperty("temperatureC").GetDouble().ShouldBe(33.1);
        records[0].GetProperty("temperatureF").GetDouble().ShouldBe(91.6);
        records[0].GetProperty("relativeHumidity").GetInt32().ShouldBe(57);
    }

    [Fact]
    public async Task PostHistory_WhenTheApiRejectsTheHour_ReturnsItsValidationErrors()
    {
        var weatherApi = StubWeatherApiHandler.Json(HttpStatusCode.BadRequest, """
            {
              "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
              "title": "One or more validation errors occurred.",
              "status": 400,
              "errors": { "hour": ["The hour must be no more than 72 hours before the current hour."] }
            }
            """, "application/problem+json");

        var (response, body) = await PostAsync(weatherApi, ValidRequest);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        body.GetProperty("title").GetString().ShouldBe("One or more validation errors occurred.");
        body.GetProperty("errors").GetProperty("hour").EnumerateArray().Select(e => e.GetString())
            .ShouldBe(["The hour must be no more than 72 hours before the current hour."]);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task PostHistory_WhenTheApiReturnsAProblem_ReturnsItWithTheSameStatus(HttpStatusCode status)
    {
        var weatherApi = StubWeatherApiHandler.Json(status, $$"""
            { "title": "Weather provider unavailable.", "status": {{(int)status}}, "detail": "Open-Meteo timed out." }
            """, "application/problem+json");

        var (response, body) = await PostAsync(weatherApi, ValidRequest);

        response.StatusCode.ShouldBe(status);
        body.GetProperty("title").GetString().ShouldBe("Weather provider unavailable.");
        body.GetProperty("detail").GetString().ShouldBe("Open-Meteo timed out.");
    }

    [Fact]
    public async Task PostHistory_WhenTheApiReturnsAnUndocumentedError_ReturnsBadGateway()
    {
        var weatherApi = StubWeatherApiHandler.Json(
            HttpStatusCode.InternalServerError, """{ "title": "An error occurred.", "status": 500 }""", "application/problem+json");

        var (response, body) = await PostAsync(weatherApi, ValidRequest);

        response.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
        body.GetProperty("title").GetString().ShouldBe("The weather service returned an error.");
    }

    [Fact]
    public async Task PostHistory_WhenTheApiReturnsIncompleteRecords_ReturnsBadGateway()
    {
        var weatherApi = StubWeatherApiHandler.Json(HttpStatusCode.OK, """
            { "requested_time": "2026-09-28T14:00:00+07:00", "records": [{ "current_time": "2026-09-28T14:00:00+07:00" }] }
            """);

        var (response, body) = await PostAsync(weatherApi, ValidRequest);

        response.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
        body.GetProperty("title").GetString().ShouldBe("The weather service returned incomplete data.");
    }

    [Fact]
    public async Task PostHistory_WhenTheApiIsUnreachable_ReturnsServiceUnavailable()
    {
        var (response, body) = await PostAsync(
            StubWeatherApiHandler.Throws(new HttpRequestException("Connection refused")), ValidRequest);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        body.GetProperty("title").GetString().ShouldBe("The weather service is unavailable. Try again in a moment.");
    }

    [Fact]
    public async Task PostHistory_WhenTheApiTimesOut_ReturnsServiceUnavailable()
    {
        var (response, _) = await PostAsync(
            StubWeatherApiHandler.Throws(new TaskCanceledException("The request timed out.")), ValidRequest);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task PostHistory_WithMalformedDate_ReturnsBadRequestWithoutCallingTheApi()
    {
        var weatherApi = StubWeatherApiHandler.Json(HttpStatusCode.OK, HistoryJson);

        var (response, _) = await PostAsync(weatherApi, new { date = "28/09/2026", hour = 14 });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        weatherApi.LastRequest.ShouldBeNull();
    }
}
