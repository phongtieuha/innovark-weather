using System.ComponentModel.DataAnnotations;

namespace Innovark.Weather.Web.ApiClients;

public sealed class WeatherApiOptions
{
    public const string SectionName = "WeatherApi";

    /// <summary>Base URL of Innovark.Weather.Api, e.g. http://localhost:5122.</summary>
    [Required]
    [Url]
    public string BaseUrl { get; init; } = "";
}
