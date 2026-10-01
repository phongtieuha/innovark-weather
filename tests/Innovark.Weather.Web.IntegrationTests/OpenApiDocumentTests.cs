using System.Net;
using System.Text.Json;
using Shouldly;

namespace Innovark.Weather.Web.IntegrationTests;

/// <summary>
/// The OpenAPI document (served in Development, and written to openapi.json on each Debug build) is
/// what Orval generates the page's client from, so its shape is the page's contract.
/// </summary>
public class OpenApiDocumentTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<JsonDocument> GetDocumentAsync()
    {
        await using var factory = new WebAppFactory(StubWeatherApiHandler.Json(HttpStatusCode.OK, "{}"));
        var response = await factory.CreateClient().GetAsync("/openapi/v1.json", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
    }

    [Fact]
    public async Task OpenApiDocument_DescribesHistoryEndpointForTheGeneratedHook()
    {
        using var document = await GetDocumentAsync();

        var operation = document.RootElement.GetProperty("paths").GetProperty("/api/weather/history").GetProperty("get");

        // The operation ID names the generated hook: useGetWeatherHistory.
        operation.GetProperty("operationId").GetString().ShouldBe("GetWeatherHistory");
        operation.GetProperty("responses").EnumerateObject().Select(r => r.Name)
            .ShouldBe(["200", "400", "502", "503"], ignoreOrder: true);

        // Required query parameters, so the generated GetWeatherHistoryParams has both, as non-optional.
        var parameters = operation.GetProperty("parameters").EnumerateArray()
            .ToDictionary(p => p.GetProperty("name").GetString()!);
        parameters.Keys.ShouldBe(["date", "hour"], ignoreOrder: true);
        parameters.Values.ShouldAllBe(p => p.GetProperty("in").GetString() == "query" && p.GetProperty("required").GetBoolean());
        parameters["date"].GetProperty("schema").GetProperty("format").GetString().ShouldBe("date");
        parameters["hour"].GetProperty("schema").GetProperty("type").GetString().ShouldBe("integer");
    }

    [Fact]
    public async Task OpenApiDocument_NumbersAreOnlyNumbers()
    {
        using var document = await GetDocumentAsync();
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");

        // Strict number handling: "integer"/"number", not ["integer", "string"], so the generated
        // TypeScript types are `number`, not `number | string`.
        Property(schemas, "WeatherHistoryRecord", "temperatureC").GetProperty("type").GetString().ShouldBe("number");
        Property(schemas, "WeatherHistoryRecord", "relativeHumidity").GetProperty("type").GetString().ShouldBe("integer");
    }

    [Theory]
    [InlineData("date=2026-09-28&hour=abc")]   // hour not a number
    [InlineData("date=2026-09-28")]            // missing hour
    [InlineData("hour=14")]                    // missing date
    public async Task GetHistory_WithInvalidQuery_ReturnsBadRequest(string query)
    {
        await using var factory = new WebAppFactory(StubWeatherApiHandler.Json(HttpStatusCode.OK, "{}"));

        var response = await factory.CreateClient().GetAsync($"/api/weather/history?{query}", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private static JsonElement Property(JsonElement schemas, string schema, string property) =>
        schemas.GetProperty(schema).GetProperty("properties").GetProperty(property);
}
