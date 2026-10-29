using PulseBar.Core;

namespace PulseBar.Core.Tests;

public class FormatTests
{
    [Theory]
    [InlineData(512, "512B")]
    [InlineData(6.1 * 1024 * 1024 * 1024, "6.1G")]
    [InlineData(16.0 * 1024 * 1024 * 1024, "16G")]
    [InlineData(2.4 * 1024 * 1024, "2.4M")]
    public void Size(double bytes, string expected) => Assert.Equal(expected, Format.Size(bytes));

    [Fact]
    public void RateMissing() => Assert.Equal(Format.Missing, Format.Rate(null));

    [Fact]
    public void RateAppendsPerSecond() => Assert.Equal("2.0K/s", Format.Rate(2048));

    [Fact]
    public void PercentIsFixedWidth() => Assert.Equal("  8%", Format.Percent(8.4));

    [Theory]
    [InlineData(71.4, TempUnit.Celsius, "71°")]
    [InlineData(100, TempUnit.Fahrenheit, "212°")]
    public void Temp(double c, TempUnit unit, string expected) => Assert.Equal(expected, Format.Temp(c, unit));

    [Fact]
    public void TempMissing() => Assert.Equal(Format.Missing, Format.Temp(null, TempUnit.Celsius));

    [Fact]
    public void FormatIgnoresCurrentCulture()
    {
        var saved = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            Assert.Equal("3.2 GHz", Format.Ghz(3.2));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = saved;
        }
    }
}
