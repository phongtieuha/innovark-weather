using System.Diagnostics.CodeAnalysis;
using Innovark.Weather.Application.Models;

namespace Innovark.Weather.Application.Services;

/// <summary>
/// Either the weather history, or validation errors keyed by request field. Invalid input is a
/// normal outcome, so it is returned rather than thrown.
/// </summary>
public sealed class WeatherHistoryResult
{
    private WeatherHistoryResult(WeatherHistoryResponse? response, IReadOnlyDictionary<string, string[]> errors)
    {
        Response = response;
        Errors = errors;
    }

    /// <summary>Set only when the request was valid.</summary>
    public WeatherHistoryResponse? Response { get; }

    public IReadOnlyDictionary<string, string[]> Errors { get; }

    [MemberNotNullWhen(true, nameof(Response))]
    public bool IsSuccess => Response is not null;

    internal static WeatherHistoryResult Success(WeatherHistoryResponse response) =>
        new(response, new Dictionary<string, string[]>());

    internal static WeatherHistoryResult Invalid(IReadOnlyDictionary<string, string[]> errors) =>
        new(null, errors);
}
