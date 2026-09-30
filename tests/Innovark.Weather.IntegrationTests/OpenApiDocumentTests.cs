using System.Net;
using System.Text.Json;
using Shouldly;

namespace Innovark.Weather.IntegrationTests;

/// <summary>The OpenAPI document (served in Development) describes the endpoint as it actually behaves.</summary>
public class OpenApiDocumentTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task OpenApiDocument_DescribesHistoryEndpoint()
    {
        await using var factory = new WeatherApiFactory(StubOpenMeteoHandler.Fixture());

        var response = await factory.CreateClient().GetAsync("/openapi/v1.json", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(Ct);
        using var json = JsonDocument.Parse(body);
        var operation = json.RootElement.GetProperty("paths").GetProperty("/api/v1/weather/history").GetProperty("get");

        operation.GetProperty("responses").EnumerateObject().Select(r => r.Name)
            .ShouldBe(["200", "400", "502", "503"], ignoreOrder: true);

        var hour = operation.GetProperty("parameters").EnumerateArray()
            .Single(p => p.GetProperty("name").GetString() == "hour");
        hour.GetProperty("required").GetBoolean().ShouldBeTrue();
        hour.GetProperty("schema").GetProperty("minimum").GetInt32().ShouldBe(0);
        hour.GetProperty("schema").GetProperty("maximum").GetInt32().ShouldBe(23);

        // Schemas use the same snake_case names as the responses.
        body.ShouldContain("\"temperature_f\"");
        body.ShouldContain("\"requested_time\"");
    }
}
