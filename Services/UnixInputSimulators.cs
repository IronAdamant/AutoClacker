using System;
using System.Runtime.InteropServices;

namespace AutoClacker.Services
{
    /// <summary>Linux input via X11 XTest extension (no external dependencies).</summary>
    public class LinuxInputSimulator : IInputSimulator
    {
        public string PlatformName => "Linux";
        public bool IsAvailable { get; }

        private readonly IntPtr _display;

        public LinuxInputSimulator()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                IsAvailable = false;
                return;
            }

            _display = X11Native.XOpenDisplay(null);
            if (_display == IntPtr.Zero)
            {
                Logger.Log("LinuxInputSimulator: XOpenDisplay failed (pure Wayland with no XWayland?)");
                IsAvailable = false;
                return;
            }

            IsAvailable = true;
            Logger.Log("LinuxInputSimulator: X11 display opened successfully");
        }

        public void MouseClick(string btn, bool dbl = false)
        {
            if (!IsAvailable) return;
            uint b = btn.ToLower() switch { "right" => 3, "middle" => 2, _ => 1 };
            Click(b);
            if (dbl) Click(b);
        }

        public void KeyPress(string key)
        {
            if (!IsAvailable) return;
            string keysymName = X11Native.MapKeyToKeysymName(key);
            ulong keysym = X11Native.XStringToKeysym(keysymName);
            if (keysym == 0) { Logger.Log($"LinuxInputSimulator: Unknown keysym for '{key}'"); return; }

            uint keycode = X11Native.XKeysymToKeycode(_display, keysym);
            if (keycode == 0) { Logger.Log($"LinuxInputSimulator: No keycode for keysym '{keysymName}'"); return; }

            X11Native.XTestFakeKeyEvent(_display, keycode, true, 0);
            X11Native.XTestFakeKeyEvent(_display, keycode, false, 0);
            X11Native.XFlush(_display);
        }

        private void Click(uint button)
        {
            X11Native.XTestFakeButtonEvent(_display, button, true, 0);
            X11Native.XTestFakeButtonEvent(_display, button, false, 0);
            X11Native.XFlush(_display);
        }
    }

    /// <summary>macOS input via Quartz CGEvent.</summary>
    public class MacInputSimulator : IInputSimulator
    {
        public string PlatformName => "macOS";
        public bool IsAvailable => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")] static extern IntPtr CGEventCreateMouseEvent(IntPtr s, int t, CGP p, int b);
        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")] static extern IntPtr CGEventCreateKeyboardEvent(IntPtr s, ushort k, bool d);
        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")] static extern void CGEventPost(int t, IntPtr e);
        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")] static extern void CFRelease(IntPtr c);
        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")] static extern CGP CGEventGetLocation(IntPtr e);
        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")] static extern IntPtr CGEventCreate(IntPtr s);
        [StructLayout(LayoutKind.Sequential)] struct CGP { public double x, y; }

        public void MouseClick(string btn, bool dbl = false)
        {
            if (!IsAvailable) return;
            (int d, int u, int b) = btn.ToLower() switch { "right" => (3, 4, 1), "middle" => (25, 26, 2), _ => (1, 2, 0) };
            var p = GetPos(); Click(d, u, b, p); if (dbl) Click(d, u, b, p);
        }

        public void KeyPress(string key)
        {
            if (!IsAvailable) return;
            ushort k = key.ToUpper() switch {
                "SPACE" => 49, "ENTER" => 36, "TAB" => 48, "ESCAPE" => 53,
                var s when s.Length == 1 && char.IsLetter(s[0]) => (ushort)(new byte[] { 0, 11, 8, 2, 14, 3, 5, 4, 34, 38, 40, 37, 46, 45, 31, 35, 12, 15, 1, 17, 32, 9, 13, 7, 16, 6 }[s[0] - 'A']),
                _ => 49
            };
            var d = CGEventCreateKeyboardEvent(IntPtr.Zero, k, true); var u = CGEventCreateKeyboardEvent(IntPtr.Zero, k, false);
            CGEventPost(0, d); CGEventPost(0, u); CFRelease(d); CFRelease(u);
        }

        void Click(int dt, int ut, int b, CGP p) { var d = CGEventCreateMouseEvent(IntPtr.Zero, dt, p, b); var u = CGEventCreateMouseEvent(IntPtr.Zero, ut, p, b); CGEventPost(0, d); CGEventPost(0, u); CFRelease(d); CFRelease(u); }
        CGP GetPos() { var e = CGEventCreate(IntPtr.Zero); var p = CGEventGetLocation(e); CFRelease(e); return p; }
    }
}
