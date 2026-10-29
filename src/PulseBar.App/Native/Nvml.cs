using System.Runtime.InteropServices;

namespace PulseBar.App.Native;

/// <summary>
/// NVIDIA Management Library, shipped with the NVIDIA driver. Used only for what Windows'
/// own counters lack: GPU temperature and total VRAM. Needs no admin rights.
/// </summary>
internal sealed unsafe class Nvml : IDisposable
{
    private const int Success = 0;
    private const int TemperatureGpu = 0;

    [StructLayout(LayoutKind.Sequential)]
    private struct Memory
    {
        public ulong Total;
        public ulong Free;
        public ulong Used;
    }

    private nint _library;
    private readonly nint _device;
    private readonly delegate* unmanaged<nint, int, uint*, int> _getTemperature;
    private readonly delegate* unmanaged<nint, Memory*, int> _getMemoryInfo;
    private readonly delegate* unmanaged<int> _shutdown;

    private Nvml(nint library, nint device)
    {
        _library = library;
        _device = device;
        _getTemperature = (delegate* unmanaged<nint, int, uint*, int>)NativeLibrary.GetExport(library, "nvmlDeviceGetTemperature");
        _getMemoryInfo = (delegate* unmanaged<nint, Memory*, int>)NativeLibrary.GetExport(library, "nvmlDeviceGetMemoryInfo");
        _shutdown = (delegate* unmanaged<int>)NativeLibrary.GetExport(library, "nvmlShutdown");
    }

    // Absolute paths only: loading "nvml.dll" by name would search the app folder and PATH first.
    private static IEnumerable<string> Candidates =>
    [
        Path.Combine(Environment.SystemDirectory, "nvml.dll"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "NVIDIA Corporation", "NVSMI", "nvml.dll"),
    ];

    /// <summary>Opens the first NVIDIA GPU, or returns null when there is no NVIDIA driver.</summary>
    public static Nvml? TryOpen()
    {
        foreach (var path in Candidates)
        {
            if (!File.Exists(path))
                continue;
            if (!NativeLibrary.TryLoad(path, out var library))
                continue;
            try
            {
                var init = (delegate* unmanaged<int>)NativeLibrary.GetExport(library, "nvmlInit_v2");
                var getHandle = (delegate* unmanaged<uint, nint*, int>)NativeLibrary.GetExport(library, "nvmlDeviceGetHandleByIndex_v2");
                if (init() != Success)
                {
                    NativeLibrary.Free(library);
                    continue;
                }
                nint device;
                if (getHandle(0, &device) == Success)
                    return new Nvml(library, device);
                ((delegate* unmanaged<int>)NativeLibrary.GetExport(library, "nvmlShutdown"))();
            }
            catch (EntryPointNotFoundException)
            {
            }
            NativeLibrary.Free(library);
        }
        return null;
    }

    public double? TemperatureC()
    {
        uint temp;
        return _library != 0 && _getTemperature(_device, TemperatureGpu, &temp) == Success ? temp : null;
    }

    public ulong? MemoryTotal()
    {
        Memory memory;
        return _library != 0 && _getMemoryInfo(_device, &memory) == Success ? memory.Total : null;
    }

    public void Dispose()
    {
        if (_library == 0)
            return;
        _shutdown();
        NativeLibrary.Free(_library);
        _library = 0;
    }
}
