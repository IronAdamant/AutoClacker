using System.Runtime.InteropServices;
using AutoClacker.Core.Abstractions;
using AutoClacker.Core.Domain;

namespace AutoClacker.Linux;

internal class LinuxHotkey : IHotkeyService
{
    public event Action? Pressed;

    private readonly ILog _log;
    private IntPtr _display;
    private IntPtr _root;
    private int _keycode;
    private Thread? _listenerThread;
    private volatile bool _running;
    private bool _disposed;
    private int _grabErrors;
    private bool _targetDown;

    private readonly X11Native.XErrorHandlerDelegate _errorHandler;
    private readonly IntPtr _errorHandlerPtr;

    public LinuxHotkey(ILog log)
    {
        _log = log;
        _errorHandler = OnXError;
        _errorHandlerPtr = Marshal.GetFunctionPointerForDelegate(_errorHandler);
    }

    public bool TryRegister(KeyToken key)
    {
        Unregister();

        _display = X11Native.XOpenDisplay(null);
        if (_display == IntPtr.Zero)
        {
            _log.Info("LinuxHotkey: XOpenDisplay failed (no X11/XWayland)");
            return false;
        }

        _root = X11Native.XDefaultRootWindow(_display);

        string? keysymName = NativeKeys.ToKeysymName(key);
        var keysym = keysymName is null ? new CULong(0) : X11Native.XStringToKeysym(keysymName);
        if (keysym.Value == 0)
        {
            _log.Info($"LinuxHotkey: Unsupported key '{key}'");
            CloseDisplay();
            return false;
        }

        _keycode = X11Native.XKeysymToKeycode(_display, keysym);
        if (_keycode == 0)
        {
            _log.Info($"LinuxHotkey: No keycode for '{keysymName}'");
            CloseDisplay();
            return false;
        }

        _grabErrors = 0;
        var previousHandler = X11Native.XSetErrorHandler(_errorHandlerPtr);
        X11Native.XGrabKey(_display, _keycode, 0, _root, true, X11Native.GrabModeAsync, X11Native.GrabModeAsync);
        X11Native.XGrabKey(_display, _keycode, X11Native.LockMask, _root, true, X11Native.GrabModeAsync, X11Native.GrabModeAsync);
        X11Native.XGrabKey(_display, _keycode, X11Native.Mod2Mask, _root, true, X11Native.GrabModeAsync, X11Native.GrabModeAsync);
        X11Native.XGrabKey(_display, _keycode, X11Native.LockMask | X11Native.Mod2Mask, _root, true, X11Native.GrabModeAsync, X11Native.GrabModeAsync);
        X11Native.XSync(_display, false);
        X11Native.XSetErrorHandler(previousHandler);

        if (_grabErrors >= 4)
        {
            _log.Info($"LinuxHotkey: XGrabKey failed for '{key}' — key already grabbed");
            CloseDisplay();
            return false;
        }

        X11Native.XSelectInput(_display, _root, X11Native.KeyPressMask | X11Native.KeyReleaseMask);

        _targetDown = false;
        _running = true;
        _listenerThread = new Thread(ListenerLoop) { IsBackground = true, Name = "X11HotkeyListener" };
        _listenerThread.Start();

        _log.Info($"LinuxHotkey: Registered keycode={_keycode} for '{key}'");
        return true;
    }

    public void Unregister()
    {
        if (_display == IntPtr.Zero) return;

        _running = false;

        var fakeEvent = new X11Native.XEvent { type = X11Native.KeyPress, keycode = 0 };
        X11Native.XSendEvent(_display, _root, false, X11Native.KeyPressMask, ref fakeEvent);
        X11Native.XFlush(_display);

        var thread = _listenerThread;
        _listenerThread = null;
        if (thread is not null && !thread.Join(2000))
        {
            _log.Info("LinuxHotkey: Listener did not exit in time — abandoning display");
            _display = IntPtr.Zero;
            _keycode = 0;
            return;
        }

        X11Native.XUngrabKey(_display, _keycode, 0, _root);
        X11Native.XUngrabKey(_display, _keycode, X11Native.LockMask, _root);
        X11Native.XUngrabKey(_display, _keycode, X11Native.Mod2Mask, _root);
        X11Native.XUngrabKey(_display, _keycode, X11Native.LockMask | X11Native.Mod2Mask, _root);

        CloseDisplay();
        _keycode = 0;
        _log.Info("LinuxHotkey: Unregistered");
    }

    private int OnXError(IntPtr display, ref X11Native.XErrorEvent errorEvent)
    {
        if (errorEvent.error_code == X11Native.BadAccess) _grabErrors++;
        return 0;
    }

    private void ListenerLoop()
    {
        var evt = new X11Native.XEvent();
        while (_running)
        {
            X11Native.XNextEvent(_display, ref evt);
            if (!_running) break;

            if (evt.keycode != (uint)_keycode) continue;

            if (evt.type == X11Native.KeyPress)
            {
                // Auto-repeat suppression: only fire once until KeyRelease
                if (!_targetDown)
                {
                    _targetDown = true;
                    Pressed?.Invoke();
                }
            }
            else if (evt.type == X11Native.KeyRelease)
            {
                _targetDown = false;
            }
        }
    }

    private void CloseDisplay()
    {
        if (_display != IntPtr.Zero)
        {
            X11Native.XCloseDisplay(_display);
            _display = IntPtr.Zero;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        Unregister();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
