using System.Diagnostics;
using System.Net.NetworkInformation;
using PulseBar.App.Native;
using PulseBar.App.Services;
using PulseBar.Core;
using WinForms = System.Windows.Forms;

namespace PulseBar.App.Sensors;

/// <summary>
/// Samples every sensor on a background thread at the configured interval and raises
/// <see cref="Sampled"/>. All sources are user-mode Windows APIs: no admin, no kernel driver.
/// </summary>
internal sealed class SensorHub : IDisposable
{
    private readonly PdhQuery _pdh = new();
    private readonly PdhCounter? _cpuUtility;
    private readonly PdhCounter? _cpuPerformance;
    private readonly PdhCounter? _cpuFrequency;
    private readonly PdhCounter? _thermalZones;
    private readonly PdhCounter? _gpuEngines;
    private readonly PdhCounter? _gpuMemory;
    private readonly PdhCounter? _diskRead;
    private readonly PdhCounter? _diskWrite;
    private readonly Nvml? _nvml;

    private readonly CancellationTokenSource _stop = new();
    private readonly PeriodicTimer _timer;
    private Task? _loop;

    private CpuTimes? _prevCpu;
    private (ulong Rx, ulong Tx)? _prevNet;
    private long _prevTimestamp;

    public event Action<Snapshot>? Sampled;

    public SensorHub(TimeSpan interval)
    {
        _timer = new PeriodicTimer(interval);

        // "% Processor Utility" is what Task Manager shows; it accounts for turbo boost.
        _cpuUtility = _pdh.TryAdd(@"\Processor Information(_Total)\% Processor Utility");
        _cpuPerformance = _pdh.TryAdd(@"\Processor Information(_Total)\% Processor Performance");
        _cpuFrequency = _pdh.TryAdd(@"\Processor Information(_Total)\Processor Frequency");
        // ACPI thermal zones: the only CPU-area temperature readable without a kernel driver.
        _thermalZones = _pdh.TryAdd(@"\Thermal Zone Information(*)\Temperature");
        _gpuEngines = _pdh.TryAdd(@"\GPU Engine(*)\Utilization Percentage");
        _gpuMemory = _pdh.TryAdd(@"\GPU Adapter Memory(*)\Dedicated Usage");
        _diskRead = _pdh.TryAdd(@"\PhysicalDisk(_Total)\Disk Read Bytes/sec");
        _diskWrite = _pdh.TryAdd(@"\PhysicalDisk(_Total)\Disk Write Bytes/sec");
        _nvml = Nvml.TryOpen();
    }

    public TimeSpan Interval
    {
        get => _timer.Period;
        set => _timer.Period = value;
    }

    public void Start() => _loop ??= Task.Run(RunAsync);

    private async Task RunAsync()
    {
        try
        {
            do
            {
                try
                {
                    Sampled?.Invoke(Sample());
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    // One bad reading must never kill the loop.
                    Log.Error("Sampling failed", e);
                }
            }
            while (await _timer.WaitForNextTickAsync(_stop.Token).ConfigureAwait(false));
        }
        catch (OperationCanceledException)
        {
        }
    }

    private Snapshot Sample()
    {
        _pdh.Collect();
        var now = Stopwatch.GetTimestamp();
        var seconds = _prevTimestamp == 0 ? 0 : Stopwatch.GetElapsedTime(_prevTimestamp, now).TotalSeconds;
        _prevTimestamp = now;

        return new Snapshot(
            ReadCpu(),
            ReadMemory(),
            ReadGpu(),
            new DiskStats(_diskRead?.Value(), _diskWrite?.Value()),
            ReadNetwork(seconds),
            ReadBattery());
    }

    private CpuStats ReadCpu()
    {
        double? percent = _cpuUtility?.Value() is { } utility ? Math.Min(100, utility) : null;
        if (percent is null && NativeMethods.GetSystemTimes(out var idle, out var kernel, out var user))
        {
            var cur = new CpuTimes(idle, kernel, user);
            percent = Rates.CpuPercent(_prevCpu, cur);
            _prevCpu = cur;
        }

        double? ghz = _cpuFrequency?.Value() is { } mhz && _cpuPerformance?.Value() is { } perf
            ? mhz * perf / 100 / 1000
            : null;

        double? tempC = null;
        if (_thermalZones?.Values() is { Count: > 0 } zones)
        {
            var kelvin = zones.Max(z => z.Value);
            if (kelvin - 273.15 is > 0 and < 125 and var c)
                tempC = c;
        }

        return new CpuStats(percent, ghz, tempC);
    }

    private static MemoryStats? ReadMemory()
    {
        var status = new MEMORYSTATUSEX { dwLength = (uint)System.Runtime.InteropServices.Marshal.SizeOf<MEMORYSTATUSEX>() };
        return NativeMethods.GlobalMemoryStatusEx(ref status)
            ? new MemoryStats(status.ullTotalPhys - status.ullAvailPhys, status.ullTotalPhys)
            : null;
    }

    private GpuStats? ReadGpu()
    {
        var memory = _gpuMemory?.Values() ?? [];
        var adapter = GpuEngines.PickAdapter(memory);
        if (adapter is null && _nvml is null)
            return null;

        double? percent = null;
        if (adapter is not null && _gpuEngines is not null &&
            GpuEngines.UtilizationByAdapter(_gpuEngines.Values()).TryGetValue(adapter, out var p))
        {
            percent = p;
        }

        ulong? used = null;
        foreach (var (instance, bytes) in memory)
        {
            if (GpuEngines.AdapterId(instance) == adapter)
                used = (ulong)bytes;
        }

        return new GpuStats(percent, _nvml?.TemperatureC(), used, _nvml?.MemoryTotal());
    }

    private NetworkStats ReadNetwork(double seconds)
    {
        ulong rx = 0, tx = 0;
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up ||
                nic.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel ||
                nic.Name.StartsWith("vEthernet", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            var stats = nic.GetIPStatistics();
            rx += (ulong)stats.BytesReceived;
            tx += (ulong)stats.BytesSent;
        }

        var prev = _prevNet;
        _prevNet = (rx, tx);
        return new NetworkStats(Rates.PerSecond(prev?.Rx, rx, seconds), Rates.PerSecond(prev?.Tx, tx, seconds));
    }

    private static BatteryStats? ReadBattery()
    {
        var power = WinForms.SystemInformation.PowerStatus;
        if (power.BatteryChargeStatus.HasFlag(WinForms.BatteryChargeStatus.NoSystemBattery) ||
            power.BatteryChargeStatus == WinForms.BatteryChargeStatus.Unknown ||
            power.BatteryLifePercent is < 0 or > 1)
        {
            return null;
        }
        return new BatteryStats((int)Math.Round(power.BatteryLifePercent * 100),
            power.PowerLineStatus == WinForms.PowerLineStatus.Online);
    }

    public void Dispose()
    {
        _stop.Cancel();
        try
        {
            _loop?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
        }
        _timer.Dispose();
        _pdh.Dispose();
        _nvml?.Dispose();
        _stop.Dispose();
    }
}
