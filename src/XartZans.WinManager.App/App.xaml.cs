using System.Windows;
using System.Windows.Threading;
using XartZans.WinManager.App.Services;

namespace XartZans.WinManager.App;

public partial class App : Application
{
    public static IAppLogger Logger { get; private set; } = new FileAppLogger();

    protected override void OnStartup(StartupEventArgs e)
    {
        Logger = new FileAppLogger();
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        try
        {
            LocalizationService.Initialize();
            ThemeService.Initialize();
        }
        catch (Exception exception)
        {
            Logger.Error("Could not initialize application language resources.", exception);
            MessageBox.Show(
                "Language catalogs could not be loaded. Restore a valid JSON file in the Languages folder and restart the app.",
                "Language resources unavailable",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        base.OnStartup(e);
        Logger.Information("Application started.");

        var window = new MainWindow();
        MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Logger.Information("Application exited.");
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Logger.Error("Unhandled UI exception.", e.Exception);
        MessageBox.Show(
            LocalizationService.Get("UnhandledError"),
            LocalizationService.Get("ErrorTitle"),
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            Logger.Error("Unhandled application exception.", exception);
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Logger.Error("Unobserved task exception.", e.Exception);
        e.SetObserved();
    }
}
