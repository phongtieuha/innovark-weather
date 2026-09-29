using System.Net;

namespace Innovark.Weather.Infrastructure.OpenMeteo;

/// <summary>Open-Meteo returned an error or a response that could not be understood.</summary>
public sealed class OpenMeteoException : Exception
{
    public OpenMeteoException()
    {
    }

    public OpenMeteoException(string message)
        : base(message)
    {
    }

    public OpenMeteoException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public OpenMeteoException(string message, HttpStatusCode statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    /// <summary>The upstream HTTP status code, when the failure was a non-success response.</summary>
    public HttpStatusCode? StatusCode { get; }
}
