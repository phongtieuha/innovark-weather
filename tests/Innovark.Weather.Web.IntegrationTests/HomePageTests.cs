using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace Innovark.Weather.Web.IntegrationTests;

public class HomePageTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Home_InDevelopment_LoadsTheEntryFromTheViteDevServer()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        html.ShouldContain("<div id=\"app\"></div>");
        html.ShouldContain("src=\"http://localhost:5173/Pages/Home/Home.ts\"");
    }
}
