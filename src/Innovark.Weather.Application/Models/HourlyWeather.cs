namespace Innovark.Weather.Application.Models;

/// <summary>
/// One hour of weather data as reported by the upstream provider.
/// Values are nullable because the provider can return gaps; completeness is checked by the caller.
/// </summary>
public sealed record HourlyWeather(DateTimeOffset Time, double? TemperatureC, int? RelativeHumidity);
