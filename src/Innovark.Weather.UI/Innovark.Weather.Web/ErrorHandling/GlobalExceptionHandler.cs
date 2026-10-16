using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Kiota.Abstractions;
using ApiModels = Innovark.Weather.Web.WeatherApi.Models;

namespace Innovark.Weather.Web.ErrorHandling;

/// <summary>
/// Turns exceptions into RFC 9457 ProblemDetails responses, so endpoints only call the weather API and
/// return the result. The API's own ProblemDetails pass through with the same status (its validation
/// errors reach the page); its undocumented errors and contract breaks → 502; not reachable or no
/// answer in time → 503; anything else → 500 without details.
/// </summary>
internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // The caller disconnected; there is no one to send a response to.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            LogRequestAborted(logger);
            return true;
        }

        var problem = exception switch
        {
            // A malformed or missing body (thrown by parameter binding in Development).
            BadHttpRequestException badRequest => new ProblemDetails
            {
                Status = badRequest.StatusCode,
                Title = "The request is invalid.",
                Detail = badRequest.Message,
            },

            // Kiota throws the API's documented error responses as their generated models.
            ApiModels.HttpValidationProblemDetails validation => new ValidationProblemDetails(
                ProblemErrors.ToDictionary(validation.Errors))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = validation.Title,
            },

            ApiModels.ProblemDetails upstream => new ProblemDetails
            {
                Status = upstream.Status ?? upstream.ResponseStatusCode,
                Title = upstream.Title,
                Detail = upstream.Detail,
            },

            // A status the API doesn't document, e.g. its 500.
            ApiException => new ProblemDetails
            {
                Status = StatusCodes.Status502BadGateway,
                Title = "The weather service returned an error.",
            },

            WeatherApiContractException => new ProblemDetails
            {
                Status = StatusCodes.Status502BadGateway,
                Title = "The weather service returned incomplete data.",
            },

            // Not reachable, or no answer within the client's timeout (the caller is still connected,
            // checked above).
            HttpRequestException or TaskCanceledException => new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "The weather service is unavailable. Try again in a moment.",
            },

            // No exception details: they could expose internals.
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
            },
        };

        if (problem.Status == StatusCodes.Status500InternalServerError)
        {
            LogUnexpectedError(logger, exception);
        }
        else if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            LogWeatherApiFailure(logger, problem.Status.Value, exception.Message);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Request aborted by the client.")]
    private static partial void LogRequestAborted(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Weather API failure, returning {StatusCode}: {Reason}")]
    private static partial void LogWeatherApiFailure(ILogger logger, int statusCode, string reason);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception.")]
    private static partial void LogUnexpectedError(ILogger logger, Exception exception);
}
