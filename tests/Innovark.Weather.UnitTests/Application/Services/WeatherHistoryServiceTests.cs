using Innovark.Weather.Application.Exceptions;
using Innovark.Weather.Application.Models;
using Innovark.Weather.Application.Services;
using Innovark.Weather.Application.Validation;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Innovark.Weather.UnitTests.Application.Services;

public sealed class WeatherHistoryServiceTests : IDisposable
{
    private static readonly TimeSpan Plus7 = TimeSpan.FromHours(7);

    // "Now": 2026-09-28 14:25 in UTC+7, so the current hour is 14:00.
    private static readonly DateTimeOffset Now = new(2026, 9, 28, 14, 25, 0, Plus7);

    private readonly FakeOpenMeteoClient _client = new();

    // A real in-memory HybridCache per test (xUnit creates a new instance for each test), so calls within
    // a test share it and tests never see each other's entries.
    private readonly ServiceProvider _cacheProvider = new ServiceCollection().AddHybridCache().Services.BuildServiceProvider();
    private readonly WeatherHistoryService _service;

    public WeatherHistoryServiceTests()
    {
        var clock = new FakeTimeProvider(Now);
        _service = new WeatherHistoryService(
            new HistoryRequestValidator(clock),
            _client,
            _cacheProvider.GetRequiredService<HybridCache>(),
            clock,
            NullLogger<WeatherHistoryService>.Instance);
    }

    public void Dispose() => _cacheProvider.Dispose();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // --- Validation ---

    [Fact]
    public async Task GetHistoryAsync_InvalidRequest_ReturnsErrorsWithoutCallingClient()
    {
        var result = await GetHistoryAsync(new DateOnly(2026, 9, 28), 15);

        result.IsSuccess.ShouldBeFalse();
        result.Response.ShouldBeNull();
        result.FromCache.ShouldBeFalse();
        result.Errors["hour"].ShouldBe(["Requested time 2026-09-28T15:00+07:00 is in the future."]);

        // The result would be the same if the service fetched before validating; only the recorded
        // calls show that an invalid request never reaches Open-Meteo.
        _client.Calls.ShouldBeEmpty();
    }

    // --- Window ---

    [Fact]
    public async Task GetHistoryAsync_EarlyHour_WindowStartsOnPreviousDay()
    {
        var result = await GetHistoryAsync(new DateOnly(2026, 9, 28), 1);

        result.Response!.Records.Select(r => r.CurrentTime)
            .ShouldBe(Enumerable.Range(0, 10).Select(i => At(28, 1).AddHours(-i)));
        result.Response.Records[^1].CurrentTime.ShouldBe(At(27, 16));
    }

    // --- Response ---

    [Fact]
    public async Task GetHistoryAsync_CompleteData_ReturnsTenRecordsNewestFirst()
    {
        var result = await GetHistoryAsync(new DateOnly(2026, 9, 28), 14);

        result.IsSuccess.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
        result.Response.Records.Select(r => r.CurrentTime)
            .ShouldBe(Enumerable.Range(0, 10).Select(i => At(28, 14).AddHours(-i)));
    }

    [Fact]
    public async Task GetHistoryAsync_CompleteData_MapsValuesAndAddsFahrenheit()
    {
        var result = await GetHistoryAsync(new DateOnly(2026, 9, 28), 14);

        // The fake returns 20.0 °C / 60% for the oldest hour (05:00) up to 29.0 °C / 69% for 14:00.
        result.Response!.Records[0].ShouldBe(new WeatherRecord(At(28, 14), 29.0, 84.2, 69));
        result.Response.Records[^1].ShouldBe(new WeatherRecord(At(28, 5), 20.0, 68.0, 60));
    }

    [Fact]
    public async Task GetHistoryAsync_CompleteData_ReturnsLocationAndRequestedTime()
    {
        var result = await GetHistoryAsync(new DateOnly(2026, 9, 28), 14);

        result.Response!.Location.ShouldBe(new LocationInfo(10.762622, 106.660172, "+07:00"));
        result.Response.RequestedTime.ShouldBe(At(28, 14));
    }

    [Fact]
    public async Task GetHistoryAsync_UpstreamTimesInUtc_AreReturnedAtPlus7()
    {
        _client.Respond = (start, end) => FakeOpenMeteoClient.CompleteWindow(start, end)
            .Select(h => h with { Time = h.Time.ToUniversalTime() })
            .ToList();

        var result = await GetHistoryAsync(new DateOnly(2026, 9, 28), 14);

        result.Response!.Records.ShouldAllBe(r => r.CurrentTime.Offset == Plus7);
    }

