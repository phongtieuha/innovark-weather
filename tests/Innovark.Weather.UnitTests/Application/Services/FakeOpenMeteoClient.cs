using Innovark.Weather.Application.Abstractions;
using Innovark.Weather.Application.Models;

namespace Innovark.Weather.UnitTests.Application.Services;

/// <summary>
/// Stands in for the Open-Meteo client in service tests. By default it returns a complete window
/// (one row per requested hour, oldest first); <see cref="Respond"/> overrides that.
/// </summary>
internal sealed class FakeOpenMeteoClient : IOpenMeteoClient
{
    public WeatherLocation Location { get; } = new(10.762622, 106.660172);

    /// <summary>
    /// Every call the service under test made to this client, in order, with the window it asked for.
    /// </summary>
    /// <remarks>
    /// No real HTTP request is sent in unit tests, but in production each call here is a request to
    /// Open-Meteo. Recording calls lets a test check behavior the service's result cannot show, such
    /// as an invalid request never reaching Open-Meteo (the result is <c>Invalid</c> either way).
    /// Prefer asserting on the result; use this only when the result cannot reveal the behavior.
    /// </remarks>
    public List<(DateTimeOffset WindowStart, DateTimeOffset WindowEnd)> Calls { get; } = [];

    /// <summary>
    /// Builds the rows returned for each call, from the <c>(windowStart, windowEnd)</c> the service asked for.
    /// Defaults to <see cref="CompleteWindow"/>: a well-behaved upstream with one complete row per hour.
    /// </summary>
    /// <remarks>
    /// Replace it to simulate a badly behaved upstream. Starting from <see cref="CompleteWindow"/> and
    /// changing one thing keeps each test focused on the difference it checks, for example:
    /// <code>
    /// // 9 hours instead of 10
    /// _client.Respond = (start, end) => FakeOpenMeteoClient.CompleteWindow(start, end).Skip(1).ToList();
    /// </code>
    /// A function, rather than a fixed list, lets one fake serve any requested window, including
    /// windows that cross midnight, without hand-written timestamps in every test.
    /// </remarks>
    public Func<DateTimeOffset, DateTimeOffset, IReadOnlyList<HourlyWeather>> Respond { get; set; } = CompleteWindow;

    public Task<IReadOnlyList<HourlyWeather>> GetHourlyAsync(
        DateTimeOffset windowStart, DateTimeOffset windowEnd, CancellationToken ct)
    {
        Calls.Add((windowStart, windowEnd));
        return Task.FromResult(Respond(windowStart, windowEnd));
    }

    /// <summary>One row per hour, with temperatures 20.0, 21.0, … and humidity 60, 61, … in time order.</summary>
    public static IReadOnlyList<HourlyWeather> CompleteWindow(DateTimeOffset windowStart, DateTimeOffset windowEnd) =>
        Enumerable.Range(0, (int)(windowEnd - windowStart).TotalHours + 1)
            .Select(i => new HourlyWeather(windowStart.AddHours(i), 20.0 + i, 60 + i))
            .ToList();
}
