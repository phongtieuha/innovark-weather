using Innovark.Weather.Application.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using Shouldly;

namespace Innovark.Weather.IntegrationTests;

/// <summary>
/// Checks the resilience settings the app ships with. The other integration tests shorten them, so only
/// this test would notice a value changing, or a misspelled key in appsettings.json silently falling
/// back to the library default.
/// </summary>
public class ResilienceSettingsTests
{
    [Fact]
    public async Task ShippedConfiguration_MatchesDocumentedResilienceSettings()
    {
        // Production, so only appsettings.json applies, as in the container.
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));

        // The standard handler's options are named "<client name>-standard".
        var options = factory.Services.GetRequiredService<IOptionsMonitor<HttpStandardResilienceOptions>>()
            .Get($"{nameof(IOpenMeteoClient)}-standard");

        options.TotalRequestTimeout.Timeout.ShouldBe(TimeSpan.FromSeconds(15));
        options.AttemptTimeout.Timeout.ShouldBe(TimeSpan.FromSeconds(5));
        options.Retry.MaxRetryAttempts.ShouldBe(2);
        options.Retry.Delay.ShouldBe(TimeSpan.FromMilliseconds(500));
        options.Retry.BackoffType.ShouldBe(DelayBackoffType.Exponential);
        options.Retry.UseJitter.ShouldBeTrue();
        options.CircuitBreaker.SamplingDuration.ShouldBe(TimeSpan.FromSeconds(30));
        options.CircuitBreaker.BreakDuration.ShouldBe(TimeSpan.FromSeconds(15));
    }
}
