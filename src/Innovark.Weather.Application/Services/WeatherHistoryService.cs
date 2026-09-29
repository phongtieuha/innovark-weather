using System.Globalization;

using Innovark.Weather.Application.Abstractions;
using Innovark.Weather.Application.Common;
using Innovark.Weather.Application.Exceptions;
using Innovark.Weather.Application.Models;
using Innovark.Weather.Application.Validation;

namespace Innovark.Weather.Application.Services;

/// <summary>
/// Returns the requested hour and the 9 hours before it, newest first, for a date and hour in UTC+7.
/// </summary>
public sealed class WeatherHistoryService(HistoryRequestValidator validator, IOpenMeteoClient client)
{
    public const int RecordCount = 10;

    public async Task<WeatherHistoryResult> GetHistoryAsync(DateOnly date, int hour, CancellationToken ct)
    {
        var validation = validator.Validate(date, hour);
        if (!validation.IsValid)
        {
            return WeatherHistoryResult.Invalid(validation.Errors);
        }

        // "Starting from the specified time and counting backwards": the requested hour is the first record.
        var windowEnd = validation.RequestedTime.Value;
        var windowStart = windowEnd.AddHours(-(RecordCount - 1));

        var hours = await client.GetHourlyAsync(windowStart, windowEnd, ct);
        var records = ToRecords(hours, windowStart, windowEnd);

        var location = client.Location;
        return WeatherHistoryResult.Success(new WeatherHistoryResponse(
            new LocationInfo(location.Latitude, location.Longitude, TimeZones.VietnamOffsetText),
            windowEnd,
            records));
    }

    private static List<WeatherRecord> ToRecords(
        IReadOnlyList<HourlyWeather> hours, DateTimeOffset windowStart, DateTimeOffset windowEnd)
    {
        // Compare against the exact expected hours, not just the count: this also catches gaps,
        // duplicate hours and shifted windows.
        var expected = Enumerable.Range(0, RecordCount).Select(i => windowStart.AddHours(i));
        if (!hours.Select(h => h.Time).Order().SequenceEqual(expected))
        {
            throw new IncompleteWeatherDataException(
                $"Expected the {RecordCount} hours from {Format(windowStart)} to {Format(windowEnd)}, " +
                $"but Open-Meteo returned {hours.Count} hour(s) that do not match.");
        }

        var missing = hours.FirstOrDefault(h => h.TemperatureC is null || h.RelativeHumidity is null);
        if (missing is not null)
        {
            throw new IncompleteWeatherDataException(
                $"Open-Meteo returned no temperature or humidity for {Format(missing.Time)}.");
        }

        return hours
            .OrderByDescending(h => h.Time)
            .Select(h => new WeatherRecord(
                CurrentTime: h.Time.ToOffset(TimeZones.Vietnam),
                TemperatureC: h.TemperatureC!.Value,
                TemperatureF: Temperature.ToFahrenheit(h.TemperatureC.Value),
                RelativeHumidity: h.RelativeHumidity!.Value))
            .ToList();
    }

    // e.g. 2026-09-28T14:00+07:00
    private static string Format(DateTimeOffset time) =>
        time.ToOffset(TimeZones.Vietnam).ToString("yyyy-MM-dd'T'HH:mmzzz", CultureInfo.InvariantCulture);
}
