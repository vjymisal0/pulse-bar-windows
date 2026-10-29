using System.Text.RegularExpressions;

namespace PulseBar.Core;

/// <summary>
/// Makes sense of Windows' "GPU Engine" and "GPU Adapter Memory" performance counter instances,
/// e.g. <c>pid_1234_luid_0x00000000_0x0000D1B4_phys_0_eng_0_engtype_3D</c>.
/// Works for any GPU vendor and never wakes a sleeping GPU, because the OS already tracks this.
/// </summary>
public static partial class GpuEngines
{
    [GeneratedRegex(@"luid_(0x[0-9A-Fa-f]+_0x[0-9A-Fa-f]+)_phys_(\d+)")]
    private static partial Regex AdapterPattern();

    [GeneratedRegex(@"_engtype_(.+)$")]
    private static partial Regex EngineTypePattern();

    /// <summary>Stable id for the physical adapter an instance belongs to, or null if unrecognised.</summary>
    public static string? AdapterId(string instance)
    {
        var m = AdapterPattern().Match(instance);
        return m.Success ? $"{m.Groups[1].Value}_phys_{m.Groups[2].Value}" : null;
    }

    /// <summary>
    /// Task Manager's rule: add up each engine type (3D, Copy, VideoDecode…) across processes;
    /// an adapter's usage is its busiest engine type.
    /// </summary>
    public static Dictionary<string, double> UtilizationByAdapter(IEnumerable<(string Instance, double Value)> samples)
    {
        var perEngineType = new Dictionary<(string Adapter, string Engine), double>();
        foreach (var (instance, value) in samples)
        {
            if (AdapterId(instance) is not { } adapter)
                continue;
            var engine = EngineTypePattern().Match(instance) is { Success: true } m ? m.Groups[1].Value : "";
            perEngineType[(adapter, engine)] = perEngineType.GetValueOrDefault((adapter, engine)) + value;
        }

        var result = new Dictionary<string, double>();
        foreach (var ((adapter, _), total) in perEngineType)
            result[adapter] = Math.Max(result.GetValueOrDefault(adapter), Math.Min(100, total));
        return result;
    }

    /// <summary>The adapter using the most dedicated memory: the discrete GPU on hybrid laptops.</summary>
    public static string? PickAdapter(IEnumerable<(string Instance, double DedicatedBytes)> memory)
    {
        string? best = null;
        var bestBytes = -1.0;
        foreach (var (instance, bytes) in memory)
        {
            if (AdapterId(instance) is { } adapter && bytes > bestBytes)
            {
                best = adapter;
                bestBytes = bytes;
            }
        }
        return best;
    }
}
