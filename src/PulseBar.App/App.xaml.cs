using System.Windows;
using System.Windows.Threading;
using PulseBar.App.Services;

namespace PulseBar.App;

public sealed partial class App : Application, IDisposable
{
    // Per-session ("Local\") so each signed-in user can run their own copy.
    private const string MutexName = @"Local\PulseBar-7d1f3c2a-5b8e-4a9f-9c61-2e4d8b0f6a13";

    private Mutex? _singleInstance;
    private PulseBarController? _controller;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstance = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        if (!createdNew)
        {
            Shutdown();
            return;
        }

        DispatcherUnhandledException += OnUnhandled;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            Log.Error("Unhandled exception", args.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Error("Unobserved task exception", args.Exception);
            args.SetObserved();
        };

        _controller = new PulseBarController(this);
        _controller.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Dispose();
        base.OnExit(e);
    }

    public void Dispose()
    {
        _controller?.Dispose();
        _controller = null;
        _singleInstance?.Dispose();
        _singleInstance = null;
    }

    private static void OnUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // A monitor should keep running through a glitch; log it for bug reports.
        Log.Error("UI exception", e.Exception);
        e.Handled = true;
    }
}
