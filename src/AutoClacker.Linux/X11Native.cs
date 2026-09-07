using System.Runtime.InteropServices;

namespace AutoClacker.Linux;

internal static partial class X11Native
{
    private const string LibX11 = "libX11.so.6";
    private const string LibXtst = "libXtst.so.6";

    [LibraryImport(LibX11)]
    public static partial int XInitThreads();

    [LibraryImport(LibX11, StringMarshalling = StringMarshalling.Utf8)]
    public static partial IntPtr XOpenDisplay(string? displayName);

    [LibraryImport(LibX11)]
    public static partial int XCloseDisplay(IntPtr display);

    [LibraryImport(LibX11)]
    public static partial int XFlush(IntPtr display);

    [LibraryImport(LibX11)]
    public static partial int XSync(IntPtr display, [MarshalAs(UnmanagedType.Bool)] bool discard);

    [LibraryImport(LibX11)]
    public static partial IntPtr XDefaultRootWindow(IntPtr display);

    [LibraryImport(LibXtst)]
    public static partial int XTestFakeButtonEvent(IntPtr display, uint button,
        [MarshalAs(UnmanagedType.Bool)] bool isPress, CULong delay);

    [LibraryImport(LibXtst)]
    public static partial int XTestFakeKeyEvent(IntPtr display, uint keycode,
        [MarshalAs(UnmanagedType.Bool)] bool isPress, CULong delay);

    [LibraryImport(LibX11, StringMarshalling = StringMarshalling.Utf8)]
    public static partial CULong XStringToKeysym(string str);

    [LibraryImport(LibX11)]
    public static partial byte XKeysymToKeycode(IntPtr display, CULong keysym);

    [LibraryImport(LibX11)]
    public static partial int XGrabKey(IntPtr display, int keycode, uint modifiers,
        IntPtr grabWindow, [MarshalAs(UnmanagedType.Bool)] bool ownerEvents, int pointerMode, int keyboardMode);

    [LibraryImport(LibX11)]
    public static partial int XUngrabKey(IntPtr display, int keycode, uint modifiers, IntPtr grabWindow);

    [LibraryImport(LibX11)]
    public static partial int XNextEvent(IntPtr display, ref XEvent eventReturn);

    [LibraryImport(LibX11)]
    public static partial int XSelectInput(IntPtr display, IntPtr window, long eventMask);

    [LibraryImport(LibX11)]
    public static partial int XSendEvent(IntPtr display, IntPtr window,
        [MarshalAs(UnmanagedType.Bool)] bool propagate, long eventMask, ref XEvent eventSend);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int XErrorHandlerDelegate(IntPtr display, ref XErrorEvent errorEvent);

    [LibraryImport(LibX11)]
    public static partial IntPtr XSetErrorHandler(IntPtr handler);

    public const int GrabModeAsync = 1;
    public const long KeyPressMask = 1L << 0;
    public const long KeyReleaseMask = 1L << 1;
    public const int KeyPress = 2;
    public const int KeyRelease = 3;
    public const byte BadAccess = 10;
    public const uint LockMask = 1 << 1;
    public const uint Mod2Mask = 1 << 4;

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
        public CULong resourceid;
        public CULong serial;
        public byte error_code;
        public byte request_code;
        public byte minor_code;
    }
}
