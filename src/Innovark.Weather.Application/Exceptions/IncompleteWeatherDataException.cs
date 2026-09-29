namespace Innovark.Weather.Application.Exceptions;

/// <summary>
/// The provider did not return every expected hour with a temperature and humidity, so the API
/// reports an upstream failure rather than returning partial data.
/// </summary>
public sealed class IncompleteWeatherDataException : Exception
{
    public IncompleteWeatherDataException()
    {
    }

    public IncompleteWeatherDataException(string message)
        : base(message)
    {
    }

    public IncompleteWeatherDataException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
