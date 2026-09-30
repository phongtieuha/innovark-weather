using Innovark.Weather.Web.ApiClients;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Innovark.Weather.Web.IntegrationTests;

/// <summary>Runs the real web app in memory, replacing only the network under the weather API client.</summary>
internal sealed class WebAppFactory(StubWeatherApiHandler weatherApi) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(services =>
            services.AddHttpClient<WeatherApiClientFactory>().ConfigurePrimaryHttpMessageHandler(() => weatherApi));
}
