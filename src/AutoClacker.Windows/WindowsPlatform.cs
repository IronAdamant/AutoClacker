using AutoClacker.Core.Abstractions;

namespace AutoClacker.Windows;

/// <summary>Public Windows OS facade. Only construct on Windows.</summary>
public sealed class WindowsPlatform : IPlatformServices
{
    private readonly WindowsInput _input;
    private readonly WindowsHotkey _hotkey;
    private bool _disposed;

    private WindowsPlatform(ILog log)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("WindowsPlatform requires Windows.");

        _input = new WindowsInput(log);
        _hotkey = new WindowsHotkey(log);
        Capabilities = new PlatformCapabilities(
            InputAvailable: true,
            HotkeyAvailable: true,
            UnavailableReason: null,
            SupportsDebugConsole: true);
        PlatformId = "Windows";
    }

    public static IPlatformServices Create(ILog? log = null) =>
        new WindowsPlatform(log ?? NullLog.Instance);

    public string PlatformId { get; }
    public PlatformCapabilities Capabilities { get; }
    public IInputSimulation Input => _input;
    public IHotkeyService Hotkey => _hotkey;

    public void Dispose()
    {
        if (_disposed) return;
        _hotkey.Dispose();
        _input.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
