using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AutoClacker.Services
{
    /// <summary>Linux input via xdotool CLI.</summary>
    public class LinuxInputSimulator : IInputSimulator
    {
        public string PlatformName => "Linux";
        public bool IsAvailable => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        public void MouseClick(string btn, bool dbl = false)
        {
            if (!IsAvailable) return;
            int b = btn.ToLower() switch { "right" => 3, "middle" => 2, _ => 1 };
            Exec(dbl ? $"xdotool click --repeat 2 {b}" : $"xdotool click {b}");
        }

        public void KeyPress(string key)
        {
            if (!IsAvailable) return;
            string k = key.ToLower() switch { "space" => "space", "enter" => "Return", "tab" => "Tab", "escape" => "Escape", _ => key };
            Exec($"xdotool key {k}");
        }

        void Exec(string cmd)
        {
            try { Process.Start(new ProcessStartInfo("/bin/bash", $"-c \"{cmd}\"") { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true })?.WaitForExit(1000); } catch { }
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
