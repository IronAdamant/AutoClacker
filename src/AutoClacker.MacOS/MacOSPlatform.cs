using System.Runtime.InteropServices;
using AutoClacker.Core.Abstractions;

namespace AutoClacker.MacOS;

/// <summary>Public macOS OS facade. Only construct on macOS.</summary>
public sealed partial class MacOSPlatform : IPlatformServices
{
    private const string ApplicationServices =
        "/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices";

    [LibraryImport(ApplicationServices)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static partial bool AXIsProcessTrusted();

    private readonly MacInput _input;
    private readonly MacHotkey _hotkey;
    private bool _disposed;

    private MacOSPlatform(ILog log)
    {
        if (!OperatingSystem.IsMacOS())
            throw new PlatformNotSupportedException("MacOSPlatform requires macOS.");

        bool trusted = IsProcessTrusted();
        _input = new MacInput(log, trusted);
        _hotkey = new MacHotkey(log);

        string? reason = trusted
            ? null
            : "Accessibility permission required (System Settings → Privacy & Security → Accessibility)";

        Capabilities = new PlatformCapabilities(
            InputAvailable: trusted,
            HotkeyAvailable: trusted,
            UnavailableReason: reason,
            SupportsDebugConsole: false);
        PlatformId = "macOS";

        if (!trusted)
            log.Info($"MacOSPlatform: {reason}");
    }

    internal static bool IsProcessTrusted()
    {
        try { return AXIsProcessTrusted(); }
        catch { return false; }
    }

    public static IPlatformServices Create(ILog? log = null) =>
        new MacOSPlatform(log ?? NullLog.Instance);

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
