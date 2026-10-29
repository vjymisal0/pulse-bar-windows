namespace PulseBar.App.Services;

/// <summary>Per-user locations. Nothing is written outside the user's own profile.</summary>
internal static class AppPaths
{
    /// <summary>%APPDATA%\PulseBar\settings.json (roams with the user profile).</summary>
    public static string SettingsFile { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PulseBar", "settings.json");

    /// <summary>%LOCALAPPDATA%\PulseBar\pulsebar.log</summary>
    public static string LogFile { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PulseBar", "pulsebar.log");
}
