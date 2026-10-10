using Innovark.Weather.Web.ApiClients;
using Innovark.Weather.Web.WeatherApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Innovark.Weather.Web.Tests.ApiClients;

public class DependencyInjectionTests
{
    private static ServiceProvider BuildProvider(string? baseUrl)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["WeatherApi:BaseUrl"] = baseUrl })
            .Build();

        return new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddWeatherApiClient()
            .BuildServiceProvider();
    }

    [Fact]
    public void AddWeatherApiClient_BindsTheBaseUrl()
    {
        using var provider = BuildProvider("http://localhost:5122");

        provider.GetRequiredService<IOptions<WeatherApiOptions>>().Value.BaseUrl.ShouldBe("http://localhost:5122");
    }

    [Fact]
    public void AddWeatherApiClient_PointsTheHttpClientAtTheApi()
    {
        using var provider = BuildProvider("http://weather-api:8080");

        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(WeatherApiClientFactory));

        client.BaseAddress.ShouldBe(new Uri("http://weather-api:8080"));
    }

    [Fact]
    public void AddWeatherApiClient_WaitsLongerThanTheApisOwnBudget()
    {
        using var provider = BuildProvider("http://localhost:5122");

        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(WeatherApiClientFactory));

        // The API gives Open-Meteo 15 seconds, retries included, so it always answers first with its
        // own 503 rather than this call timing out.
        client.Timeout.ShouldBe(TimeSpan.FromSeconds(20));
        client.Timeout.ShouldBeGreaterThan(TimeSpan.FromSeconds(15));
    }

    [Fact]
    public void AddWeatherApiClient_RegistersTheKiotaClientAsTransient()
    {
        // Transient, like the typed HttpClient it's built on.
        var descriptor = new ServiceCollection().AddWeatherApiClient()
            .Single(d => d.ServiceType == typeof(WeatherApiClient));

        descriptor.Lifetime.ShouldBe(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddWeatherApiClient_ResolvesTheKiotaClient()
    {
        using var provider = BuildProvider("http://localhost:5122");

        provider.GetRequiredService<WeatherApiClient>().ShouldNotBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a url")]
    public void AddWeatherApiClient_WithoutAValidBaseUrl_FailsValidation(string? baseUrl)
    {
        using var provider = BuildProvider(baseUrl);

        var exception = Should.Throw<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<WeatherApiOptions>>().Value);

        exception.Failures.ShouldHaveSingleItem().ShouldContain(nameof(WeatherApiOptions.BaseUrl));
    }

    [Fact]
    public void App_WithoutTheApisBaseUrl_FailsToStart()
    {
        // Production has no appsettings for it, so a missing environment variable stops the app at
        // startup (ValidateOnStart) instead of failing on the first request.
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseEnvironment("Production")
            .UseSetting("WeatherApi:BaseUrl", ""));

        Should.Throw<OptionsValidationException>(() => factory.CreateClient());
    }
}