    [Fact]
    public async Task GetHistoryAsync_UpstreamInAnyOrder_ReturnsNewestFirst()
    {
        _client.Respond = (start, end) => FakeOpenMeteoClient.CompleteWindow(start, end).Reverse().ToList();

        var result = await GetHistoryAsync(new DateOnly(2026, 9, 28), 14);

        result.Response!.Records[0].CurrentTime.ShouldBe(At(28, 14));
    }

    // --- Incomplete data ---

    [Fact]
    public async Task GetHistoryAsync_NineHours_ThrowsIncompleteData()
    {
        _client.Respond = (start, end) => FakeOpenMeteoClient.CompleteWindow(start, end).Skip(1).ToList();

        var ex = await Should.ThrowAsync<IncompleteWeatherDataException>(
            () => GetHistoryAsync(new DateOnly(2026, 9, 28), 14));

        ex.Message.ShouldContain("2026-09-28T05:00+07:00 to 2026-09-28T14:00+07:00");
    }

    [Fact]
    public async Task GetHistoryAsync_GapReplacedByDuplicateHour_ThrowsIncompleteData()
    {
        // Still 10 rows, but 07:00 is missing and 08:00 appears twice.
        _client.Respond = (start, end) =>
        {
            var hours = FakeOpenMeteoClient.CompleteWindow(start, end).ToList();
            hours[2] = hours[3];
            return hours;
        };

        await Should.ThrowAsync<IncompleteWeatherDataException>(() => GetHistoryAsync(new DateOnly(2026, 9, 28), 14));
    }

    [Fact]
    public async Task GetHistoryAsync_ShiftedWindow_ThrowsIncompleteData()
    {
        _client.Respond = (start, end) => FakeOpenMeteoClient.CompleteWindow(start.AddHours(1), end.AddHours(1));

        await Should.ThrowAsync<IncompleteWeatherDataException>(() => GetHistoryAsync(new DateOnly(2026, 9, 28), 14));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task GetHistoryAsync_NullValue_ThrowsIncompleteDataNamingTheHour(bool nullTemperature, bool nullHumidity)
    {
        _client.Respond = (start, end) =>
        {
            var hours = FakeOpenMeteoClient.CompleteWindow(start, end).ToList();
            hours[2] = hours[2] with
            {
                TemperatureC = nullTemperature ? null : hours[2].TemperatureC,
                RelativeHumidity = nullHumidity ? null : hours[2].RelativeHumidity,
            };
            return hours;
        };

        var ex = await Should.ThrowAsync<IncompleteWeatherDataException>(
            () => GetHistoryAsync(new DateOnly(2026, 9, 28), 14));

        ex.Message.ShouldContain("2026-09-28T07:00+07:00");
    }

    // --- Caching ---
    // The result is the same whether or not the cache was used; only the recorded calls show it.

    [Fact]
    public async Task GetHistoryAsync_SameRequestTwice_CallsClientOnce()
    {
        var first = await GetHistoryAsync(new DateOnly(2026, 9, 28), 14);
        var second = await GetHistoryAsync(new DateOnly(2026, 9, 28), 14);

        _client.Calls.Count.ShouldBe(1);
        first.FromCache.ShouldBeFalse();
        second.FromCache.ShouldBeTrue();
        second.Response!.Records.ShouldBe(first.Response!.Records);
    }

    [Fact]
    public async Task GetHistoryAsync_DifferentHours_AreCachedSeparately()
    {
        await GetHistoryAsync(new DateOnly(2026, 9, 28), 14);
        await GetHistoryAsync(new DateOnly(2026, 9, 28), 13);

        _client.Calls.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetHistoryAsync_IncompleteData_IsNotCached()
    {
        _client.Respond = (start, end) => FakeOpenMeteoClient.CompleteWindow(start, end).Skip(1).ToList();
        await Should.ThrowAsync<IncompleteWeatherDataException>(() => GetHistoryAsync(new DateOnly(2026, 9, 28), 14));

        _client.Respond = FakeOpenMeteoClient.CompleteWindow;
        var result = await GetHistoryAsync(new DateOnly(2026, 9, 28), 14);

        result.IsSuccess.ShouldBeTrue();
        result.FromCache.ShouldBeFalse();
        _client.Calls.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetHistoryAsync_CachedRecords_KeepPlus7Offset()
    {
        await GetHistoryAsync(new DateOnly(2026, 9, 28), 14);

        var cached = await GetHistoryAsync(new DateOnly(2026, 9, 28), 14);

        cached.Response!.Records.ShouldAllBe(r => r.CurrentTime.Offset == Plus7);
    }

    // --- Helpers ---

    private static DateTimeOffset At(int day, int hour) => new(2026, 9, day, hour, 0, 0, Plus7);

    private Task<WeatherHistoryResult> GetHistoryAsync(DateOnly date, int hour) =>
        _service.GetHistoryAsync(date, hour, Ct);
}
