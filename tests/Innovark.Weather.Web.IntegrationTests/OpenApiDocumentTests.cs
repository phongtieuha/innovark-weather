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

        var operation = document.RootElement.GetProperty("paths").GetProperty("/api/weather/history").GetProperty("post");

        // The operation ID names the generated hook: usePostWeatherHistory.
        operation.GetProperty("operationId").GetString().ShouldBe("PostWeatherHistory");
        operation.GetProperty("responses").EnumerateObject().Select(r => r.Name)
            .ShouldBe(["200", "400", "502", "503"], ignoreOrder: true);
    }

    [Fact]
    public async Task OpenApiDocument_NumbersAreOnlyNumbers()
    {
        using var document = await GetDocumentAsync();
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");

        // Strict number handling: "integer"/"number", not ["integer", "string"], so the generated
        // TypeScript types are `number`, not `number | string`.
        Property(schemas, "WeatherHistoryRequest", "hour").GetProperty("type").GetString().ShouldBe("integer");
        Property(schemas, "WeatherHistoryRecord", "temperatureC").GetProperty("type").GetString().ShouldBe("number");
        Property(schemas, "WeatherHistoryRecord", "relativeHumidity").GetProperty("type").GetString().ShouldBe("integer");
    }

    [Fact]
    public async Task PostHistory_WithHourAsString_ReturnsBadRequest()
    {
        await using var factory = new WebAppFactory(StubWeatherApiHandler.Json(HttpStatusCode.OK, "{}"));
        using var content = new StringContent("""{ "date": "2026-09-28", "hour": "14" }""", System.Text.Encoding.UTF8, "application/json");

        var response = await factory.CreateClient().PostAsync("/api/weather/history", content, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private static JsonElement Property(JsonElement schemas, string schema, string property) =>
        schemas.GetProperty(schema).GetProperty("properties").GetProperty(property);
}
