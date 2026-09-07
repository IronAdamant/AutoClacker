using System.Runtime.InteropServices;
using Avalonia;
using AutoClacker.App;
using AutoClacker.Core.Abstractions;
using AutoClacker.Core.Logging;
using AutoClacker.Core.Settings;
using AutoClacker.Linux;
using AutoClacker.MacOS;
using AutoClacker.Windows;
using Microsoft.Win32.SafeHandles;

namespace AutoClacker.Desktop;

partial class Program
{
    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AllocConsole();

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool FreeConsole();

    [LibraryImport("kernel32.dll")]
    private static partial IntPtr GetConsoleWindow();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetStdHandle(int nStdHandle, IntPtr hHandle);

    [LibraryImport("kernel32.dll", EntryPoint = "CreateFileW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode,
        IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    const int SW_HIDE = 0;
    const int STD_OUTPUT_HANDLE = -11;
    const int STD_ERROR_HANDLE = -12;
    const uint GENERIC_WRITE = 0x40000000;
    const uint FILE_SHARE_WRITE = 2;
    const uint OPEN_EXISTING = 3;

    [STAThread]
    public static void Main(string[] args)
    {
        // First Xlib call in the process (Linux hotkey multi-thread safety).
        LinuxPlatform.InitializeXThreads();
        if (OperatingSystem.IsMacOS())
            MacAppBranding.Apply("AutoClacker");

        var settings = new SettingsStore().Load();
        var log = new FileLog { WritesEnabled = settings.EnableDebugLog };
        log.Init();
        if (OperatingSystem.IsWindows())
            ConfigureWindowsConsole(settings, log);

        var platform = CreatePlatform(log);
        log.Info($"Platform={platform.PlatformId} Input={platform.Capabilities.InputAvailable} Hotkey={platform.Capabilities.HotkeyAvailable}");
        log.Info($"DebugLog={settings.EnableDebugLog} path={log.FilePath}");
        log.Info("Starting classic desktop lifetime");

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            log.Info($"UnhandledException: {e.ExceptionObject}");
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            log.Info($"UnobservedTaskException: {e.Exception}");
            e.SetObserved();
        };

        BuildAvaloniaApp(platform, log).StartWithClassicDesktopLifetime(args);
    }

    static IPlatformServices CreatePlatform(ILog log) =>
        OperatingSystem.IsWindows() ? WindowsPlatform.Create(log) :
        OperatingSystem.IsMacOS() ? MacOSPlatform.Create(log) :
        OperatingSystem.IsLinux() ? LinuxPlatform.Create(log) :
        throw new PlatformNotSupportedException("AutoClacker supports Windows, macOS, and Linux only.");

    static void ConfigureWindowsConsole(AppSettings settings, FileLog log)
    {
        if (settings.ShowDebugConsole)
        {
            if (GetConsoleWindow() == IntPtr.Zero)
                AllocConsole();

            var conout = CreateFile("CONOUT$", GENERIC_WRITE, FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
            if (conout != IntPtr.Zero && conout != new IntPtr(-1))
            {
                SetStdHandle(STD_OUTPUT_HANDLE, conout);
                SetStdHandle(STD_ERROR_HANDLE, conout);
                var writer = new StreamWriter(new FileStream(new SafeFileHandle(conout, false), FileAccess.Write)) { AutoFlush = true };
                Console.SetOut(writer);
                Console.SetError(writer);
            }

            log.EnableConsole(true);
            Console.WriteLine("=== AutoClacker Debug Console ===");
            Console.WriteLine($"Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        }
        else
        {
            var existingConsole = GetConsoleWindow();
            if (existingConsole != IntPtr.Zero)
            {
                ShowWindow(existingConsole, SW_HIDE);
                FreeConsole();
            }
        }
    }

    public static AppBuilder BuildAvaloniaApp(IPlatformServices platform, ILog log) =>
        // Fully qualify — namespace AutoClacker.App shadows type App.
        AppBuilder.Configure<global::AutoClacker.App.App>(() =>
            {
                var app = new global::AutoClacker.App.App();
                app.Configure(platform, log);
                return app;
            })
            .UsePlatformDetect()
            .LogToTrace();
}
