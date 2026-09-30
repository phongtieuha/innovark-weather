namespace Innovark.Weather.IntegrationTests;

/// <summary>
/// Freezes "now" but keeps real timers and timestamps.
/// </summary>
/// <remarks>
/// The validator only needs a fixed <see cref="GetUtcNow"/>. The resilience pipeline takes the same
/// <see cref="TimeProvider"/> from DI for its timeouts and retry delays, and with
/// <c>FakeTimeProvider</c> those would never elapse, so requests would hang. Inheriting
/// <see cref="TimeProvider"/>'s timers keeps them running in real time; tests keep them short through
/// the <c>OpenMeteo:Resilience</c> settings.
/// </remarks>
internal sealed class FixedNowTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
