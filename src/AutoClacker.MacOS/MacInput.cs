using System.Runtime.InteropServices;
using AutoClacker.Core.Abstractions;
using AutoClacker.Core.Domain;

namespace AutoClacker.MacOS;

internal partial class MacInput : IInputSimulation
{
    private readonly ILog _log;
    private readonly bool _trusted;

    private const string CoreGraphics = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";

    [LibraryImport(CoreGraphics)]
    private static partial IntPtr CGEventCreateMouseEvent(IntPtr source, int type, CGPoint point, int button);

    [LibraryImport(CoreGraphics)]
    private static partial IntPtr CGEventCreateKeyboardEvent(IntPtr source, ushort keycode,
        [MarshalAs(UnmanagedType.U1)] bool keyDown);

    [LibraryImport(CoreGraphics)]
    private static partial void CGEventPost(int tap, IntPtr eventRef);

    [LibraryImport(CoreGraphics)]
    private static partial void CFRelease(IntPtr cf);

    [LibraryImport(CoreGraphics)]
    private static partial CGPoint CGEventGetLocation(IntPtr eventRef);

    [LibraryImport(CoreGraphics)]
    private static partial IntPtr CGEventCreate(IntPtr source);

    [StructLayout(LayoutKind.Sequential)]
    private struct CGPoint { public double x, y; }

    public MacInput(ILog log, bool trusted)
    {
        _log = log;
        _trusted = trusted;
    }

    public void MouseClick(MouseButton button, ClickKind kind)
    {
        if (!_trusted)
        {
            _log.Info("MacInput: skipped click — Accessibility not granted");
            return;
        }

        (int down, int up, int btn) = button switch
        {
            MouseButton.Right => (3, 4, 1),
            MouseButton.Middle => (25, 26, 2),
            _ => (1, 2, 0),
        };
        var p = GetPos();
        Click(down, up, btn, p);
        if (kind == ClickKind.Double) Click(down, up, btn, p);
    }

    public void KeyPress(KeyToken key)
    {
        if (!_trusted)
        {
            _log.Info("MacInput: skipped key — Accessibility not granted");
            return;
        }

        ushort k = NativeKeys.ToKeycode(key);
        if (k == NativeKeys.Unsupported)
        {
            _log.Info($"MacInput: Unsupported key '{key}'");
            return;
        }

        var down = CGEventCreateKeyboardEvent(IntPtr.Zero, k, true);
        var up = CGEventCreateKeyboardEvent(IntPtr.Zero, k, false);
        if (down == IntPtr.Zero || up == IntPtr.Zero)
        {
            _log.Info("MacInput: CGEventCreateKeyboardEvent failed");
            if (down != IntPtr.Zero) CFRelease(down);
            if (up != IntPtr.Zero) CFRelease(up);
            return;
        }

        CGEventPost(0, down);
        CGEventPost(0, up);
        CFRelease(down);
        CFRelease(up);
    }

    static void Click(int downType, int upType, int button, CGPoint p)
    {
        var down = CGEventCreateMouseEvent(IntPtr.Zero, downType, p, button);
        var up = CGEventCreateMouseEvent(IntPtr.Zero, upType, p, button);
        if (down != IntPtr.Zero)
        {
            CGEventPost(0, down);
            CFRelease(down);
        }
        if (up != IntPtr.Zero)
        {
            CGEventPost(0, up);
            CFRelease(up);
        }
    }

    static CGPoint GetPos()
    {
        var e = CGEventCreate(IntPtr.Zero);
        if (e == IntPtr.Zero) return default;
        var p = CGEventGetLocation(e);
        CFRelease(e);
        return p;
    }

    public void Dispose() { }
}
