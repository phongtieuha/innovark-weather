using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;

namespace Innovark.Weather.Web.Tests;

/// <summary>Bodiless error responses get a ProblemDetails body (UseStatusCodePages).</summary>
public class StatusCodePagesTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("/does-not-exist")]
    [InlineData("/api/weather/forecast")]
    public async Task UnknownPath_ReturnsNotFoundProblemDetails(string path)
    {
        await using var factory = new WebAppFactory(StubWeatherApiHandler.Json(HttpStatusCode.OK, "{}"));

        var response = await factory.CreateClient().GetAsync(path, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        body.GetProperty("status").GetInt32().ShouldBe(404);
    }

    [Fact]
    public async Task WrongMethod_ReturnsMethodNotAllowedProblemDetails()
    {
        await using var factory = new WebAppFactory(StubWeatherApiHandler.Json(HttpStatusCode.OK, "{}"));

        var response = await factory.CreateClient().PostAsync("/api/weather/history?date=2026-09-28&hour=14", null, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
    }
}
