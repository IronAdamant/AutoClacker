using System.Runtime.InteropServices;
using AutoClacker.Core.Abstractions;
using AutoClacker.Core.Domain;

namespace AutoClacker.Linux;

internal class LinuxInput : IInputSimulation
{
    private readonly ILog _log;
    private IntPtr _display;
    private bool _available;

    public bool IsAvailable => _available;

    public LinuxInput(ILog log)
    {
        _log = log;
        if (!OperatingSystem.IsLinux()) return;

        _display = X11Native.XOpenDisplay(null);
        if (_display == IntPtr.Zero)
        {
            _log.Info("LinuxInput: XOpenDisplay failed (pure Wayland with no XWayland?)");
            return;
        }

        _available = true;
        _log.Info("LinuxInput: X11 display opened");
    }

    public void MouseClick(MouseButton button, ClickKind kind)
    {
        if (!_available) return;
        uint b = button switch { MouseButton.Right => 3, MouseButton.Middle => 2, _ => 1 };
        Click(b);
        if (kind == ClickKind.Double) Click(b);
    }

    public void KeyPress(KeyToken key)
    {
        if (!_available) return;
        string? keysymName = NativeKeys.ToKeysymName(key);
        if (keysymName is null)
        {
            _log.Info($"LinuxInput: Unsupported key '{key}'");
            return;
        }

        var keysym = X11Native.XStringToKeysym(keysymName);
        if (keysym.Value == 0)
        {
            _log.Info($"LinuxInput: Unknown keysym for '{key}'");
            return;
        }

        byte keycode = X11Native.XKeysymToKeycode(_display, keysym);
        if (keycode == 0)
        {
            _log.Info($"LinuxInput: No keycode for '{keysymName}'");
            return;
        }

        X11Native.XTestFakeKeyEvent(_display, keycode, true, new CULong(0));
        X11Native.XTestFakeKeyEvent(_display, keycode, false, new CULong(0));
        X11Native.XFlush(_display);
    }

    void Click(uint button)
    {
        X11Native.XTestFakeButtonEvent(_display, button, true, new CULong(0));
        X11Native.XTestFakeButtonEvent(_display, button, false, new CULong(0));
        X11Native.XFlush(_display);
    }

    public void Dispose()
    {
        if (_display != IntPtr.Zero)
        {
            _available = false;
            X11Native.XCloseDisplay(_display);
            _display = IntPtr.Zero;
        }
        GC.SuppressFinalize(this);
    }
}
