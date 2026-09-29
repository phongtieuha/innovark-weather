using Innovark.Weather.Application;
using Innovark.Weather.Application.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Innovark.Weather.UnitTests.Application;

public class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_RegistersValidatorAndSystemClock()
    {
        using var provider = new ServiceCollection().AddApplication().BuildServiceProvider();

        provider.GetRequiredService<HistoryRequestValidator>().ShouldNotBeNull();
        provider.GetRequiredService<TimeProvider>().ShouldBeSameAs(TimeProvider.System);
    }

    [Fact]
    public void AddApplication_KeepsTimeProviderRegisteredEarlier()
    {
        var fakeClock = new FakeTimeProvider();
        using var provider = new ServiceCollection()
            .AddSingleton<TimeProvider>(fakeClock)
            .AddApplication()
            .BuildServiceProvider();

        provider.GetRequiredService<TimeProvider>().ShouldBeSameAs(fakeClock);
    }
}
