using System.Windows;
using Microsoft.Win32;
using PulseBar.App.Sensors;
using PulseBar.App.Services;
using PulseBar.App.UI;
using PulseBar.Core;
using WinForms = System.Windows.Forms;

namespace PulseBar.App;

/// <summary>Wires sensors, windows, tray icon and settings together.</summary>
internal sealed class PulseBarController : IDisposable
{
    private readonly Application _app;
    private readonly SettingsStore _store = new(AppPaths.SettingsFile);
    private readonly Dictionary<string, History> _history = new()
    {
        ["cpu"] = new(), ["gpu"] = new(), ["ram"] = new(), ["disk"] = new(), ["net"] = new(),
    };

    private Settings _settings;
    private SensorHub? _hub;
    private BarWindow? _bar;
    private FlyoutWindow? _flyout;
    private WinForms.NotifyIcon? _tray;
    private Snapshot? _last;

    public PulseBarController(Application app)
    {
        _app = app;
        _settings = _store.Load();
    }

    public void Start()
    {
        ThemeManager.Apply();
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        _bar = new BarWindow();
        _bar.ApplySettings(_settings);
        _bar.Clicked += () => _flyout?.Toggle(_bar.ScreenBounds);
        _bar.Moved += (x, y) =>
        {
            _settings.FloatX = x;
            _settings.FloatY = y;
            Save();
        };
        _bar.Show();

        _flyout = new FlyoutWindow();
        _flyout.SettingsRequested += OpenSettings;
        _flyout.IsVisibleChanged += (_, _) =>
        {
            if (_last is not null)
                _flyout.Render(_last, _settings, _history);
        };

        _tray = CreateTray();

        _hub = new SensorHub(TimeSpan.FromMilliseconds(_settings.RefreshMs));
        // Sensors run on a background thread; hop to the UI thread to draw.
        _hub.Sampled += s => _app.Dispatcher.BeginInvoke(() => OnSample(s));
        _hub.Start();
    }

    private void OnSample(Snapshot s)
    {
        _last = s;
        _history["cpu"].Add(s.Cpu.Percent);
        _history["gpu"].Add(s.Gpu?.Percent);
        _history["ram"].Add(s.Memory?.Percent);
        _history["disk"].Add(s.Disk.ReadPerSec + s.Disk.WritePerSec);
        _history["net"].Add(s.Network.DownPerSec + s.Network.UpPerSec);

        _bar?.Render(s);
        _flyout?.Render(s, _settings, _history);
        if (_tray is not null)
            _tray.Text = TrayText(s);
    }

    private static string TrayText(Snapshot s)
    {
        var text = $"CPU {Format.Percent(s.Cpu.Percent).Trim()}";
        if (s.Gpu is { } gpu)
            text += $" · GPU {Format.Percent(gpu.Percent).Trim()}";
        if (s.Memory is { } mem)
            text += $" · RAM {Format.Size(mem.Used)}";
        // NotifyIcon tooltips are limited to 127 characters.
        return text.Length > 127 ? text[..127] : text;
    }

    private WinForms.NotifyIcon CreateTray()
    {
        var startup = new WinForms.ToolStripMenuItem("Start with Windows") { CheckOnClick = true };
        startup.Checked = SafeStartupState();
        startup.CheckedChanged += (_, _) => SetStartup(startup.Checked);

        var menu = new WinForms.ContextMenuStrip();
        menu.Items.Add("Details", null, (_, _) => _flyout?.Toggle(_bar!.ScreenBounds));
        menu.Items.Add("Settings…", null, (_, _) => OpenSettings());
        menu.Items.Add(startup);
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => _app.Shutdown());

        var tray = new WinForms.NotifyIcon
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!),
            Text = "Pulse Bar",
            ContextMenuStrip = menu,
            Visible = true,
        };
        tray.MouseClick += (_, e) =>
        {
            if (e.Button == WinForms.MouseButtons.Left)
                _flyout?.Toggle(_bar!.ScreenBounds);
        };
        return tray;
    }

    private static bool SafeStartupState()
    {
        try
        {
            return StartupRegistration.IsEnabled;
        }
        catch (Exception e) when (e is System.Security.SecurityException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static void SetStartup(bool enabled)
    {
        try
        {
            StartupRegistration.Set(enabled);
        }
        catch (Exception e) when (e is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            Log.Error("Could not change startup setting", e);
        }
    }

    private void OpenSettings()
    {
        var window = new SettingsWindow(_settings);
        if (window.ShowDialog() != true || window.Result is not { } updated)
            return;

        _settings = updated;
        Save();
        if (_hub is not null)
            _hub.Interval = TimeSpan.FromMilliseconds(_settings.RefreshMs);
        _bar?.ApplySettings(_settings);
        if (_last is not null)
            _bar?.Render(_last);
    }

    private void Save()
    {
        try
        {
            _store.Save(_settings);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Log.Error("Could not save settings", e);
        }
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General)
            _app.Dispatcher.BeginInvoke(ThemeManager.Apply);
    }

    public void Dispose()
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _hub?.Dispose();
        if (_tray is not null)
        {
            _tray.Visible = false;
            _tray.Dispose();
        }
    }
}
