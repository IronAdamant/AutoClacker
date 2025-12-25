using System;
using System.Diagnostics;
using System.IO;
using AutoClacker.Models;

namespace AutoClacker.Services
{
    /// <summary>
    /// Debug logger that writes to log file and optionally console.
    /// </summary>
    public static class Logger
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AutoClacker", "debug.log");

        private static readonly object _lock = new();
        private static bool _initialized;
        private static bool _consoleEnabled;
        private static int _lastCounterLength;

        [Conditional("DEBUG")]
        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            
            try
            {
                var dir = Path.GetDirectoryName(LogPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                
                File.WriteAllText(LogPath, $"=== AutoClacker Debug Log ===\n");
                Log("Application started");
                Log($"Log file: {LogPath}");
            }
            catch { }
        }

        public static void EnableConsole(bool enabled)
        {
            _consoleEnabled = enabled;
        }

        [Conditional("DEBUG")]
        public static void Log(string message)
        {
            try
            {
                lock (_lock)
                {
                    var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n";
                    File.AppendAllText(LogPath, line);
                    Debug.WriteLine(line.TrimEnd());
                    
                    if (_consoleEnabled)
                    {
                        Console.WriteLine(line.TrimEnd());
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Updates a counter on the same console line (no newline spam).
        /// </summary>
        public static void UpdateCounter(string mode, int count, bool running)
        {
            if (!_consoleEnabled) return;
            
            try
            {
                string action = mode == "Mouse" ? "Mouse clicks" : "Key presses";
                string status = running ? "RUNNING" : "STOPPED";
                string line = $"\r[{status}] {action} since activated: {count}     ";
                
                Console.Write(line);
                _lastCounterLength = line.Length;
            }
            catch { }
        }

        /// <summary>
        /// Clears the counter line and prints a summary.
        /// </summary>
        public static void PrintSummary(string mode, int count)
        {
            if (!_consoleEnabled) return;
            
            try
            {
                string action = mode == "Mouse" ? "Mouse clicks" : "Key presses";
                Console.WriteLine($"\r[STOPPED] {action} total: {count}                    ");
            }
            catch { }
        }

        public static string GetLogPath() => LogPath;
    }
}
