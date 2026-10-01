using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Innovark.Weather.Application.Abstractions;
using Innovark.Weather.Application.Common;
using Innovark.Weather.Application.Models;
using Microsoft.Extensions.Options;

namespace Innovark.Weather.Infrastructure.OpenMeteo;

internal sealed class OpenMeteoClient(HttpClient httpClient, IOptions<OpenMeteoOptions> options) : IOpenMeteoClient
{
    private const string ArchivePath = "v1/archive";   // Historical Weather API
    private const string HourlyFields = "temperature_2m,relative_humidity_2m";
    private const string TimeZoneId = "GMT";   // dates and times in UTC (checked in Map)
    private const string DateFormat = "yyyy-MM-dd";
    private const string HourFormat = "yyyy-MM-dd'T'HH:mm";

    private readonly OpenMeteoOptions _options = options.Value;

    // Latitude and Longitude are required and range-checked at startup (ValidateOnStart).
    public WeatherLocation Location => new(_options.Latitude!.Value, _options.Longitude!.Value);

    public async Task<IReadOnlyList<HourlyWeather>> GetHourlyAsync(
        DateTimeOffset windowStart, DateTimeOffset windowEnd, CancellationToken ct)
    {
        using var response = await httpClient.GetAsync(BuildRequestUri(windowStart, windowEnd), ct);

        if (!response.IsSuccessStatusCode)
        {
            throw await CreateUpstreamErrorAsync(response, ct);
        }

        OpenMeteoResponse? body;
        try
        {
            body = await response.Content.ReadFromJsonAsync(OpenMeteoJsonContext.Default.OpenMeteoResponse, ct);
        }
        catch (JsonException ex)
        {
            throw new OpenMeteoException("Open-Meteo returned a response that is not valid JSON.", ex);
        }

        // The archive API only accepts whole days, so trim to the requested hours. Both sides are
        // exact instants, so the comparison doesn't depend on the offsets.
        return Map(body)
            .Where(r => r.Time >= windowStart && r.Time <= windowEnd)
            .ToList();
    }

    // The days are UTC days, not Vietnam's. Open-Meteo accepts end_date only up to its current UTC
    // date, and from 00:00 to 07:00 UTC+7 Vietnam's date is already a day ahead of it, so asking for
    // Vietnam's days would be rejected for the most recent hours.
    internal Uri BuildRequestUri(DateTimeOffset windowStart, DateTimeOffset windowEnd)
    {
        (string Name, string Value)[] parameters =
        [
            ("latitude", Location.Latitude.ToString(CultureInfo.InvariantCulture)),
            ("longitude", Location.Longitude.ToString(CultureInfo.InvariantCulture)),
            ("hourly", HourlyFields),
            ("timezone", TimeZoneId),
            ("start_date", FormatUtcDate(windowStart)),
            ("end_date", FormatUtcDate(windowEnd)),
        ];

        var query = string.Join('&', parameters.Select(p => $"{p.Name}={Uri.EscapeDataString(p.Value)}"));
        return new Uri($"{ArchivePath}?{query}", UriKind.Relative);
    }

    private static string FormatUtcDate(DateTimeOffset time) =>
        time.UtcDateTime.ToString(DateFormat, CultureInfo.InvariantCulture);

    // Open-Meteo returns hourly data as parallel arrays (time[i], temperature_2m[i] and
    // relative_humidity_2m[i] describe the same hour). This turns them into one row per hour.
    // Only the shape is checked here; nulls are passed through for the service to judge.
    private static List<HourlyWeather> Map(OpenMeteoResponse? body)
    {
        var hourly = body?.Hourly
            ?? throw new OpenMeteoException("Open-Meteo response did not contain hourly data.");

        // All three arrays must be present and the same length, or index i would not line up.
        if (hourly.Time is not { } times
            || hourly.Temperature2m is not { } temperatures
            || hourly.RelativeHumidity2m is not { } humidities
            || temperatures.Count != times.Count
            || humidities.Count != times.Count)
        {
            throw new OpenMeteoException("Open-Meteo hourly data is missing fields or has mismatched lengths.");
        }

        // The days were sent as UTC days; any other applied offset means the returned hours are
        // not the ones requested.
        var offset = TimeSpan.FromSeconds(body.UtcOffsetSeconds);
        if (offset != TimeSpan.Zero)
        {
            throw new OpenMeteoException(
                $"Open-Meteo applied UTC offset {offset}, expected {TimeSpan.Zero} for {TimeZoneId}.");
        }

        // Lengths were checked above, so Zip pairs every hour's values without dropping any.
        return times
            .Zip(temperatures, humidities)
            .Select(hour => new HourlyWeather(
                Time: ParseUtcTime(hour.First).ToOffset(TimeZones.Vietnam),
                TemperatureC: hour.Second,
                RelativeHumidity: RoundHumidity(hour.Third)))
            .ToList();
    }

    // Times arrive as UTC clock readings without an offset, e.g. "2026-09-27T22:00".
    // They are parsed as DateTime (Kind = Unspecified) so no machine time zone is assumed;
    // DateTimeOffset parsing would fill in the server's offset instead. Attaching offset zero then
    // turns the reading into an exact instant, which Map shows in UTC+7: 2026-09-28T05:00+07:00.
    private static DateTimeOffset ParseUtcTime(string value) =>
        DateTime.TryParseExact(value, HourFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var utcTime)
            ? new DateTimeOffset(utcTime, TimeSpan.Zero)
            : throw new OpenMeteoException($"Open-Meteo returned an unrecognized time value '{value}'.");

    // Humidity is read as double so a value like 57.0 cannot fail deserialization, then rounded
    // (half away from zero, like °F) to the whole percentage the API returns.
    private static int? RoundHumidity(double? value) =>
        value is { } h ? (int)Math.Round(h, MidpointRounding.AwayFromZero) : null;

    private static async Task<OpenMeteoException> CreateUpstreamErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        string? reason = null;
        try
        {
            var error = await response.Content.ReadFromJsonAsync(OpenMeteoJsonContext.Default.OpenMeteoErrorResponse, ct);
            reason = error?.Reason;
        }
        catch (JsonException)
        {
            // Not Open-Meteo's JSON error body (e.g. a proxy error page); report the status code only.
        }

        var status = (int)response.StatusCode;
        var message = reason is null
            ? $"Open-Meteo returned HTTP {status}."
            : $"Open-Meteo returned HTTP {status}: {reason}";

        return new OpenMeteoException(message, response.StatusCode);
    }
}
