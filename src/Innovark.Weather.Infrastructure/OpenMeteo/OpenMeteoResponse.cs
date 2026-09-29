using System.Text.Json.Serialization;

namespace Innovark.Weather.Infrastructure.OpenMeteo;

internal sealed record OpenMeteoResponse(
    [property: JsonPropertyName("utc_offset_seconds")] int UtcOffsetSeconds,
    [property: JsonPropertyName("hourly")] OpenMeteoHourly? Hourly);

internal sealed record OpenMeteoHourly(
    [property: JsonPropertyName("time")] IReadOnlyList<string>? Time,
    [property: JsonPropertyName("temperature_2m")] IReadOnlyList<double?>? Temperature2m,
    [property: JsonPropertyName("relative_humidity_2m")] IReadOnlyList<double?>? RelativeHumidity2m);

internal sealed record OpenMeteoErrorResponse(
    [property: JsonPropertyName("error")] bool Error,
    [property: JsonPropertyName("reason")] string? Reason);

[JsonSerializable(typeof(OpenMeteoResponse))]
[JsonSerializable(typeof(OpenMeteoErrorResponse))]
internal sealed partial class OpenMeteoJsonContext : JsonSerializerContext;
