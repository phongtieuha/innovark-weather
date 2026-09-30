using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace Innovark.Weather.Web.IntegrationTests;

public class WeatherEndpointsTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task PostHistory_ReturnsOkWithTheRequest()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/weather/history", new { date = "2026-09-28", hour = 14 }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<RequestBody>(TestContext.Current.CancellationToken);
        body.ShouldBe(new RequestBody("2026-09-28", 14));
    }

    [Fact]
    public async Task PostHistory_WithMalformedDate_ReturnsBadRequest()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/weather/history", new { date = "28/09/2026", hour = 14 }, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private sealed record RequestBody(string Date, int Hour);
}
