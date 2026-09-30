using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Innovark.Weather.Application.Models;
using Innovark.Weather.Application.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Innovark.Weather.Api.Endpoints;

public static class WeatherEndpoints
{
    // RFC 9211: which cache handled the request, and whether it was a hit or had to go upstream.
    internal const string CacheStatusHeader = "Cache-Status";
    internal const string CacheStatusHit = "innovark-weather; hit";
    internal const string CacheStatusMiss = "innovark-weather; fwd=miss";

    public static IEndpointRouteBuilder MapWeatherEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/weather").WithTags("Weather");

        group.MapGet("/history", GetHistoryAsync)
            .WithName("GetWeatherHistory")
            .WithSummary("Get 10 hourly weather records for Ho Chi Minh City")
            .WithDescription(
                "Returns the requested hour and the 9 hours before it, newest first. " +
                "The date and hour are in UTC+7 and must be no later than the current hour " +
                "and no more than 72 hours before it.")
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return app;
    }

    // Endpoints only bind input, call the service and map its result to HTTP.
    private static async Task<Results<Ok<WeatherHistoryResponse>, ValidationProblem>> GetHistoryAsync(
        [Description("Date in UTC+7, e.g. 2026-09-28.")] DateOnly date,
        [Description("Hour in UTC+7, 0–23.")][Range(0, 23)] int hour,
        WeatherHistoryService service,
        HttpResponse response,
        CancellationToken ct)
    {
        var result = await service.GetHistoryAsync(date, hour, ct);

        if (!result.IsSuccess)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>(result.Errors));
        }

        response.Headers[CacheStatusHeader] = result.FromCache ? CacheStatusHit : CacheStatusMiss;
        return TypedResults.Ok(result.Response);
    }
}
