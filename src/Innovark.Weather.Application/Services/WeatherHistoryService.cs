using System.Globalization;
using Innovark.Weather.Application.Abstractions;
using Innovark.Weather.Application.Common;
using Innovark.Weather.Application.Exceptions;
using Innovark.Weather.Application.Models;
using Innovark.Weather.Application.Validation;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace Innovark.Weather.Application.Services;

/// <summary>
/// Returns the requested hour and the 9 hours before it, newest first, for a date and hour in UTC+7.
/// </summary>
public sealed partial class WeatherHistoryService(
    HistoryRequestValidator validator,
    IOpenMeteoClient client,
    HybridCache cache,
    TimeProvider timeProvider,
    ILogger<WeatherHistoryService> logger)
{
    public const int RecordCount = 10;

    /// <summary>
    /// How long a window's records are cached. Recent hours are provisional model data that Open-Meteo
    /// can revise until ERA5 replaces them, so records are not cached forever.
    /// </summary>
    public static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    private static readonly HybridCacheEntryOptions CacheEntryOptions = new()
    {
        Expiration = CacheDuration,
        LocalCacheExpiration = CacheDuration,
    };

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

        var (records, fromCache) = await GetRecordsAsync(windowStart, windowEnd, ct);

        var location = client.Location;
        return WeatherHistoryResult.Success(
            new WeatherHistoryResponse(
                new LocationInfo(location.Latitude, location.Longitude, TimeZones.VietnamOffsetText),
                windowEnd,
                records),
            fromCache);
    }

    // Caches the checked records, not the raw upstream rows: if the data is incomplete, ToRecords throws
    // inside the factory, nothing is cached, and the next request tries Open-Meteo again. HybridCache also
    // lets only one of several concurrent requests for the same window call Open-Meteo.
    private async Task<(IReadOnlyList<WeatherRecord> Records, bool FromCache)> GetRecordsAsync(
        DateTimeOffset windowStart, DateTimeOffset windowEnd, CancellationToken ct)
    {
        var location = client.Location;
        var key = string.Create(
            CultureInfo.InvariantCulture,
            $"weather:{location.Latitude}:{location.Longitude}:{windowStart.UtcDateTime:yyyy-MM-dd'T'HH:mm'Z'}");

        var fetched = false;
        var records = await cache.GetOrCreateAsync<IReadOnlyList<WeatherRecord>>(
            key,
            async token =>
            {
                fetched = true;
                var started = timeProvider.GetTimestamp();

                var hours = await client.GetHourlyAsync(windowStart, windowEnd, token);
                var checkedRecords = ToRecords(hours, windowStart, windowEnd);

                var elapsedMilliseconds = timeProvider.GetElapsedTime(started).TotalMilliseconds;
                LogFetched(logger, key, elapsedMilliseconds);

                return checkedRecords;
            },
            CacheEntryOptions,
            cancellationToken: ct);

        if (!fetched)
        {
            LogCacheHit(logger, key);
        }

        return (records, FromCache: !fetched);
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Cache miss for {CacheKey}; fetched from Open-Meteo in {ElapsedMilliseconds:F0} ms")]
    private static partial void LogFetched(ILogger logger, string cacheKey, double elapsedMilliseconds);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cache hit for {CacheKey}")]
    private static partial void LogCacheHit(ILogger logger, string cacheKey);

    // e.g. 2026-09-28T14:00+07:00
    private static string Format(DateTimeOffset time) =>
        time.ToOffset(TimeZones.Vietnam).ToString("yyyy-MM-dd'T'HH:mmzzz", CultureInfo.InvariantCulture);
}
