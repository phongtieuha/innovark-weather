using System.ComponentModel.DataAnnotations;

namespace Innovark.Weather.Infrastructure.OpenMeteo;

public sealed class OpenMeteoOptions
{
    public const string SectionName = "OpenMeteo";

    /// <summary>Base address of the Open-Meteo API, with a trailing slash.</summary>
    [Required]
    public Uri? BaseUrl { get; set; }

    [Required]
    [Range(-90.0, 90.0)]
    public double? Latitude { get; set; }

    [Required]
    [Range(-180.0, 180.0)]
    public double? Longitude { get; set; }

    [Range(1, 60)]
    public int TimeoutSeconds { get; set; } = 10;
}
