namespace Innovark.Weather.Application.Common;

public static class TimeZones
{
    /// <summary>IANA time zone name for Vietnam, as understood by Open-Meteo.</summary>
    public const string VietnamId = "Asia/Ho_Chi_Minh";

    /// <summary>
    /// Vietnam's UTC offset. Vietnam has no daylight saving time, so a fixed offset is used instead of
    /// <see cref="TimeZoneInfo"/>: chiseled container images ship without tzdata.
    /// </summary>
    public static readonly TimeSpan Vietnam = TimeSpan.FromHours(7);
}
