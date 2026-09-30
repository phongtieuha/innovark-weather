using Innovark.Weather.Web.WeatherApi;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Kiota.Abstractions;
using ApiModels = Innovark.Weather.Web.WeatherApi.Models;

namespace Innovark.Weather.Web.Endpoints;

public static class WeatherEndpoints
{
    public static IEndpointRouteBuilder MapWeatherEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/weather");

        group.MapPost("/history", GetHistoryAsync);

        return app;
    }

    // Calls Innovark.Weather.Api through its Kiota client and returns the records in this app's own
    // contract. The API's errors come back as ProblemDetails with the same status, so the page
    // shows the API's validation messages.
    private static async Task<Results<Ok<WeatherHistoryResponse>, ValidationProblem, ProblemHttpResult>> GetHistoryAsync(
        WeatherHistoryRequest request,
        WeatherApiClient api,
        CancellationToken ct)
    {
        try
        {
            var history = await api.Api.V1.Weather.History.GetAsync(config =>
            {
                config.QueryParameters.Date = new Date(request.Date.Year, request.Date.Month, request.Date.Day);
                config.QueryParameters.Hour = request.Hour;
            }, ct);

            var response = history is null ? null : WeatherHistoryResponse.From(history);
            return response is null ? InvalidResponse() : TypedResults.Ok(response);
        }
        catch (ApiModels.HttpValidationProblemDetails problem)
        {
            return TypedResults.ValidationProblem(ProblemErrors.ToDictionary(problem.Errors), title: problem.Title);
        }
        catch (ApiModels.ProblemDetails problem)
        {
            return TypedResults.Problem(
                title: problem.Title, detail: problem.Detail, statusCode: problem.Status ?? problem.ResponseStatusCode);
        }
        catch (ApiException)
        {
            // A status the API doesn't document, e.g. its 500.
            return TypedResults.Problem(
                title: "The weather service returned an error.", statusCode: StatusCodes.Status502BadGateway);
        }
        catch (Exception ex) when (ex is HttpRequestException || (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            // Not reachable, or no answer within the client's timeout.
            return TypedResults.Problem(
                title: "The weather service is unavailable. Try again in a moment.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    private static ProblemHttpResult InvalidResponse() => TypedResults.Problem(
        title: "The weather service returned incomplete data.", statusCode: StatusCodes.Status502BadGateway);
}

/// <summary>The home page form: a date and hour in UTC+7.</summary>
public sealed record WeatherHistoryRequest(DateOnly Date, int Hour);

public sealed record WeatherHistoryResponse(DateTimeOffset RequestedTime, IReadOnlyList<WeatherHistoryRecord> Records)
{
    // Null when a required value is missing; the API always sends them, so that means a contract bug.
    internal static WeatherHistoryResponse? From(ApiModels.WeatherHistoryResponse history)
    {
        if (history.RequestedTime is not { } requestedTime || history.Records is not { } records)
        {
            return null;
        }

        var mapped = records.Select(WeatherHistoryRecord.From).ToList();
        return mapped.Contains(null) ? null : new WeatherHistoryResponse(requestedTime, mapped!);
    }
}

public sealed record WeatherHistoryRecord(DateTimeOffset Time, double TemperatureC, double TemperatureF, int RelativeHumidity)
{
    internal static WeatherHistoryRecord? From(ApiModels.WeatherRecord record) =>
        record is { CurrentTime: { } time, TemperatureC: { } c, TemperatureF: { } f, RelativeHumidity: { } humidity }
            ? new WeatherHistoryRecord(time, c, f, humidity)
            : null;
}
