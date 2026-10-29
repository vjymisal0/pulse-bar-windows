namespace PulseBar.Core;

/// <summary>One reading of every sensor. Fields are null when the hardware or OS doesn't report them.</summary>
public sealed record Snapshot(
    CpuStats Cpu,
    MemoryStats? Memory,
    GpuStats? Gpu,
    DiskStats Disk,
    NetworkStats Network,
    BatteryStats? Battery);

public sealed record CpuStats(double? Percent, double? Ghz, double? TempC);

public sealed record MemoryStats(ulong Used, ulong Total)
{
    public double Percent => Total == 0 ? 0 : 100.0 * Used / Total;
}

public sealed record GpuStats(double? Percent, double? TempC, ulong? MemoryUsed, ulong? MemoryTotal);

public sealed record DiskStats(double? ReadPerSec, double? WritePerSec);

public sealed record NetworkStats(double? DownPerSec, double? UpPerSec);

public sealed record BatteryStats(int Percent, bool Charging);
