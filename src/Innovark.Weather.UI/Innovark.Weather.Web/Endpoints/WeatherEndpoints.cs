using Innovark.Weather.Web.ErrorHandling;
using Innovark.Weather.Web.WeatherApi;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Kiota.Abstractions;
using ApiModels = Innovark.Weather.Web.WeatherApi.Models;

namespace Innovark.Weather.Web.Endpoints;

public static class WeatherEndpoints
{
    public static IEndpointRouteBuilder MapWeatherEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/weather").WithTags("Weather");

        // The operation ID names the hook Orval generates for the page: usePostWeatherHistory.
        group.MapPost("/history", GetHistoryAsync)
            .WithName("PostWeatherHistory")
            .WithSummary("Get the 10 hourly records for a date and hour in UTC+7 from the weather API")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return app;
    }

    // Calls Innovark.Weather.Api through its Kiota client and returns the records in this app's own
    // contract. Errors are thrown and turned into ProblemDetails by GlobalExceptionHandler.
    private static async Task<Ok<WeatherHistoryResponse>> GetHistoryAsync(
        WeatherHistoryRequest request,
        WeatherApiClient api,
        CancellationToken ct)
    {
        var history = await api.Api.V1.Weather.History.GetAsync(config =>
        {
            config.QueryParameters.Date = new Date(request.Date.Year, request.Date.Month, request.Date.Day);
            config.QueryParameters.Hour = request.Hour;
        }, ct);

        return TypedResults.Ok(WeatherHistoryResponse.From(history));
    }
}

/// <summary>The home page form: a date and hour in UTC+7.</summary>
public sealed record WeatherHistoryRequest(DateOnly Date, int Hour);

public sealed record WeatherHistoryResponse(DateTimeOffset RequestedTime, IReadOnlyList<WeatherHistoryRecord> Records)
{
    // The API always sends every value, so a missing one means the two disagree on the contract.
    internal static WeatherHistoryResponse From(ApiModels.WeatherHistoryResponse? history) =>
        history is { RequestedTime: { } requestedTime, Records: { } records }
            ? new WeatherHistoryResponse(requestedTime, [.. records.Select(WeatherHistoryRecord.From)])
            : throw new WeatherApiContractException("The weather API's response has no requested time or records.");
}

public sealed record WeatherHistoryRecord(DateTimeOffset Time, double TemperatureC, double TemperatureF, int RelativeHumidity)
{
    internal static WeatherHistoryRecord From(ApiModels.WeatherRecord record) =>
        record is { CurrentTime: { } time, TemperatureC: { } c, TemperatureF: { } f, RelativeHumidity: { } humidity }
            ? new WeatherHistoryRecord(time, c, f, humidity)
            : throw new WeatherApiContractException("A record from the weather API is missing a value.");
}
