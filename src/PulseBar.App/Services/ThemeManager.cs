using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using PulseBar.Core;

namespace PulseBar.App.Services;

/// <summary>
/// Follows Windows' light/dark settings. The taskbar and apps can be themed separately,
/// so the bar follows "Windows mode" and the flyout/settings follow "App mode".
/// </summary>
internal static class ThemeManager
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    public static void Apply()
    {
        var res = Application.Current.Resources;
        var lightBar = ReadLight("SystemUsesLightTheme", fallback: false);
        var lightApps = ReadLight("AppsUseLightTheme", fallback: true);

        // Bar (taskbar)
        res["BarText"] = Brush(lightBar ? "#1B1F2A" : "#F2F4F8");
        res["BarDim"] = Brush(lightBar ? "#5A6275" : "#A7AFC2");
        res["PillBackground"] = Brush(lightBar ? "#1A808080" : "#26FFFFFF");
        res[Key(Level.Ok)] = Brush(lightBar ? "#0D9450" : "#5EF2A0");
        res[Key(Level.Warn)] = Brush(lightBar ? "#B87800" : "#FFD166");
        res[Key(Level.Hot)] = Brush(lightBar ? "#D63A1E" : "#FF7A59");
        res[Key(Level.Idle)] = Brush(lightBar ? "#6B7385" : "#8A93A8");

        // Flyout and settings
        res["FlyoutBackground"] = Brush(lightApps ? "#F7F8FB" : "#1C2130");
        res["FlyoutBorder"] = Brush(lightApps ? "#22000000" : "#33FFFFFF");
        res["FlyoutText"] = Brush(lightApps ? "#1B1F2A" : "#E6ECFF");
        res["FlyoutDim"] = Brush(lightApps ? "#5A6275" : "#9AA3B8");
        res["Series.Cpu"] = Brush(lightApps ? "#BF8000" : "#FFD166");
        res["Series.Gpu"] = Brush(lightApps ? "#0D9450" : "#5EF2A0");
        res["Series.Ram"] = Brush(lightApps ? "#2966DB" : "#7AA2FF");
        res["Series.Disk"] = Brush(lightApps ? "#DB4D24" : "#FF7A59");
        res["Series.Net"] = Brush(lightApps ? "#854DDB" : "#BF99FF");
    }

    public static string Key(Level level) => "Bar" + level;

    private static bool ReadLight(string name, bool fallback)
    {
        using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
        return key?.GetValue(name) is int value ? value != 0 : fallback;
    }

    private static SolidColorBrush Brush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
