using Innovark.Weather.Application.Abstractions;
using Innovark.Weather.Infrastructure;
using Innovark.Weather.Infrastructure.OpenMeteo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
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
        ["OpenMeteo:Resilience:AttemptTimeout:Timeout"] = "00:00:07",
        ["OpenMeteo:Resilience:Retry:MaxRetryAttempts"] = "4",
    };

    [Fact]
    public void AddInfrastructure_ValidConfiguration_RegistersConfiguredClient()
    {
        using var provider = BuildProvider(ValidSettings);

        provider.GetRequiredService<IOpenMeteoClient>().ShouldBeOfType<OpenMeteoClient>();

        // Typed clients are named after the interface they are registered for.
        var httpClient = provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(IOpenMeteoClient));
        httpClient.BaseAddress.ShouldBe(new Uri("https://archive-api.open-meteo.com/"));
    }

    [Fact]
    public void AddInfrastructure_ResilienceSettings_AreReadFromConfiguration()
    {
        using var provider = BuildProvider(ValidSettings);

        // The standard handler's options are named "<client name>-standard".
        var resilience = provider.GetRequiredService<IOptionsMonitor<HttpStandardResilienceOptions>>()
            .Get($"{nameof(IOpenMeteoClient)}-standard");

        resilience.AttemptTimeout.Timeout.ShouldBe(TimeSpan.FromSeconds(7));
        resilience.Retry.MaxRetryAttempts.ShouldBe(4);
    }

    [Fact]
    public void AddInfrastructure_NoResilienceSection_UsesLibraryDefaults()
    {
        var settings = ValidSettings
            .Where(s => !s.Key.StartsWith("OpenMeteo:Resilience", StringComparison.Ordinal))
            .ToDictionary();
        using var provider = BuildProvider(settings);

        var resilience = provider.GetRequiredService<IOptionsMonitor<HttpStandardResilienceOptions>>()
            .Get($"{nameof(IOpenMeteoClient)}-standard");

        resilience.AttemptTimeout.Timeout.ShouldBe(new HttpStandardResilienceOptions().AttemptTimeout.Timeout);
        provider.GetRequiredService<IOpenMeteoClient>().ShouldNotBeNull();
    }

    [Theory]
    [InlineData("OpenMeteo:Latitude", "200", "Latitude")]
    [InlineData("OpenMeteo:Longitude", "-181", "Longitude")]
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
