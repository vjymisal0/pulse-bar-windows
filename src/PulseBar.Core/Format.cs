using System.Globalization;

namespace PulseBar.Core;

public enum TempUnit
{
    Celsius,
    Fahrenheit,
}

/// <summary>Short, fixed-width strings for the bar. Invariant culture so output doesn't vary by locale.</summary>
public static class Format
{
    public const string Missing = "–";

    private static readonly string[] Units = ["B", "K", "M", "G", "T"];

    public static string Size(double bytes)
    {
        var i = 0;
        while (bytes >= 1024 && i < Units.Length - 1)
        {
            bytes /= 1024;
            i++;
        }

        var number = bytes < 10 && i > 0
            ? bytes.ToString("0.0", CultureInfo.InvariantCulture)
            : Math.Round(bytes).ToString(CultureInfo.InvariantCulture);
        return number + Units[i];
    }

    public static string Rate(double? bytesPerSec) =>
        bytesPerSec is { } v ? Size(v) + "/s" : Missing;

    public static string Percent(double? value) =>
        value is { } v ? (Math.Round(v).ToString(CultureInfo.InvariantCulture) + "%").PadLeft(4) : Missing;

    public static string Temp(double? celsius, TempUnit unit)
    {
        if (celsius is not { } c)
            return Missing;
        var v = unit == TempUnit.Fahrenheit ? c * 9 / 5 + 32 : c;
        return Math.Round(v).ToString(CultureInfo.InvariantCulture) + "°";
    }

    public static string Ghz(double? ghz) =>
        ghz is { } v ? v.ToString("0.0", CultureInfo.InvariantCulture) + " GHz" : Missing;
}
