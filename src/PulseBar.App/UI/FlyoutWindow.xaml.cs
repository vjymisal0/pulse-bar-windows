using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using PulseBar.App.Native;
using PulseBar.Core;
using static PulseBar.App.Native.NativeMethods;
using WinForms = System.Windows.Forms;

namespace PulseBar.App.UI;

/// <summary>The click-open panel: one line per part, each with a live trend graph.</summary>
public partial class FlyoutWindow : Window
{
    private const string Sep = "  ·  ";
    private DateTime _hiddenAt;

    public event Action? SettingsRequested;

    public FlyoutWindow()
    {
        InitializeComponent();
        Deactivated += (_, _) =>
        {
            _hiddenAt = DateTime.UtcNow;
            Hide();
        };
    }

    /// <summary>Shows the flyout next to the bar, or hides it if it's open.</summary>
    internal void Toggle(RECT bar)
    {
        // Clicking the bar deactivates (and so hides) the flyout just before the click lands;
        // don't immediately reopen it.
        if (IsVisible || DateTime.UtcNow - _hiddenAt < TimeSpan.FromMilliseconds(300))
        {
            Hide();
            return;
        }

        Show();
        UpdateLayout();
        var scale = VisualTreeHelper.GetDpi(this).DpiScaleX;
        var w = (int)Math.Ceiling(ActualWidth * scale);
        var h = (int)Math.Ceiling(ActualHeight * scale);

        var work = WinForms.Screen.FromRectangle(new System.Drawing.Rectangle(bar.Left, bar.Top, bar.Width, bar.Height)).WorkingArea;
        var below = bar.Top < work.Top + work.Height / 2;
        var x = Math.Clamp(bar.Right - w, work.Left, Math.Max(work.Left, work.Right - w));
        var y = below ? bar.Bottom : bar.Top - h;
        y = Math.Clamp(y, work.Top, Math.Max(work.Top, work.Bottom - h));

        SetWindowPos(new WindowInteropHelper(this).Handle, HWND_TOPMOST, x, y, 0, 0, SWP_NOSIZE);
        Activate();
    }

    public void Render(Snapshot s, Settings set, IReadOnlyDictionary<string, History> history)
    {
        if (!IsVisible)
            return;
        var unit = set.TempUnit;

        CpuDetail.Text = Join(Format.Percent(s.Cpu.Percent).Trim(),
            s.Cpu.TempC is { } ct ? Format.Temp(ct, unit) : null,
            s.Cpu.Ghz is { } ghz ? Format.Ghz(ghz) : null);
        CpuSpark.SetValues(history["cpu"].Values);

        var hasGpu = s.Gpu is not null;
        GpuName.Visibility = GpuDetail.Visibility = GpuSpark.Visibility = hasGpu ? Visibility.Visible : Visibility.Collapsed;
        if (s.Gpu is { } gpu)
        {
            GpuDetail.Text = Join(Format.Percent(gpu.Percent).Trim(),
                gpu.TempC is { } gt ? Format.Temp(gt, unit) : null,
                gpu.MemoryUsed is { } used
                    ? gpu.MemoryTotal is { } total ? $"{Format.Size(used)} / {Format.Size(total)}" : Format.Size(used)
                    : null);
            GpuSpark.SetValues(history["gpu"].Values);
        }

        if (s.Memory is { } mem)
            RamDetail.Text = Join($"{Format.Size(mem.Used)} / {Format.Size(mem.Total)}", $"{Math.Round(mem.Percent)}%");
        RamSpark.SetValues(history["ram"].Values);

        DiskDetail.Text = $"↓{Format.Rate(s.Disk.ReadPerSec)}  ↑{Format.Rate(s.Disk.WritePerSec)}";
        DiskSpark.SetValues(history["disk"].Values, max: null);

        NetDetail.Text = $"↓{Format.Rate(s.Network.DownPerSec)}  ↑{Format.Rate(s.Network.UpPerSec)}";
        NetSpark.SetValues(history["net"].Values, max: null);

        var hasBattery = s.Battery is not null;
        BatName.Visibility = BatDetail.Visibility = hasBattery ? Visibility.Visible : Visibility.Collapsed;
        if (s.Battery is { } bat)
            BatDetail.Text = Join($"{bat.Percent}%", bat.Charging ? "charging" : "on battery");
    }

    private static string Join(params string?[] parts) => string.Join(Sep, parts.Where(p => !string.IsNullOrEmpty(p)));

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        Hide();
        SettingsRequested?.Invoke();
    }
}
