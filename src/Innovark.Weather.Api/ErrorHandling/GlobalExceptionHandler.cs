using Innovark.Weather.Application.Exceptions;
using Innovark.Weather.Infrastructure.OpenMeteo;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Innovark.Weather.Api.ErrorHandling;

/// <summary>
/// Turns unhandled exceptions into RFC 9457 ProblemDetails responses:
/// upstream errors → 502, upstream timeouts or unreachable → 503, anything else → 500 without details.
/// </summary>
internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        // The caller disconnected; there is no one to send a response to.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            LogRequestAborted(logger);
            return true;
        }

        var problem = exception switch
        {
            // Malformed or missing query parameters (thrown by parameter binding).
            BadHttpRequestException badRequest => new ProblemDetails
            {
                Status = badRequest.StatusCode,
                Title = "The request is invalid.",
                Detail = badRequest.Message,
            },

            OpenMeteoException or IncompleteWeatherDataException => new ProblemDetails
            {
                Status = StatusCodes.Status502BadGateway,
                Title = "The weather provider returned an error.",
                Detail = exception.Message,
            },

            // The resilience pipeline's timeouts throw TimeoutRejectedException and an open circuit throws
            // BrokenCircuitException; HttpClient's own timeout surfaces as TaskCanceledException with an
            // inner TimeoutException; HttpRequestException covers connection and DNS failures.
            TimeoutRejectedException
                or BrokenCircuitException
                or TaskCanceledException { InnerException: TimeoutException }
                or HttpRequestException => new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "The weather provider is unavailable.",
                Detail = "Open-Meteo did not respond in time or could not be reached. Try again later.",
            },

            // No exception details: they could expose internals.
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
            },
        };

        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            if (problem.Status == StatusCodes.Status500InternalServerError)
            {
                LogUnexpectedError(logger, exception);
            }
            else
            {
                LogUpstreamFailure(logger, problem.Status.Value, exception.Message);
            }
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "Weather provider failure, returning {StatusCode}: {Reason}")]
    private static partial void LogUpstreamFailure(ILogger logger, int statusCode, string reason);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception.")]
    private static partial void LogUnexpectedError(ILogger logger, Exception exception);
}
