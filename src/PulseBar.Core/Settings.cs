using System.Text.Json;
using System.Text.Json.Serialization;

namespace PulseBar.Core;

public enum DockMode
{
    /// <summary>On the taskbar, just left of the notification area.</summary>
    TaskbarRight,

    /// <summary>On the taskbar, at its left edge.</summary>
    TaskbarLeft,

    /// <summary>A small draggable window anywhere on screen.</summary>
    Floating,
}

public sealed class Settings
{
    public const int MinRefreshMs = 500;
    public const int MaxRefreshMs = 10_000;

    public int RefreshMs { get; set; } = 1500;

    public bool ShowCpu { get; set; } = true;
    public bool ShowGpu { get; set; } = true;
    public bool ShowRam { get; set; } = true;
    public bool ShowTemps { get; set; } = true;
    public bool ShowNet { get; set; }

    public TempUnit TempUnit { get; set; } = TempUnit.Celsius;

    /// <summary>°C at which temperatures turn yellow, whatever unit is displayed.</summary>
    public int TempWarn { get; set; } = 70;

    /// <summary>°C at which temperatures turn red.</summary>
    public int TempCrit { get; set; } = 85;

    public DockMode Dock { get; set; } = DockMode.TaskbarRight;

    /// <summary>Extra gap in pixels between the bar and the notification area, to clear other tray widgets.</summary>
    public int TaskbarOffset { get; set; }

    public double? FloatX { get; set; }
    public double? FloatY { get; set; }

    public Settings Clone() => (Settings)MemberwiseClone();

    /// <summary>Clamps every value into range. The settings file is user-editable, so never trust it.</summary>
    public Settings Normalized()
    {
        var s = Clone();
        s.RefreshMs = Math.Clamp(s.RefreshMs, MinRefreshMs, MaxRefreshMs);
        s.TempWarn = Math.Clamp(s.TempWarn, 30, 110);
        s.TempCrit = Math.Clamp(s.TempCrit, s.TempWarn, 110);
        s.TaskbarOffset = Math.Clamp(s.TaskbarOffset, 0, 2000);
        if (!Enum.IsDefined(s.TempUnit))
            s.TempUnit = TempUnit.Celsius;
        if (!Enum.IsDefined(s.Dock))
            s.Dock = DockMode.TaskbarRight;
        if (s.FloatX is { } x && !double.IsFinite(x))
            s.FloatX = null;
        if (s.FloatY is { } y && !double.IsFinite(y))
            s.FloatY = null;
        return s;
    }
}

[JsonSerializable(typeof(Settings))]
[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
internal sealed partial class SettingsJsonContext : JsonSerializerContext;

/// <summary>Loads and saves settings as JSON. A missing, oversized or corrupt file falls back to defaults.</summary>
public sealed class SettingsStore(string path)
{
    private const long MaxFileBytes = 64 * 1024;

    public string Path { get; } = path;

    public Settings Load()
    {
        try
        {
            var info = new FileInfo(Path);
            if (!info.Exists || info.Length > MaxFileBytes)
                return new Settings();
            using var stream = info.OpenRead();
            var loaded = JsonSerializer.Deserialize(stream, SettingsJsonContext.Default.Settings);
            return (loaded ?? new Settings()).Normalized();
        }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
        {
            return new Settings();
        }
    }

    /// <summary>Writes to a temp file first, then swaps it in, so a crash mid-write can't corrupt settings.</summary>
    public void Save(Settings settings)
    {
        var dir = System.IO.Path.GetDirectoryName(Path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        var tmp = Path + ".tmp";
        using (var stream = File.Create(tmp))
            JsonSerializer.Serialize(stream, settings.Normalized(), SettingsJsonContext.Default.Settings);
        File.Move(tmp, Path, overwrite: true);
    }
}
