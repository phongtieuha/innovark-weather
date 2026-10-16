using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;

namespace Innovark.Weather.Web.Tests;

public class HomePageTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Home_InDevelopment_LoadsTheEntryFromTheViteDevServer()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync(Ct);
        html.ShouldContain("<div id=\"app\"></div>");
        html.ShouldContain("src=\"http://localhost:5173/Pages/Home/Home.ts\"");
    }

    [Fact]
    public async Task Home_InProduction_LoadsTheBuiltBundle()
    {
        using var webRoot = new BuiltWebRoot();
        await using var production = Production(webRoot.Path);

        var response = await production.CreateClient().GetAsync("/", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync(Ct);
        html.ShouldContain("<div id=\"app\"></div>");
        html.ShouldContain("<script type=\"module\" src=\"/js/build/Pages/Home/Home-B1a2C3d4.js\"></script>");
        html.ShouldContain("<link rel=\"stylesheet\" href=\"/js/build/assets/Home-E5f6G7h8.css\" />");
        html.ShouldNotContain("localhost:5173");
    }

    [Fact]
    public async Task Home_InProduction_SendsHsts()
    {
        using var webRoot = new BuiltWebRoot();
        await using var production = Production(webRoot.Path);
        var client = production.CreateClient(new WebApplicationFactoryClientOptions
        {
            // HSTS is only sent over HTTPS, and never for localhost.
            BaseAddress = new Uri("https://weather.example.com"),
        });

        var response = await client.GetAsync("/", Ct);

        response.Headers.GetValues("Strict-Transport-Security").ShouldHaveSingleItem().ShouldStartWith("max-age=");
    }

    [Fact]
    public async Task Home_InDevelopment_SendsNoHsts()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://weather.example.com"),
        });

        var response = await client.GetAsync("/", Ct);

        response.Headers.Contains("Strict-Transport-Security").ShouldBeFalse();
    }

    // Production has no appsettings for the API's URL (docker-compose sets it), so the test supplies one.
    private static WebApplicationFactory<Program> Production(string webRootPath) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseEnvironment("Production")
            .UseSetting("WeatherApi:BaseUrl", "http://localhost:5122")
            .UseWebRoot(webRootPath));

    /// <summary>A web root holding what `bun run build` writes for the Home page: hashed JS and CSS.</summary>
    private sealed class BuiltWebRoot : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory("innovark-web-root-").FullName;

        public BuiltWebRoot()
        {
            Write("js/build/Pages/Home/Home-B1a2C3d4.js", "export {}");
            Write("js/build/assets/Home-E5f6G7h8.css", "body {}");
        }

        public void Dispose() => Directory.Delete(Path, recursive: true);

        private void Write(string relativePath, string content)
        {
            var file = System.IO.Path.Combine(Path, relativePath);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(file)!);
            File.WriteAllText(file, content);
        }
    }
}
