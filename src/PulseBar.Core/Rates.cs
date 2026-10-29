namespace PulseBar.Core;

public static class Rates
{
    /// <summary>Per-second rate between two readings of an ever-increasing counter.
    /// Null without a previous reading, or when the counter went backwards (reset/wrap).</summary>
    public static double? PerSecond(ulong? previous, ulong current, double seconds)
    {
        if (previous is not { } prev || seconds <= 0 || current < prev)
            return null;
        return (current - prev) / seconds;
    }

    /// <summary>CPU busy % from GetSystemTimes-style totals. Kernel time includes idle time.</summary>
    public static double? CpuPercent(CpuTimes? previous, CpuTimes current)
    {
        if (previous is not { } prev)
            return null;
        var total = (current.Kernel - prev.Kernel) + (current.User - prev.User);
        if (total <= 0)
            return null;
        var idle = current.Idle - prev.Idle;
        return Math.Clamp(100.0 * (total - idle) / total, 0, 100);
    }
}

public readonly record struct CpuTimes(long Idle, long Kernel, long User);
