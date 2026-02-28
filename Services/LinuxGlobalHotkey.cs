using System;
using System.Threading;

namespace AutoClacker.Services
{
    /// <summary>
    /// Linux global hotkey using X11 XGrabKey on the root window.
    /// Uses its own X11 display connection (X11 is not thread-safe).
    /// </summary>
    public class LinuxGlobalHotkey : IGlobalHotkey
    {
        public event Action? OnHotkeyPressed;

        private IntPtr _display;
        private IntPtr _root;
        private int _keycode;
        private Thread? _listenerThread;
        private volatile bool _running;
        private bool _disposed;

        // Suppress X11 errors (e.g. BadAccess if key already grabbed by another app)
        private static readonly X11Native.XErrorHandlerDelegate ErrorHandler = (IntPtr display, ref X11Native.XErrorEvent error) => 0;

        public void Register(string keyName)
        {
            Unregister();

            _display = X11Native.XOpenDisplay(null);
            if (_display == IntPtr.Zero)
            {
                Logger.Log("LinuxGlobalHotkey: XOpenDisplay failed");
                return;
            }

            X11Native.XSetErrorHandler(ErrorHandler);

            _root = X11Native.XDefaultRootWindow(_display);

            string keysymName = X11Native.MapKeyToKeysymName(keyName);
            ulong keysym = X11Native.XStringToKeysym(keysymName);
            if (keysym == 0)
            {
                Logger.Log($"LinuxGlobalHotkey: Unknown keysym for '{keyName}'");
                X11Native.XCloseDisplay(_display);
                _display = IntPtr.Zero;
                return;
            }

            _keycode = (int)X11Native.XKeysymToKeycode(_display, keysym);
            if (_keycode == 0)
            {
                Logger.Log($"LinuxGlobalHotkey: No keycode for keysym '{keysymName}'");
                X11Native.XCloseDisplay(_display);
                _display = IntPtr.Zero;
                return;
            }

            // Grab with common modifier combos so Caps/Num Lock don't interfere
            GrabKeyWithModifiers(_keycode, 0);
            GrabKeyWithModifiers(_keycode, X11Native.LockMask);
            GrabKeyWithModifiers(_keycode, X11Native.Mod2Mask);
            GrabKeyWithModifiers(_keycode, X11Native.LockMask | X11Native.Mod2Mask);

            X11Native.XSelectInput(_display, _root, X11Native.KeyPressMask);

            _running = true;
            _listenerThread = new Thread(ListenerLoop) { IsBackground = true, Name = "X11HotkeyListener" };
            _listenerThread.Start();

            Logger.Log($"LinuxGlobalHotkey: Registered keycode={_keycode} for '{keyName}'");
        }

        public void Unregister()
        {
            if (_display == IntPtr.Zero) return;

            _running = false;

            // Send a synthetic event to unblock XNextEvent
            var fakeEvent = new X11Native.XEvent { type = X11Native.KeyPress, keycode = 0 };
            X11Native.XSendEvent(_display, _root, false, X11Native.KeyPressMask, ref fakeEvent);
            X11Native.XFlush(_display);

            _listenerThread?.Join(2000);
            _listenerThread = null;

            UngrabKeyWithModifiers(_keycode, 0);
            UngrabKeyWithModifiers(_keycode, X11Native.LockMask);
            UngrabKeyWithModifiers(_keycode, X11Native.Mod2Mask);
            UngrabKeyWithModifiers(_keycode, X11Native.LockMask | X11Native.Mod2Mask);

            X11Native.XCloseDisplay(_display);
            _display = IntPtr.Zero;
            _keycode = 0;

            Logger.Log("LinuxGlobalHotkey: Unregistered");
        }

        private void ListenerLoop()
        {
            Logger.Log("LinuxGlobalHotkey: Listener thread started");
            var evt = new X11Native.XEvent();

            while (_running)
            {
                X11Native.XNextEvent(_display, ref evt);

                if (!_running) break;

                if (evt.type == X11Native.KeyPress && evt.keycode == (uint)_keycode)
                {
                    Logger.Log($"LinuxGlobalHotkey: Key detected, keycode={evt.keycode}");
                    OnHotkeyPressed?.Invoke();
                }
            }

            Logger.Log("LinuxGlobalHotkey: Listener thread exiting");
        }

        private void GrabKeyWithModifiers(int keycode, uint modifiers)
        {
            X11Native.XGrabKey(_display, keycode, modifiers, _root,
                true, X11Native.GrabModeAsync, X11Native.GrabModeAsync);
        }

        private void UngrabKeyWithModifiers(int keycode, uint modifiers)
        {
            X11Native.XUngrabKey(_display, keycode, modifiers, _root);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Unregister();
                _disposed = true;
            }
        }
    }
}
