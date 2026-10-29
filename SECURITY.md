# Security policy

## Reporting a vulnerability

Please report security issues **privately** through GitHub:
**Security → Report a vulnerability** on this repository.
Don't open a public issue for security problems.

You can expect an acknowledgement within 7 days. Fixes ship as a new release, and the advisory is published once users have had a chance to update.

## Supported versions

Only the latest release receives security fixes.

## Security design

Pulse Bar reads hardware stats, so it is designed to need as little trust as possible.

| Principle | How |
|-----------|-----|
| **No administrator rights** | The manifest requests `asInvoker`. Pulse Bar never prompts for elevation. |
| **No kernel drivers** | Only user-mode Windows APIs are used: performance counters (PDH), `GlobalMemoryStatusEx`, `GetSystemTimes`, and NVIDIA's NVML. Many hardware monitors bundle the WinRing0 driver, which is on Microsoft's vulnerable driver blocklist. Pulse Bar deliberately doesn't. |
| **No network access** | Pulse Bar makes no network connections. There is no telemetry, update check or analytics. It only *reads* network byte counters. |
| **No DLL hijacking** | All P/Invoke DLLs resolve from `System32` only (`DefaultDllImportSearchPaths`). `nvml.dll` is loaded by absolute path, never by name. |
| **Writes only to your profile** | Settings: `%APPDATA%\PulseBar\settings.json`. Error log: `%LOCALAPPDATA%\PulseBar\pulsebar.log` (size-capped, exception details only). Startup entry: `HKCU\...\Run`, which is per-user, opt-in and quoted. |
| **Untrusted input is validated** | The settings file is size-capped (64 KB), and corrupt or out-of-range values fall back to safe defaults. |
| **Supply chain** | The code has no third-party runtime dependencies. GitHub Actions are pinned to commit SHAs, workflows run with least-privilege tokens, NuGet restores use lock files, and Dependabot and CodeQL run continuously. |
| **Verifiable releases** | Every release zip has a SHA-256 checksum and a signed build-provenance attestation. |

## Verifying a download

```powershell
# Checksum
Get-FileHash .\PulseBar-<version>-win-x64.zip -Algorithm SHA256
# compare with SHA256SUMS.txt from the same release

# Provenance: proves the zip was built by this repo's release workflow
gh attestation verify .\PulseBar-<version>-win-x64.zip --repo vjymisal0/pulse-bar-windows
```

The executable is not yet Authenticode code-signed, so Windows SmartScreen may warn on first launch. The attestation above is how you can confirm the file is genuine.
