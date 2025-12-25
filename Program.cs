using Avalonia;
using System;
using System.IO;
using System.Runtime.InteropServices;
using AutoClacker.Models;
using Microsoft.Win32.SafeHandles;

namespace AutoClacker;

class Program
{
    [DllImport("kernel32.dll")] static extern bool AllocConsole();
    [DllImport("kernel32.dll")] static extern bool FreeConsole();
    [DllImport("kernel32.dll")] static extern IntPtr GetConsoleWindow();
    [DllImport("kernel32.dll")] static extern bool AttachConsole(uint pid);
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr GetStdHandle(int nStdHandle);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool SetStdHandle(int nStdHandle, IntPtr hHandle);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)] 
    static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);
    
    const int SW_HIDE = 0;
    const int STD_OUTPUT_HANDLE = -11;
    const int STD_ERROR_HANDLE = -12;
    const uint ATTACH_PARENT_PROCESS = 0xFFFFFFFF;
    const uint GENERIC_WRITE = 0x40000000;
    const uint OPEN_EXISTING = 3;

    [STAThread]
    public static void Main(string[] args)
    {
        var settings = Settings.Load();
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (settings.ShowDebugConsole)
            {
                // Try to attach to parent console or create new one
                var existingConsole = GetConsoleWindow();
                if (existingConsole == IntPtr.Zero)
                {
                    AllocConsole();
                }
                
                // Reopen stdout to CONOUT$
                var conout = CreateFile("CONOUT$", GENERIC_WRITE, 2, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                if (conout != IntPtr.Zero && conout != new IntPtr(-1))
                {
                    SetStdHandle(STD_OUTPUT_HANDLE, conout);
                    var fs = new FileStream(new SafeFileHandle(conout, false), FileAccess.Write);
                    var writer = new StreamWriter(fs) { AutoFlush = true };
                    Console.SetOut(writer);
                    Console.SetError(writer);
                }
                
                AutoClacker.Services.Logger.EnableConsole(true);
                Console.WriteLine("=== AutoClacker Debug Console ===");
                Console.WriteLine($"Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"Settings: Mode={settings.Mode}, Interval={settings.IntervalMs}ms, TriggerKey={settings.TriggerKey}");
                Console.WriteLine("Press hotkey to start/stop. Counter updates live below.");
                Console.WriteLine();
            }
            else
            {
                // Hide and detach console if debug is off
                var existingConsole = GetConsoleWindow();
                if (existingConsole != IntPtr.Zero)
                {
                    ShowWindow(existingConsole, SW_HIDE);
                    FreeConsole();
                }
            }
        }
        
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
