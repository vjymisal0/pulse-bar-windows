using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using PulseBar.App.Native;
using PulseBar.Core;
using static PulseBar.App.Native.NativeMethods;

namespace PulseBar.App.UI;

/// <summary>The always-visible strip of pills, docked on the taskbar or floating.</summary>
public partial class BarWindow : Window
{
    private nint _hwnd;
    private Settings _settings = new();

    /// <summary>Raised on a click (not a drag).</summary>
    public event Action? Clicked;

    /// <summary>Raised when the user finishes dragging the floating bar.</summary>
    public event Action<double, double>? Moved;

    public BarWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            _hwnd = new WindowInteropHelper(this).Handle;
            // Tool window: no Alt+Tab entry. No-activate: clicking the bar doesn't steal focus.
            var style = (long)GetWindowLongPtr(_hwnd, GWL_EXSTYLE);
            SetWindowLongPtr(_hwnd, GWL_EXSTYLE, (nint)(style | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE));
        };
        SizeChanged += (_, _) => Reposition();
        MouseLeftButtonDown += OnMouseDown;
    }

    /// <summary>Physical-pixel bounds, for placing the flyout next to the bar.</summary>
    internal RECT ScreenBounds => GetWindowRect(_hwnd, out var r) ? r : default;

    private double Scale => VisualTreeHelper.GetDpi(this).DpiScaleX;

    private bool IsFloating => _settings.Dock == DockMode.Floating;

    public void ApplySettings(Settings settings)
    {
        _settings = settings;
        Reposition();
    }

    public void Render(Snapshot s)
    {
        var set = _settings;
        var unit = set.TempUnit;
        Level TempLevel(double? c) => Levels.For(c, set.TempWarn, set.TempCrit);

        CpuPill.Visibility = Show(set.ShowCpu);
        CpuPill.SetMain(Format.Percent(s.Cpu.Percent), Levels.For(s.Cpu.Percent, Levels.UsageWarn, Levels.UsageCrit));
        CpuPill.SetExtra(set.ShowTemps && s.Cpu.TempC is { } ct ? Format.Temp(ct, unit).PadLeft(4) : null, TempLevel(s.Cpu.TempC));

        GpuPill.Visibility = Show(set.ShowGpu && s.Gpu is not null);
        if (s.Gpu is { } gpu)
        {
            GpuPill.SetMain(Format.Percent(gpu.Percent), Levels.For(gpu.Percent, Levels.UsageWarn, Levels.UsageCrit));
            GpuPill.SetExtra(set.ShowTemps && gpu.TempC is { } gt ? Format.Temp(gt, unit).PadLeft(4) : null, TempLevel(gpu.TempC));
        }

        RamPill.Visibility = Show(set.ShowRam && s.Memory is not null);
        if (s.Memory is { } mem)
        {
            RamPill.SetMain(Format.Size(mem.Used).PadLeft(4), Levels.For(mem.Percent, Levels.MemoryWarn, Levels.MemoryCrit));
            RamPill.SetExtra(null, Level.Ok);
        }

        NetPill.Visibility = Show(set.ShowNet);
        NetPill.SetMain("↓" + Format.Size(s.Network.DownPerSec ?? 0).PadLeft(4), Level.Ok);
        NetPill.SetExtra("↑" + Format.Size(s.Network.UpPerSec ?? 0).PadLeft(4), Level.Ok);

        // Get out of the way of fullscreen games and videos.
        Visibility = TaskbarPlacer.FullscreenAppRunning() ? Visibility.Hidden : Visibility.Visible;
        Reposition();
    }

    /// <summary>Docks the bar, and re-asserts topmost: clicking the taskbar raises it above us.</summary>
    public void Reposition()
    {
        if (_hwnd == 0 || ActualWidth <= 0)
            return;

        var scale = Scale;
        var width = (int)Math.Ceiling(ActualWidth * scale);
        var height = (int)Math.Ceiling(ActualHeight * scale);

        (int X, int Y)? spot = IsFloating ? null : TaskbarPlacer.Locate(_settings.Dock, width, height, _settings.TaskbarOffset);
        if (spot is { } p)
        {
            SetWindowPos(_hwnd, HWND_TOPMOST, p.X, p.Y, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE);
            return;
        }

        // Floating, or a vertical/missing taskbar: use the saved spot, else above the bottom-right corner.
        var area = SystemParameters.WorkArea;
        Left = _settings.FloatX ?? area.Right - ActualWidth - 16;
        Top = _settings.FloatY ?? area.Bottom - ActualHeight - 16;
        SetWindowPos(_hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!IsFloating)
        {
            Clicked?.Invoke();
            return;
        }

        var (left, top) = (Left, Top);
        DragMove();
        if (Math.Abs(Left - left) < 2 && Math.Abs(Top - top) < 2)
            Clicked?.Invoke();
        else
            Moved?.Invoke(Left, Top);
    }

    private static Visibility Show(bool visible) => visible ? Visibility.Visible : Visibility.Collapsed;
}
