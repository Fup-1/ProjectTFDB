using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ProjectTFDB.Services;

namespace ProjectTFDB;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        AppLogger.Initialize();
        AppLogger.Log("App startup");

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        base.OnStartup(e);
    }

    private void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            AppLogger.Log(ex, "AppDomain.UnhandledException");
            return;
        }

        AppLogger.Log($"AppDomain.UnhandledException: {e.ExceptionObject}");
    }

    private void OnDispatcherUnhandledException(object? sender, DispatcherUnhandledExceptionEventArgs e)
    {
        AppLogger.Log(e.Exception, "DispatcherUnhandledException");
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        AppLogger.Log(e.Exception, "UnobservedTaskException");
    }
}
