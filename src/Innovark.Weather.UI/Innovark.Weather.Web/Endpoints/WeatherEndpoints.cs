using Microsoft.AspNetCore.Http.HttpResults;

namespace Innovark.Weather.Web.Endpoints;

public static class WeatherEndpoints
{
    public static IEndpointRouteBuilder MapWeatherEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/weather");

        group.MapPost("/history", GetHistory);

        return app;
    }

    // Placeholder until it's wired to the API: returns 200 OK with the input it received.
    private static Ok<WeatherHistoryRequest> GetHistory(WeatherHistoryRequest request) => TypedResults.Ok(request);
}

/// <summary>The home page form: a date and hour in UTC+7.</summary>
public sealed record WeatherHistoryRequest(DateOnly Date, int Hour);
