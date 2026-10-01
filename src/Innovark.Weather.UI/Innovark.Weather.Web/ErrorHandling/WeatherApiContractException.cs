namespace Innovark.Weather.Web.ErrorHandling;

/// <summary>
/// The weather API answered 200 without a value its contract requires. It always sends them, so this
/// means the API and this app disagree on the contract; returned as 502.
/// </summary>
public sealed class WeatherApiContractException(string message) : Exception(message);
