using Innovark.Weather.Application.Exceptions;
using Innovark.Weather.Application.Models;
using Innovark.Weather.Application.Services;
using Innovark.Weather.Application.Validation;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Innovark.Weather.UnitTests.Application.Services;

public class WeatherHistoryServiceTests
{
    private static readonly TimeSpan Plus7 = TimeSpan.FromHours(7);

    // "Now": 2026-09-28 14:25 in UTC+7, so the current hour is 14:00.
    private static readonly DateTimeOffset Now = new(2026, 9, 28, 14, 25, 0, Plus7);

    private readonly FakeOpenMeteoClient _client = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // --- Validation ---

    [Fact]
    public async Task GetHistoryAsync_InvalidRequest_ReturnsErrorsWithoutCallingClient()
    {
        var result = await GetHistoryAsync(new DateOnly(2026, 9, 28), 15);

        result.IsSuccess.ShouldBeFalse();
        result.Response.ShouldBeNull();
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

    // --- Helpers ---

    private static DateTimeOffset At(int day, int hour) => new(2026, 9, day, hour, 0, 0, Plus7);

    private Task<WeatherHistoryResult> GetHistoryAsync(DateOnly date, int hour) =>
        new WeatherHistoryService(new HistoryRequestValidator(new FakeTimeProvider(Now)), _client)
            .GetHistoryAsync(date, hour, Ct);
}
