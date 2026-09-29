namespace Innovark.Weather.Application.Models;

/// <summary>The weather history for a requested hour: that hour and the 9 before it, newest first.</summary>
public sealed record WeatherHistoryResponse(
    LocationInfo Location,
    DateTimeOffset RequestedTime,
    IReadOnlyList<WeatherRecord> Records);

/// <summary>The location in the response, with the UTC offset all times are given in (e.g. <c>+07:00</c>).</summary>
public sealed record LocationInfo(double Latitude, double Longitude, string UtcOffset);
