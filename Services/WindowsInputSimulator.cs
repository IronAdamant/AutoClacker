using System;
using System.Runtime.InteropServices;

namespace AutoClacker.Services
{
    public interface IInputSimulator
    {
        void MouseClick(string button, bool doubleClick = false);
        void KeyPress(string key);
        bool IsAvailable { get; }
        string PlatformName { get; }
    }

    /// <summary>Windows input via user32.dll SendInput.</summary>
    public class WindowsInputSimulator : IInputSimulator
    {
        public string PlatformName => "Windows";
        public bool IsAvailable => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("kernel32.dll")]
        private static extern uint GetLastError();

        const uint INPUT_MOUSE = 0, INPUT_KEYBOARD = 1;
        const uint MOUSEEVENTF_LEFTDOWN = 0x0002, MOUSEEVENTF_LEFTUP = 0x0004;
        const uint MOUSEEVENTF_RIGHTDOWN = 0x0008, MOUSEEVENTF_RIGHTUP = 0x0010;
        const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020, MOUSEEVENTF_MIDDLEUP = 0x0040;
        const uint KEYEVENTF_KEYUP = 0x0002;

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx, dy;
            public uint mouseData, dwFlags, time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk, wScan;
            public uint dwFlags, time;
            public IntPtr dwExtraInfo;
        }

        public void MouseClick(string button, bool doubleClick = false)
        {
            Logger.Log($"MouseClick called: button={button}, double={doubleClick}, available={IsAvailable}");
            if (!IsAvailable) { Logger.Log("MouseClick: Platform not available"); return; }

            uint down, up;
            switch (button.ToLower())
            {
                case "right": down = MOUSEEVENTF_RIGHTDOWN; up = MOUSEEVENTF_RIGHTUP; break;
                case "middle": down = MOUSEEVENTF_MIDDLEDOWN; up = MOUSEEVENTF_MIDDLEUP; break;
                default: down = MOUSEEVENTF_LEFTDOWN; up = MOUSEEVENTF_LEFTUP; break;
            }

            var inputs = new INPUT[2];
            inputs[0].type = INPUT_MOUSE;
            inputs[0].u.mi.dwFlags = down;
            inputs[1].type = INPUT_MOUSE;
            inputs[1].u.mi.dwFlags = up;

            int size = Marshal.SizeOf<INPUT>();
            Logger.Log($"MouseClick: Sending INPUT size={size}, flags down={down} up={up}");
            
            uint result = SendInput(2, inputs, size);
            uint err = GetLastError();
            Logger.Log($"MouseClick: SendInput returned {result}, GetLastError={err}");
            
            if (doubleClick)
            {
                result = SendInput(2, inputs, size);
                Logger.Log($"MouseClick (double): SendInput returned {result}");
            }
        }

        public void KeyPress(string key)
        {
            Logger.Log($"KeyPress called: key={key}, available={IsAvailable}");
            if (!IsAvailable) { Logger.Log("KeyPress: Platform not available"); return; }

            ushort vk = GetVirtualKeyCode(key);
            Logger.Log($"KeyPress: VK code for '{key}' = 0x{vk:X2} ({vk})");
            if (vk == 0) { Logger.Log("KeyPress: VK is 0, returning"); return; }

            var inputs = new INPUT[2];
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].u.ki.wVk = vk;
            inputs[1].type = INPUT_KEYBOARD;
            inputs[1].u.ki.wVk = vk;
            inputs[1].u.ki.dwFlags = KEYEVENTF_KEYUP;

            int size = Marshal.SizeOf<INPUT>();
            Logger.Log($"KeyPress: Sending INPUT size={size}");
            
            uint result = SendInput(2, inputs, size);
            uint err = GetLastError();
            Logger.Log($"KeyPress: SendInput returned {result}, GetLastError={err}");
        }

        static ushort GetVirtualKeyCode(string key)
        {
            var k = key.ToUpper();
            if (k == "SPACE") return 0x20;
            if (k == "ENTER") return 0x0D;
            if (k == "TAB") return 0x09;
            if (k == "ESCAPE") return 0x1B;
            if (k.Length == 1 && k[0] >= 'A' && k[0] <= 'Z') return (ushort)(0x41 + k[0] - 'A');
            if (k.Length == 1 && k[0] >= '0' && k[0] <= '9') return (ushort)(0x30 + k[0] - '0');
            if (k.StartsWith("F") && int.TryParse(k[1..], out int n) && n >= 1 && n <= 12) return (ushort)(0x6F + n);
            Logger.Log($"GetVirtualKeyCode: Unknown key '{key}'");
            return 0;
        }
    }
}
