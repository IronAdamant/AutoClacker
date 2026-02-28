using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace AutoClacker.Services
{
    /// <summary>
    /// Global keyboard hook for detecting hotkeys even when app is not focused.
    /// Windows-only implementation using SetWindowsHookEx.
    /// </summary>
    public class GlobalHotkey : IGlobalHotkey
    {
        public event Action? OnHotkeyPressed;
        
        private IntPtr _hookId = IntPtr.Zero;
        private readonly LowLevelKeyboardProc _proc;
        private uint _targetVk;
        private bool _disposed;

        delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetModuleHandle(string? lpModuleName);

        const int WH_KEYBOARD_LL = 13;
        const int WM_KEYDOWN = 0x0100;

        public GlobalHotkey()
        {
            _proc = HookCallback;
        }

        public void Register(string keyName)
        {
            _targetVk = GetVkFromKeyName(keyName);
            Logger.Log($"GlobalHotkey.Register: keyName={keyName}, vk=0x{_targetVk:X2}");
            
            if (_targetVk == 0) return;
            if (_hookId != IntPtr.Zero) Unregister();

            _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(null), 0);
            Logger.Log($"GlobalHotkey: SetWindowsHookEx returned {_hookId}");
        }

        public void Unregister()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                Logger.Log("GlobalHotkey: Unhooked");
                _hookId = IntPtr.Zero;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (vkCode == _targetVk)
                {
                    Logger.Log($"GlobalHotkey: Detected VK=0x{vkCode:X2}");
                    OnHotkeyPressed?.Invoke();
                }
            }
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        static uint GetVkFromKeyName(string key)
        {
            var k = key.ToUpper();
            if (k == "SPACE") return 0x20;
            if (k.StartsWith("F") && int.TryParse(k[1..], out int n) && n >= 1 && n <= 12) return (uint)(0x6F + n);
            if (k.Length == 1 && k[0] >= 'A' && k[0] <= 'Z') return (uint)(0x41 + k[0] - 'A');
            return 0;
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
