using System.Runtime.InteropServices;
using AutoClacker.Core.Abstractions;
using AutoClacker.Core.Domain;

namespace AutoClacker.Windows;

internal partial class WindowsHotkey : IHotkeyService
{
    public event Action? Pressed;

    private readonly ILog _log;
    private IntPtr _hookId;
    private readonly LowLevelKeyboardProc _proc;
    private readonly IntPtr _procPtr;
    private uint _targetVk;
    private bool _targetDown;
    private bool _disposed;

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial IntPtr SetWindowsHookExW(int idHook, IntPtr lpfn, IntPtr hMod, uint dwThreadId);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UnhookWindowsHookEx(IntPtr hhk);

    [LibraryImport("user32.dll")]
    private static partial IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial IntPtr GetModuleHandle(string? lpModuleName);

    const int WH_KEYBOARD_LL = 13;
    const int WM_KEYDOWN = 0x0100;
    const int WM_KEYUP = 0x0101;
    const int WM_SYSKEYDOWN = 0x0104;
    const int WM_SYSKEYUP = 0x0105;

    public WindowsHotkey(ILog log)
    {
        _log = log;
        _proc = HookCallback;
        _procPtr = Marshal.GetFunctionPointerForDelegate(_proc);
    }

    public bool TryRegister(KeyToken key)
    {
        Unregister();
        _targetVk = NativeKeys.ToVirtualKey(key);
        if (_targetVk == 0)
        {
            _log.Info($"WindowsHotkey: Unsupported key '{key}'");
            return false;
        }

        _targetDown = false;
        _hookId = SetWindowsHookExW(WH_KEYBOARD_LL, _procPtr, GetModuleHandle(null), 0);
        if (_hookId == IntPtr.Zero)
        {
            _log.Info($"WindowsHotkey: SetWindowsHookEx failed, error={Marshal.GetLastPInvokeError()}");
            return false;
        }

        _log.Info($"WindowsHotkey: Registered '{key}' (vk=0x{_targetVk:X2})");
        return true;
    }

    public void Unregister()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
            _log.Info("WindowsHotkey: Unhooked");
        }
    }

    private unsafe IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = (int)wParam;
            uint vk = *(uint*)lParam;
            if (vk == _targetVk)
            {
                if (msg is WM_KEYDOWN or WM_SYSKEYDOWN)
                {
                    if (!_targetDown)
                    {
                        _targetDown = true;
                        Pressed?.Invoke();
                    }
                }
                else if (msg is WM_KEYUP or WM_SYSKEYUP)
                {
                    _targetDown = false;
                }
            }
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed) return;
        Unregister();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
