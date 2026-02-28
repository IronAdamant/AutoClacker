using System;
using System.Runtime.InteropServices;

namespace AutoClacker.Services
{
    /// <summary>
    /// Centralised X11/XTest P/Invoke declarations for Linux.
    /// All libraries (libX11, libXtst) are built-in OS components.
    /// </summary>
    public static class X11Native
    {
        private const string LibX11 = "libX11.so.6";
        private const string LibXtst = "libXtst.so.6";

        // Display management
        [DllImport(LibX11)]
        public static extern IntPtr XOpenDisplay(string? displayName);

        [DllImport(LibX11)]
        public static extern int XCloseDisplay(IntPtr display);

        [DllImport(LibX11)]
        public static extern int XFlush(IntPtr display);

        [DllImport(LibX11)]
        public static extern IntPtr XDefaultRootWindow(IntPtr display);

        // XTest extension - input simulation
        [DllImport(LibXtst)]
        public static extern int XTestFakeButtonEvent(IntPtr display, uint button, bool isPress, ulong delay);

        [DllImport(LibXtst)]
        public static extern int XTestFakeKeyEvent(IntPtr display, uint keycode, bool isPress, ulong delay);

        // Key symbol/code mapping
        [DllImport(LibX11)]
        public static extern ulong XStringToKeysym(string str);

        [DllImport(LibX11)]
        public static extern uint XKeysymToKeycode(IntPtr display, ulong keysym);

        // Global hotkey support
        [DllImport(LibX11)]
        public static extern int XGrabKey(IntPtr display, int keycode, uint modifiers,
            IntPtr grabWindow, bool ownerEvents, int pointerMode, int keyboardMode);

        [DllImport(LibX11)]
        public static extern int XUngrabKey(IntPtr display, int keycode, uint modifiers, IntPtr grabWindow);

        [DllImport(LibX11)]
        public static extern int XNextEvent(IntPtr display, ref XEvent eventReturn);

        [DllImport(LibX11)]
        public static extern int XSelectInput(IntPtr display, IntPtr window, long eventMask);

        [DllImport(LibX11)]
        public static extern int XSendEvent(IntPtr display, IntPtr window, bool propagate, long eventMask, ref XEvent eventSend);

        // Error handling
        public delegate int XErrorHandlerDelegate(IntPtr display, ref XErrorEvent errorEvent);

        [DllImport(LibX11)]
        public static extern IntPtr XSetErrorHandler(XErrorHandlerDelegate handler);

        // Constants
        public const int GrabModeAsync = 1;
        public const long KeyPressMask = 1L << 0;
        public const int KeyPress = 2;

        // Modifier masks
        public const uint AnyModifier = 1 << 15;
        public const uint LockMask = 1 << 1;    // Caps Lock
        public const uint Mod2Mask = 1 << 4;     // Num Lock

        /// <summary>X11 XEvent union — 192 bytes on x86_64. Keycode at offset 84.</summary>
        [StructLayout(LayoutKind.Explicit, Size = 192)]
        public struct XEvent
        {
            [FieldOffset(0)] public int type;
            [FieldOffset(84)] public uint keycode;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct XErrorEvent
        {
            public int type;
            public IntPtr display;
            public ulong resourceid;
            public ulong serial;
            public byte error_code;
            public byte request_code;
            public byte minor_code;
        }

        /// <summary>
        /// Maps a key name (e.g. "F6", "A", "space") to an X11 keysym name
        /// that XStringToKeysym understands.
        /// </summary>
        public static string MapKeyToKeysymName(string key)
        {
            var k = key.ToUpper();
            if (k.StartsWith("F") && int.TryParse(k[1..], out int n) && n >= 1 && n <= 12)
                return key; // F1-F12 work as-is
            return k switch
            {
                "SPACE" => "space",
                "ENTER" => "Return",
                "TAB" => "Tab",
                "ESCAPE" => "Escape",
                _ when k.Length == 1 && char.IsLetter(k[0]) => k.ToLower(),
                _ => key
            };
        }
    }
}
