using AutoClacker.Core.Abstractions;

namespace AutoClacker.Linux;

/// <summary>Public Linux OS facade. Only construct on Linux. Requires X11/XWayland.</summary>
public sealed class LinuxPlatform : IPlatformServices
{
    private readonly LinuxInput _input;
    private readonly LinuxHotkey _hotkey;
    private bool _disposed;

    private LinuxPlatform(ILog log)
    {
        if (!OperatingSystem.IsLinux())
            throw new PlatformNotSupportedException("LinuxPlatform requires Linux.");

        _input = new LinuxInput(log);
        _hotkey = new LinuxHotkey(log);

        bool ok = _input.IsAvailable;
        Capabilities = new PlatformCapabilities(
            InputAvailable: ok,
            HotkeyAvailable: ok,
            UnavailableReason: ok
                ? null
                : "No X11 display (pure Wayland without XWayland is not supported)",
            SupportsDebugConsole: false);
        PlatformId = "Linux";
    }

    /// <summary>Must be the first Xlib call in the process (call from Desktop.Main before Avalonia).</summary>
    public static void InitializeXThreads()
    {
        if (OperatingSystem.IsLinux())
            X11Native.XInitThreads();
    }

    public static IPlatformServices Create(ILog? log = null) =>
        new LinuxPlatform(log ?? NullLog.Instance);

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
