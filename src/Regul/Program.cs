using System.Diagnostics;
using Avalonia;
using Avalonia.Win32;
using ReactiveUI.Avalonia;
using Regul.Logging;
using Regul.Other;

namespace Regul;

public static class Program
{
    private static FileStream? _lockFile;

    public static string[] Arguments { get; private set; } = [];
    public static bool IsCrashReport { get; private set; }
    public static string CrashReportText { get; private set; } = string.Empty;

    [STAThread]
    public static void Main(string[] args)
    {
#if DEBUG
        // Pipe Debug.WriteLine to console in debug builds
        Trace.Listeners.Add(new ConsoleTraceListener());
#endif

        Arguments = args;

        // Check if launched as crash report viewer
        if (args.Length >= 2 && args[0] == "--crash-report")
        {
            IsCrashReport = true;
            CrashReportText = args.Length >= 2 ? Uri.UnescapeDataString(args[1]) : string.Empty;
            Debug.WriteLine("[Program] Launching in crash report mode");
            Debug.WriteLine($"[Program] Crash text length: {CrashReportText.Length}");
            
            // Load settings before showing crash reporter
            ApplicationSettings.Load();
            Debug.WriteLine("[Program] ApplicationSettings loaded in crash report mode");
            
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            
            // After crash reporter closes, check if restart was requested
            Debug.WriteLine($"[Program] Crash reporter closed. RestartingApp: {ApplicationSettings.Current.RestartingApp}");
            if (ApplicationSettings.Current.RestartingApp)
            {
                Debug.WriteLine("[Program] Restarting application from crash reporter...");
                ApplicationSettings.Current.RestartingApp = false;
                ApplicationSettings.Save();
                
                string exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                Debug.WriteLine($"[Program] Executable path: {exePath}");
                
                if (!string.IsNullOrEmpty(exePath))
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = exePath,
                        UseShellExecute = true
                    };
                    Process.Start(startInfo);
                    Debug.WriteLine("[Program] New process started from crash reporter");
                }
            }
            
            return;
        }

        // Single-instance lock
        try
        {
            _lockFile = File.Open(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".lock"),
                FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            _lockFile.Lock(0, 0);
        }
        catch
        {
            // Another instance is running — pass files to it
            if (!Directory.Exists(RegulDirectories.Cache))
                Directory.CreateDirectory(RegulDirectories.Cache);
            if (!Directory.Exists(Path.Combine(RegulDirectories.Cache, "OpenFiles")))
                Directory.CreateDirectory(Path.Combine(RegulDirectories.Cache, "OpenFiles"));

            string newArgs = string.Join("|", args);
            File.WriteAllText(Path.Combine(RegulDirectories.Cache, "OpenFiles", Guid.NewGuid() + ".cache"), newArgs);

            EventWaitHandle eventWaitHandle = new(false, EventResetMode.AutoReset, "Onebeld-Regul-MemoryMap-dG17tr7Nv3_BytesWritten");
            eventWaitHandle.Set();
            return;
        }

        AppDomain.CurrentDomain.UnhandledException += (_, e) => HandleUnhandledException("Non-UI", (Exception)e.ExceptionObject);
        TaskScheduler.UnobservedTaskException += (_, e) => 
        {
            HandleUnhandledException("Task", e.Exception);
            e.SetObserved(); // Mark as observed to prevent app termination before we handle it
        };
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            _lockFile?.Unlock(0, 0);
            _lockFile?.Dispose();
        };

        ApplicationSettings.Load();
        Debug.WriteLine("[Program] ApplicationSettings loaded");

        try
        {
            Debug.WriteLine("[Program] Starting Avalonia app");
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            Debug.WriteLine("[Program] Avalonia app exited normally");
            
            // Check if app should restart
            Debug.WriteLine($"[Program] RestartingApp flag: {ApplicationSettings.Current.RestartingApp}");
            if (ApplicationSettings.Current.RestartingApp)
            {
                Debug.WriteLine("[Program] Restarting application...");
                ApplicationSettings.Current.RestartingApp = false;
                ApplicationSettings.Save();
                
                string exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                Debug.WriteLine($"[Program] Executable path: {exePath}");
                
                if (!string.IsNullOrEmpty(exePath))
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = exePath,
                        UseShellExecute = true
                    };
                    Process.Start(startInfo);
                    Debug.WriteLine("[Program] New process started");
                }
                else
                {
                    Debug.WriteLine("[Program] ERROR: Could not determine executable path");
                }
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine($"[Program] FATAL EXCEPTION: {e}");
            HandleUnhandledException("Application", e);
        }
    }

    private static void HandleUnhandledException(string category, Exception ex)
    {
        Debug.WriteLine($"[Program] HandleUnhandledException category={category}");
        Debug.WriteLine($"[Program] Exception: {ex}");
        
        try
        {
            Logger.Instance.WriteLog(LogType.Error, $"[Fatal {category} Error] {ex}\r\n");
            Logger.Instance.SaveLogs();
        }
        catch { /* ignored */ }

        if (!IsCrashReport)
        {
            try
            {
                string exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                string crashText = Uri.EscapeDataString(ex.ToString());
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = $"--crash-report \"{crashText}\"",
                    UseShellExecute = true
                };
                
                Process.Start(startInfo);
                Debug.WriteLine("[Program] Crash reporter process started");
                
#if DEBUG
                // In debug mode, wait a bit for the crash reporter to start before exiting
                // This allows the debugger to remain attached to see what's happening
                if (Debugger.IsAttached)
                {
                    Debug.WriteLine("[Program] Debugger attached - waiting 2 seconds before exit");
                    Thread.Sleep(2000);
                }
#endif
            }
            catch (Exception launchEx)
            {
                Debug.WriteLine($"[Program] Failed to launch crash reporter: {launchEx}");
            }
        }

        Environment.Exit(-1);
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        AppBuilder appBuilder = AppBuilder.Configure<App>();
        return appBuilder.ConfigureAppBuilder();
    }

    public static AppBuilder ConfigureAppBuilder(this AppBuilder appBuilder)
    {
        appBuilder.UseSkia().UseHarfBuzz().UseReactiveUI(_ => { });

#if Windows
        appBuilder.UseWin32()
            .With(new AngleOptions
            {
                AllowedPlatformApis = [AngleOptions.PlatformApi.DirectX11]
            })
            .With(new Win32PlatformOptions
            {
                OverlayPopups = true
            });
#elif OSX
        appBuilder.UsePlatformDetect()
            .With(new MacOSPlatformOptions
            {
                DisableDefaultApplicationMenuItems = true,
                ShowInDock = false
            });
#else
        appBuilder.UsePlatformDetect()
            .With(new AvaloniaNativePlatformOptions
            {
                UseGpu = ApplicationSettings.Current.HardwareAcceleration,
            });
#endif

#if DEBUG
        return appBuilder.LogToTrace();
#else
        return appBuilder;
#endif
    }
}
