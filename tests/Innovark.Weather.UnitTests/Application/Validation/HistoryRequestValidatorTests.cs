using Innovark.Weather.Application.Validation;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace Innovark.Weather.UnitTests.Application.Validation;

public class HistoryRequestValidatorTests
{
    private static readonly TimeSpan Plus7 = TimeSpan.FromHours(7);

    // "Now" for most tests: 2026-09-28 14:25 in UTC+7, so the current hour is 14:00.
    private static readonly DateTimeOffset Now = new(2026, 9, 28, 14, 25, 0, Plus7);

    [Fact]
    public void Validate_CurrentHour_IsValid()
    {
        var result = Validate(Now, new DateOnly(2026, 9, 28), 14);

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
        result.RequestedTime.ShouldBe(new DateTimeOffset(2026, 9, 28, 14, 0, 0, Plus7));
    }

    [Fact]
    public void Validate_ValidRequest_ReturnsRequestedTimeAtPlus7()
    {
        var result = Validate(Now, new DateOnly(2026, 9, 27), 1);

        result.IsValid.ShouldBeTrue();
        result.RequestedTime.Value.Offset.ShouldBe(Plus7);
        result.RequestedTime.Value.ShouldBe(new DateTimeOffset(2026, 9, 27, 1, 0, 0, Plus7));
    }

    [Fact]
    public void Validate_NextHour_IsInTheFuture()
    {
        var result = Validate(Now, new DateOnly(2026, 9, 28), 15);

        result.IsValid.ShouldBeFalse();
        result.RequestedTime.ShouldBeNull();
        result.Errors.ShouldContainKey("hour");
        result.Errors["hour"].ShouldBe(["Requested time 2026-09-28T15:00+07:00 is in the future."]);
    }

    [Fact]
    public void Validate_Exactly72HoursAgo_IsValid()
    {
        var result = Validate(Now, new DateOnly(2026, 9, 25), 14);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_73HoursAgo_IsTooOld()
    {
        var result = Validate(Now, new DateOnly(2026, 9, 25), 13);

        result.IsValid.ShouldBeFalse();
        result.Errors["hour"].ShouldBe(["Requested time 2026-09-25T13:00+07:00 is more than 72 hours in the past."]);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(24)]
    public void Validate_HourOutOfRange_IsInvalid(int hour)
    {
        var result = Validate(Now, new DateOnly(2026, 9, 28), hour);

        result.IsValid.ShouldBeFalse();
        result.Errors["hour"].ShouldBe(["Hour must be between 0 and 23."]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(23)]
    public void Validate_HourAtRangeBoundary_IsAccepted(int hour)
    {
        var result = Validate(Now, new DateOnly(2026, 9, 27), hour);

        result.IsValid.ShouldBeTrue();
    }

    // "Now" is 00:10 on 09-29 in UTC+7 but still 17:10 on 09-28 in UTC. Using the UTC date or
    // hour anywhere would put these on the wrong side of the rule.
    [Theory]
    [InlineData(2026, 9, 28, 23, true)]
    [InlineData(2026, 9, 29, 0, true)]
    [InlineData(2026, 9, 29, 1, false)]
    public void Validate_JustAfterMidnightInUtcPlus7_UsesLocalDate(int year, int month, int day, int hour, bool expectedValid)
    {
        var justAfterMidnight = new DateTimeOffset(2026, 9, 28, 17, 10, 0, TimeSpan.Zero);

        var result = Validate(justAfterMidnight, new DateOnly(year, month, day), hour);

        result.IsValid.ShouldBe(expectedValid);
    }

    [Fact]
    public void Validate_NowGivenInUtc_GivesSameResultAsPlus7()
    {
        var result = Validate(Now.ToUniversalTime(), new DateOnly(2026, 9, 28), 14);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ResultChangesAsTimeMovesOn()
    {
        var clock = new FakeTimeProvider(Now);
        var validator = new HistoryRequestValidator(clock);
        var date = new DateOnly(2026, 9, 27);

        validator.Validate(date, 1).IsValid.ShouldBeTrue();    // 37h before the current hour (14:00)

        clock.Advance(TimeSpan.FromHours(36));                  // now 2026-09-30 02:25
        validator.Validate(date, 1).IsValid.ShouldBeFalse();   // 73h before the current hour (02:00)
    }

    [Fact]
    public void Validate_ExtremeDates_ReturnErrorsInsteadOfThrowing()
    {
        Validate(Now, DateOnly.MinValue, 0).Errors["hour"][0].ShouldContain("in the past");
        Validate(Now, DateOnly.MaxValue, 23).Errors["hour"][0].ShouldContain("in the future");
    }

    private static HistoryRequestValidationResult Validate(DateTimeOffset now, DateOnly date, int hour) =>
        new HistoryRequestValidator(new FakeTimeProvider(now)).Validate(date, hour);
}
