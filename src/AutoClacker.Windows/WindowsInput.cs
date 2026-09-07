using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AutoClacker.Core.Abstractions;
using AutoClacker.Core.Domain;

namespace AutoClacker.Windows;

internal partial class WindowsInput : IInputSimulation
{
    private readonly ILog _log;

    public WindowsInput(ILog log) => _log = log;

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial uint SendInput(uint nInputs, ReadOnlySpan<INPUT> pInputs, int cbSize);

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

    public void MouseClick(MouseButton button, ClickKind kind)
    {
        (uint down, uint up) = button switch
        {
            MouseButton.Right => (MOUSEEVENTF_RIGHTDOWN, MOUSEEVENTF_RIGHTUP),
            MouseButton.Middle => (MOUSEEVENTF_MIDDLEDOWN, MOUSEEVENTF_MIDDLEUP),
            _ => (MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP),
        };

        Span<INPUT> inputs = stackalloc INPUT[2];
        inputs[0].type = INPUT_MOUSE;
        inputs[0].u.mi.dwFlags = down;
        inputs[1].type = INPUT_MOUSE;
        inputs[1].u.mi.dwFlags = up;
        Send(inputs);
        if (kind == ClickKind.Double) Send(inputs);
    }

    public void KeyPress(KeyToken key)
    {
        ushort vk = NativeKeys.ToVirtualKey(key);
        if (vk == 0)
        {
            _log.Info($"WindowsInput: Unsupported key '{key}'");
            return;
        }

        Span<INPUT> inputs = stackalloc INPUT[2];
        inputs[0].type = INPUT_KEYBOARD;
        inputs[0].u.ki.wVk = vk;
        inputs[1].type = INPUT_KEYBOARD;
        inputs[1].u.ki.wVk = vk;
        inputs[1].u.ki.dwFlags = KEYEVENTF_KEYUP;
        Send(inputs);
    }

    void Send(ReadOnlySpan<INPUT> inputs)
    {
        uint sent = SendInput((uint)inputs.Length, inputs, Unsafe.SizeOf<INPUT>());
        if (sent != inputs.Length)
            _log.Info($"WindowsInput: SendInput sent {sent}/{inputs.Length}, error={Marshal.GetLastPInvokeError()}");
    }

    public void Dispose() { }
}
