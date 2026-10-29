<p align="center"><img src="docs/icon.png" width="96" alt="Pulse Bar icon"></p>

# Pulse Bar for Windows

Live CPU, GPU, RAM and temperatures on your Windows taskbar. Click it for a compact panel with live trend graphs, plus disk, network and battery.

This is the Windows edition of [Pulse Bar](https://github.com/vjymisal0/pulse-bar), a GNOME Shell extension.

> **Status: v0.1, early preview.** Built and unit-tested in CI, but not yet tried on many real machines. Taskbar placement in particular may need tuning on some Windows 11 setups (see [Settings](#settings)). Bug reports are very welcome.

## Features

- **Always on.** Sits on the taskbar next to the clock and refreshes every 1.5 s (configurable).
- **Click for details.** A flyout shows every stat with a 60-second trend graph.
- **Colour-coded.** Values turn yellow, then red, as load or temperature rises. Follows Windows light/dark mode.
- **Stays out of the way.** Hides automatically during fullscreen games, videos and presentations.
- **Safe by design.** No admin rights, no kernel driver, no network access, no telemetry. See [SECURITY.md](SECURITY.md).

## What it shows

| Stat | Source | Notes |
|------|--------|-------|
| CPU usage | `% Processor Utility` counter | Matches Task Manager |
| CPU speed | `Processor Frequency` × `% Processor Performance` | Real clock, including turbo |
| CPU temperature | ACPI thermal zones | Approximate; not every PC exposes one (see below) |
| GPU usage | `GPU Engine` counters | Any vendor; same method as Task Manager |
| GPU memory | `GPU Adapter Memory` counters | Any vendor |
| GPU temperature, total VRAM | NVIDIA NVML | NVIDIA only |
| RAM | `GlobalMemoryStatusEx` | |
| Disk read/write | `PhysicalDisk` counters | |
| Network | Network adapter byte counters | Physical adapters only |
| Battery | Windows power status | Laptops |

### Why some temperatures are missing

Accurate CPU core temperatures, fan speeds and AMD/Intel GPU temperatures on Windows can only be read through a **kernel driver**. The commonly used one (WinRing0) is on Microsoft's vulnerable driver blocklist and is flagged by Defender. Pulse Bar chooses not to install any driver, so it shows only what Windows exposes to normal apps. If your PC reports no thermal zone, the CPU temperature is simply hidden.

## Install

1. Download `PulseBar-<version>-win-x64.zip` (or `win-arm64` for ARM PCs) from [Releases](https://github.com/vjymisal0/pulse-bar-windows/releases).
2. Extract it anywhere, for example `%LOCALAPPDATA%\Programs\PulseBar\`.
3. Run `PulseBar.exe`. Nothing else needs to be installed.

The exe isn't code-signed yet, so SmartScreen may show "Windows protected your PC". Choose **More info → Run anyway**. You can [verify the download](SECURITY.md#verifying-a-download) first.

To start it automatically, right-click the tray icon and tick **Start with Windows**.

To uninstall: untick **Start with Windows**, choose **Exit**, then delete the folder and `%APPDATA%\PulseBar`.

## Settings

Right-click the tray icon and choose **Settings…**, or click the bar and then **Settings**.

- Which stats show on the bar
- **Position:** `TaskbarRight` (next to the clock), `TaskbarLeft`, or `Floating` (drag it anywhere)
- **Gap from the notification area:** increase it if the bar covers tray icons
- Refresh interval, °C/°F, and the yellow/red temperature thresholds

Windows 11 doesn't let apps put widgets inside the taskbar, so Pulse Bar is a small always-on-top window placed over it. Popular tools like TrafficMonitor work the same way. If it doesn't sit right on your setup, use `Floating`.

## Build from source

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```powershell
dotnet test                                   # unit tests (also run on Linux/macOS)
dotnet run --project src/PulseBar.App         # run it (Windows)
dotnet publish src/PulseBar.App -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

```
src/PulseBar.Core/        platform-neutral logic: formatting, maths, settings (unit-tested)
src/PulseBar.App/
  Native/                 Win32 interop: PDH counters, NVML, taskbar/window APIs
  Sensors/SensorHub.cs    samples everything on a background thread
  Services/               settings paths, theme, startup entry, error log
  UI/                     bar, flyout, settings window, sparkline
tests/PulseBar.Core.Tests/
```

## License

[MIT](LICENSE)
