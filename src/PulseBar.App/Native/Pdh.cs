using System.ComponentModel;
using System.Runtime.InteropServices;
using static PulseBar.App.Native.NativeMethods;

namespace PulseBar.App.Native;

/// <summary>
/// A batch of Windows performance counters read together, the same source Task Manager uses.
/// Rate counters need two collections before they report, so the first reading is always null.
/// </summary>
internal sealed class PdhQuery : IDisposable
{
    private nint _handle;

    public PdhQuery()
    {
        var status = PdhOpenQuery(null, 0, out _handle);
        if (status != 0)
            throw new Win32Exception(unchecked((int)status), "PdhOpenQuery failed");
    }

    /// <summary>Adds a counter by its English path; null if this PC doesn't have it.</summary>
    public PdhCounter? TryAdd(string englishPath) =>
        PdhAddEnglishCounter(_handle, englishPath, 0, out var counter) == 0 ? new PdhCounter(counter) : null;

    public bool Collect() => PdhCollectQueryData(_handle) == 0;

    public void Dispose()
    {
        if (_handle != 0)
        {
            _ = PdhCloseQuery(_handle);
            _handle = 0;
        }
    }
}

internal sealed class PdhCounter(nint handle)
{
    private const uint Format = PDH_FMT_DOUBLE | PDH_FMT_NOCAP100;

    public double? Value() =>
        PdhGetFormattedCounterValue(handle, Format, out _, out var v) == 0 && v.CStatus <= PDH_CSTATUS_NEW_DATA
            ? v.DoubleValue
            : null;

    /// <summary>Every instance of a wildcard counter such as <c>\GPU Engine(*)\Utilization Percentage</c>.</summary>
    public List<(string Instance, double Value)> Values()
    {
        // Instances come and go between calls (e.g. a new process starts using the GPU),
        // so the buffer size can change; retry a few times.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            uint size = 0;
            var status = PdhGetFormattedCounterArray(handle, Format, ref size, out _, 0);
            if (status != PDH_MORE_DATA || size == 0)
                return [];

            var buffer = Marshal.AllocHGlobal(checked((int)size));
            try
            {
                status = PdhGetFormattedCounterArray(handle, Format, ref size, out var count, buffer);
                if (status == PDH_MORE_DATA)
                    continue;
                if (status != 0)
                    return [];

                var stride = Marshal.SizeOf<PDH_FMT_COUNTERVALUE_ITEM>();
                var items = new List<(string, double)>((int)count);
                for (var i = 0; i < count; i++)
                {
                    var item = Marshal.PtrToStructure<PDH_FMT_COUNTERVALUE_ITEM>(buffer + i * stride);
                    if (item.FmtValue.CStatus <= PDH_CSTATUS_NEW_DATA)
                        items.Add((Marshal.PtrToStringUni(item.szName) ?? "", item.FmtValue.DoubleValue));
                }
                return items;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        return [];
    }
}
