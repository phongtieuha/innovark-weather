namespace Innovark.Weather.Application.Common;

public static class Temperature
{
    /// <summary>
    /// Converts °C to °F, rounded to 1 decimal place, half away from zero (31.25 °C → 88.3 °F).
    /// Calculated rather than fetched, so °C and °F always describe the same reading.
    /// </summary>
    public static double ToFahrenheit(double celsius)
    {
        var fahrenheit = Math.Round(celsius * 9 / 5 + 32, 1, MidpointRounding.AwayFromZero);

        // Rounding a small negative value (e.g. -17.78 °C → -0.004 °F) gives -0.0, which JSON
        // would write as "-0". Adding 0.0 turns negative zero into zero.
        return fahrenheit + 0.0;
    }
}
