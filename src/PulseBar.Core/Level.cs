namespace PulseBar.Core;

/// <summary>Colour band for a value.</summary>
public enum Level
{
    Ok,
    Warn,
    Hot,
    Idle,
}

public static class Levels
{
    // Usage percentages turn yellow/red at these; temperatures use the user's settings.
    public const double UsageWarn = 60;
    public const double UsageCrit = 85;
    public const double MemoryWarn = 75;
    public const double MemoryCrit = 90;

    public static Level For(double? value, double warn, double crit) => value switch
    {
        null => Level.Ok,
        var v when v < warn => Level.Ok,
        var v when v < crit => Level.Warn,
        _ => Level.Hot,
    };
}
