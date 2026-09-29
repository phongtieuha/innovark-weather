using Innovark.Weather.Application.Common;
using Shouldly;

namespace Innovark.Weather.UnitTests.Application.Common;

public class TemperatureTests
{
    [Theory]
    [InlineData(0.0, 32.0)]
    [InlineData(100.0, 212.0)]
    [InlineData(-40.0, -40.0)]
    [InlineData(31.25, 88.3)]   // 88.25 rounds half away from zero
    [InlineData(33.1, 91.6)]
    [InlineData(36.6, 97.9)]
    public void ToFahrenheit_KnownValues_RoundToOneDecimal(double celsius, double expected)
    {
        Temperature.ToFahrenheit(celsius).ShouldBe(expected);
    }

    [Fact]
    public void ToFahrenheit_ResultRoundingToZero_IsNotNegativeZero()
    {
        var fahrenheit = Temperature.ToFahrenheit(-17.78);   // -0.004 °F before rounding

        fahrenheit.ShouldBe(0.0);
        double.IsNegative(fahrenheit).ShouldBeFalse();
    }
}
