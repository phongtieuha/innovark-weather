using System.Globalization;
using Innovark.Weather.Application.Common;

namespace Innovark.Weather.Application.Validation;

/// <summary>
/// Validates a history request given as a date and hour in UTC+7. The requested hour may be the
/// current hour, but not in the future and not more than 72 hours before the current hour.
/// </summary>
public sealed class HistoryRequestValidator(TimeProvider timeProvider)
{
    public const int MaxAgeHours = 72;

    // Time rules are reported under "hour", the field that pins the exact requested time.
    private const string HourField = "hour";

    public HistoryRequestValidationResult Validate(DateOnly date, int hour)
    {
        if (hour is < 0 or > 23)
        {
            return HistoryRequestValidationResult.Invalid(HourField, "Hour must be between 0 and 23.");
        }

        // Compare local clock values, both at +07:00. Building a DateTimeOffset first would throw
        // for extreme dates such as 0001-01-01, whose UTC equivalent is out of range.
        var requested = date.ToDateTime(new TimeOnly(hour, 0));
        var now = timeProvider.GetUtcNow().ToOffset(TimeZones.Vietnam).DateTime;
        var nowHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);

        if (requested > nowHour)
        {
            return HistoryRequestValidationResult.Invalid(
                HourField, $"Requested time {Format(requested)} is in the future.");
        }

        if (requested < nowHour.AddHours(-MaxAgeHours))
        {
            return HistoryRequestValidationResult.Invalid(
                HourField, $"Requested time {Format(requested)} is more than {MaxAgeHours} hours in the past.");
        }

        return HistoryRequestValidationResult.Valid(new DateTimeOffset(requested, TimeZones.Vietnam));
    }

    // e.g. 2026-09-28T18:00+07:00. Built from the local clock value, since extreme dates cannot be
    // represented as a DateTimeOffset.
    private static string Format(DateTime localTime) =>
        localTime.ToString("yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture) + TimeZones.VietnamOffsetText;
}
