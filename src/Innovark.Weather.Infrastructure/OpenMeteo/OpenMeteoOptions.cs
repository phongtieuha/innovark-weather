using System.ComponentModel.DataAnnotations;

namespace Innovark.Weather.Infrastructure.OpenMeteo;

public sealed class OpenMeteoOptions
{
    public const string SectionName = "OpenMeteo";

    /// <summary>
    /// Subsection with the standard resilience handler's settings (timeouts, retry, circuit breaker),
    /// bound to <c>HttpStandardResilienceOptions</c>.
    /// </summary>
    public const string ResilienceSectionName = "Resilience";

    /// <summary>Base address of the Open-Meteo API, with a trailing slash.</summary>
    [Required]
    public Uri? BaseUrl { get; set; }

    [Required]
    [Range(-90.0, 90.0)]
    public double? Latitude { get; set; }

    [Required]
    [Range(-180.0, 180.0)]
    public double? Longitude { get; set; }
}
