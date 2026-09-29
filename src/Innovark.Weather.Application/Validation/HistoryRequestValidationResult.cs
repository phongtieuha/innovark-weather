using System.Diagnostics.CodeAnalysis;

namespace Innovark.Weather.Application.Validation;

/// <summary>
/// Outcome of validating a history request. Errors are keyed by request field, in the shape
/// expected by a validation ProblemDetails response.
/// </summary>
public sealed class HistoryRequestValidationResult
{
    private HistoryRequestValidationResult(DateTimeOffset? requestedTime, IReadOnlyDictionary<string, string[]> errors)
    {
        RequestedTime = requestedTime;
        Errors = errors;
    }

    /// <summary>The requested hour at +07:00; set only when the request is valid.</summary>
    public DateTimeOffset? RequestedTime { get; }

    public IReadOnlyDictionary<string, string[]> Errors { get; }

    [MemberNotNullWhen(true, nameof(RequestedTime))]
    public bool IsValid => RequestedTime is not null;

    internal static HistoryRequestValidationResult Valid(DateTimeOffset requestedTime) =>
        new(requestedTime, new Dictionary<string, string[]>());

    internal static HistoryRequestValidationResult Invalid(string field, string message) =>
        new(null, new Dictionary<string, string[]> { [field] = [message] });
}
