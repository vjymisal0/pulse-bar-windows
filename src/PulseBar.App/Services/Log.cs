using System.Globalization;

namespace PulseBar.App.Services;

/// <summary>
/// Tiny local error log for bug reports. Capped in size, never sent anywhere,
/// and only ever contains exception details, not sensor data or personal information.
/// </summary>
internal static class Log
{
    private const long MaxBytes = 512 * 1024;
    private static readonly object Gate = new();

    public static void Error(string message, Exception? e = null)
    {
        try
        {
            lock (Gate)
            {
                var path = AppPaths.LogFile;
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                if (File.Exists(path) && new FileInfo(path).Length > MaxBytes)
                    File.Move(path, path + ".old", overwrite: true);
                var stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                File.AppendAllText(path, $"{stamp} {message}{(e is null ? "" : ": " + e)}{Environment.NewLine}");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Logging must never crash the app.
        }
    }
}
