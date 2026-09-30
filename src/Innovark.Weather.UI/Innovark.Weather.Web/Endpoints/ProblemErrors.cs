using Microsoft.Kiota.Abstractions.Serialization;
using ApiModels = Innovark.Weather.Web.WeatherApi.Models;

namespace Innovark.Weather.Web.Endpoints;

/// <summary>
/// Reads a ValidationProblemDetails' <c>errors</c>. Its keys are dynamic (one per field), so Kiota
/// leaves them as untyped values in <c>AdditionalData</c>.
/// </summary>
internal static class ProblemErrors
{
    public static Dictionary<string, string[]> ToDictionary(ApiModels.HttpValidationProblemDetails_errors? errors) =>
        errors?.AdditionalData.ToDictionary(entry => entry.Key, entry => Messages(entry.Value)) ?? [];

    private static string[] Messages(object value) => value switch
    {
        UntypedArray array => [.. array.GetValue().Select(Message)],
        _ => [Message(value)],
    };

    private static string Message(object? value) => value switch
    {
        UntypedString text => text.GetValue() ?? "",
        _ => value?.ToString() ?? "",
    };
}
