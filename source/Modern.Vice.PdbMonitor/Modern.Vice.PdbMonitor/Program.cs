using System;
using Avalonia;
using Microsoft.Extensions.Hosting;
using NLog;
#if SQUIRREL
using Squirrel;
#endif

namespace Modern.Vice.PdbMonitor;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    public static void Main(string[] args)
    {
        NLog.Common.InternalLogger.LogToConsole = true;
        var logger = LogManager.GetCurrentClassLogger();
        try
        {
#if SQUIRREL
            if (OperatingSystem.IsWindows())
            {
                SquirrelAwareApp.HandleEvents(
                onInitialInstall: OnAppInstall,
                onAppUninstall: OnAppUninstall,
                onEveryRun: OnAppRun);
            }
#endif
            var host = CreateHostBuilder(args);
            Core.IoC.Init(host.Build());
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            // NLog: catch any exception and log it.
            logger.Error(ex, "Stopped program because of an exception");
            throw;
        }
        finally
        {
            // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
            LogManager.Shutdown();
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((_, services) =>
                services.Configure()
            );

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();

#if SQUIRREL
    static void OnAppInstall(SemanticVersion version, IAppTools tools)
    {
        if (OperatingSystem.IsWindows())
        {
            tools.CreateShortcutForThisExe(ShortcutLocation.StartMenu);
        }
    }

    static void OnAppUninstall(SemanticVersion version, IAppTools tools)
    {
        if (OperatingSystem.IsWindows())
        {
            tools.RemoveShortcutForThisExe(ShortcutLocation.StartMenu);
        }
    }

    static void OnAppRun(SemanticVersion version, IAppTools tools, bool firstRun)
    {
        tools.SetProcessAppUserModelId();
        // show a welcome message when the app is first installed
        //if (firstRun) MessageBox.Show("Thanks for installing my application!");
    }
#endif
}
