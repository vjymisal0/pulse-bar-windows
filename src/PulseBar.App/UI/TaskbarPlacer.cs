using System.Runtime.InteropServices;
using PulseBar.App.Native;
using PulseBar.Core;
using static PulseBar.App.Native.NativeMethods;

namespace PulseBar.App.UI;

/// <summary>Works out where on the taskbar the bar should sit, in physical pixels.</summary>
internal static class TaskbarPlacer
{
    private const int Margin = 8;

    /// <summary>Top-left corner for a bar of the given size, or null when the taskbar is
    /// vertical or can't be found (the caller then falls back to floating).</summary>
    public static (int X, int Y)? Locate(DockMode mode, int width, int height, int offset)
    {
        var data = new APPBARDATA { cbSize = (uint)Marshal.SizeOf<APPBARDATA>() };
        if (SHAppBarMessage(ABM_GETTASKBARPOS, ref data) == 0)
            return null;
        if (data.uEdge is not (ABE_BOTTOM or ABE_TOP))
            return null;

        var taskbar = data.rc;
        var y = taskbar.Top + (taskbar.Height - height) / 2;
        if (mode == DockMode.TaskbarLeft)
            return (taskbar.Left + Margin + offset, y);

        return (NotificationAreaLeft(taskbar) - width - Margin - offset, y);
    }

    /// <summary>Left edge of the tray/clock area. Windows 11's redesigned taskbar may not report it,
    /// so fall back to an estimate scaled to the taskbar's height (which tracks display scaling).</summary>
    private static int NotificationAreaLeft(RECT taskbar)
    {
        var tray = FindWindow("Shell_TrayWnd", null);
        var notify = tray == 0 ? 0 : FindWindowEx(tray, 0, "TrayNotifyWnd", null);
        if (notify != 0 && GetWindowRect(notify, out var rect) &&
            rect.Width > 0 && rect.Left > taskbar.Left && rect.Left < taskbar.Right)
        {
            return rect.Left;
        }
        return taskbar.Right - taskbar.Height * 6;
    }

    /// <summary>True while a fullscreen game, video or presentation is running.</summary>
    public static bool FullscreenAppRunning() =>
        SHQueryUserNotificationState(out var state) == 0 &&
        state is QUNS_BUSY or QUNS_RUNNING_D3D_FULL_SCREEN or QUNS_PRESENTATION_MODE;
}
