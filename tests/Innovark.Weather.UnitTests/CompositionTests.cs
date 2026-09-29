using Innovark.Weather.Application;
using Innovark.Weather.Application.Services;
using Innovark.Weather.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Innovark.Weather.UnitTests;

/// <summary>Checks that Application and Infrastructure registrations work together, as in Program.cs.</summary>
public class CompositionTests
{
    [Fact]
    public void AddApplicationAndInfrastructure_ResolveHistoryServiceWithScopeValidation()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["OpenMeteo:BaseUrl"] = "https://archive-api.open-meteo.com/",
            ["OpenMeteo:Latitude"] = "10.762622",
            ["OpenMeteo:Longitude"] = "106.660172",
        }).Build();

        // ValidateOnBuild fails on missing dependencies; ValidateScopes fails if a singleton depends on
        // a scoped service. A singleton holding the transient typed client is not detected here, which
        // is why AddApplication_RegistersHistoryServiceAsScoped checks the lifetime directly.
        using var provider = new ServiceCollection()
            .AddApplication()
            .AddInfrastructure(configuration)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<WeatherHistoryService>().ShouldNotBeNull();
    }
}
