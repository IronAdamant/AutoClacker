using System.Diagnostics;
using AutoClacker.Core.Abstractions;

namespace AutoClacker.Core.Logging;

/// <summary>Optional debug file log. Info lines are written only when <see cref="WritesEnabled"/> is true.</summary>
public sealed class FileLog : ILog
{
    private readonly object _gate = new();
    private readonly string _path;
    private StreamWriter? _writer;
    private bool _consoleEnabled;
    private bool _enabled;
    private bool _initialized;

    public FileLog(string? filePath = null)
    {
        _path = filePath ?? System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AutoClacker", "debug.log");
    }

    public string FilePath => _path;

    public bool WritesEnabled
    {
        get
        {
            lock (_gate) return _enabled;
        }
        set
        {
            lock (_gate)
            {
                if (_enabled == value) return;
                _enabled = value;
                if (!_enabled)
                    CloseWriterUnlocked();
                else if (_initialized)
                    OpenWriterUnlocked();
            }
        }
    }

    public void Init()
    {
        lock (_gate)
        {
            _initialized = true;
            if (_enabled)
                OpenWriterUnlocked();
        }

        if (WritesEnabled)
            Info("=== Log started ===");
    }

    public void EnableConsole(bool enabled) => _consoleEnabled = enabled;

    public void Info(string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}";
        Debug.WriteLine(line);
        lock (_gate)
        {
            if (!_enabled) return;
            try { _writer?.WriteLine(line); }
            catch { /* ignore */ }
        }
    }

    public void UpdateCounter(string mode, int count)
    {
        if (!_consoleEnabled) return;
        try
        {
            Console.Write($"\r[{mode}] clicks={count}   ");
        }
        catch { /* ignore */ }
    }

    public void PrintSummary(string mode, int count)
    {
        if (!_consoleEnabled) return;
        try
        {
            Console.WriteLine();
            Console.WriteLine($"[{mode}] finished — {count} iterations");
        }
        catch { /* ignore */ }
    }

    public void DisposeWriter()
    {
        lock (_gate)
            CloseWriterUnlocked();
    }

    void OpenWriterUnlocked()
    {
        if (_writer is not null) return;
        try
        {
            var dir = System.IO.Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            _writer = new StreamWriter(_path, append: true) { AutoFlush = true };
        }
        catch
        {
            _writer = null;
        }
    }

    void CloseWriterUnlocked()
    {
        try { _writer?.Dispose(); }
        catch { /* ignore */ }
        _writer = null;
    }
}
