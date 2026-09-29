using Innovark.Weather.Application.Models;

namespace Innovark.Weather.Application.Abstractions;

/// <summary>Source of hourly weather data for the configured location.</summary>
public interface IOpenMeteoClient
{
    /// <summary>The configured coordinates all data is fetched for.</summary>
    WeatherLocation Location { get; }

    /// <summary>
    /// Gets the hours from <paramref name="windowStart"/> to <paramref name="windowEnd"/>, both inclusive,
    /// oldest first. The caller works out this window from the requested date and hour.
    /// </summary>
    /// <remarks>
    /// Values are returned as the provider reported them, so hours may be missing or contain nulls;
    /// the caller decides whether the data is complete.
    /// </remarks>
    Task<IReadOnlyList<HourlyWeather>> GetHourlyAsync(DateTimeOffset windowStart, DateTimeOffset windowEnd, CancellationToken ct);
}
