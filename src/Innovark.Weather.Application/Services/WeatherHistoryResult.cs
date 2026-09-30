using System.Diagnostics.CodeAnalysis;
using Innovark.Weather.Application.Models;

namespace Innovark.Weather.Application.Services;

/// <summary>
/// Either the weather history, or validation errors keyed by request field. Invalid input is a
/// normal outcome, so it is returned rather than thrown.
/// </summary>
public sealed class WeatherHistoryResult
{
    private WeatherHistoryResult(
        WeatherHistoryResponse? response, IReadOnlyDictionary<string, string[]> errors, bool fromCache)
    {
        Response = response;
        Errors = errors;
        FromCache = fromCache;
    }

    /// <summary>Set only when the request was valid.</summary>
    public WeatherHistoryResponse? Response { get; }

    public IReadOnlyDictionary<string, string[]> Errors { get; }

    /// <summary>True when the records were served from the cache rather than fetched from Open-Meteo.</summary>
    public bool FromCache { get; }

    [MemberNotNullWhen(true, nameof(Response))]
    public bool IsSuccess => Response is not null;

    internal static WeatherHistoryResult Success(WeatherHistoryResponse response, bool fromCache) =>
        new(response, new Dictionary<string, string[]>(), fromCache);

    internal static WeatherHistoryResult Invalid(IReadOnlyDictionary<string, string[]> errors) =>
        new(null, errors, fromCache: false);
}
