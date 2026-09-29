namespace Innovark.Weather.Application.Models;

/// <summary>One hour in the history response. All values are present; incomplete hours are rejected earlier.</summary>
public sealed record WeatherRecord(
    DateTimeOffset CurrentTime,
    double TemperatureC,
    double TemperatureF,
    int RelativeHumidity);
