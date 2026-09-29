using Innovark.Weather.Application.Abstractions;
using Innovark.Weather.Infrastructure;
using Innovark.Weather.Infrastructure.OpenMeteo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Innovark.Weather.UnitTests.Infrastructure;

public class DependencyInjectionTests
{
    private static readonly Dictionary<string, string?> ValidSettings = new()
    {
        ["OpenMeteo:BaseUrl"] = "https://archive-api.open-meteo.com/",
        ["OpenMeteo:Latitude"] = "10.762622",
        ["OpenMeteo:Longitude"] = "106.660172",
        ["OpenMeteo:TimeoutSeconds"] = "7",
    };

    [Fact]
    public void AddInfrastructure_ValidConfiguration_RegistersConfiguredClient()
    {
        using var provider = BuildProvider(ValidSettings);

        provider.GetRequiredService<IOpenMeteoClient>().ShouldBeOfType<OpenMeteoClient>();

        // Typed clients are named after the interface they are registered for.
        var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(IOpenMeteoClient));
        httpClient.BaseAddress.ShouldBe(new Uri("https://archive-api.open-meteo.com/"));
        httpClient.Timeout.ShouldBe(TimeSpan.FromSeconds(7));
    }

    [Theory]
    [InlineData("OpenMeteo:Latitude", "200", "Latitude")]
    [InlineData("OpenMeteo:Longitude", "-181", "Longitude")]
    [InlineData("OpenMeteo:TimeoutSeconds", "0", "TimeoutSeconds")]
    public void AddInfrastructure_OutOfRangeValue_FailsValidation(string key, string value, string member)
    {
        using var provider = BuildProvider(new(ValidSettings) { [key] = value });

        var ex = Should.Throw<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<OpenMeteoOptions>>().Value);

        ex.Message.ShouldContain(member);
    }

    [Theory]
    [InlineData("OpenMeteo:BaseUrl", "BaseUrl")]
    [InlineData("OpenMeteo:Latitude", "Latitude")]
    [InlineData("OpenMeteo:Longitude", "Longitude")]
    public void AddInfrastructure_MissingRequiredValue_FailsValidation(string key, string member)
    {
        var settings = new Dictionary<string, string?>(ValidSettings);
        settings.Remove(key);
        using var provider = BuildProvider(settings);

        var ex = Should.Throw<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<OpenMeteoOptions>>().Value);

        ex.Message.ShouldContain($"The {member} field is required.");
    }

    private static ServiceProvider BuildProvider(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new ServiceCollection().AddInfrastructure(configuration).BuildServiceProvider();
    }
}
