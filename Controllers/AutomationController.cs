using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AutoClacker.Models;
using AutoClacker.ViewModels;
using AutoClacker.Utilities;
using System.Windows.Input;

namespace AutoClacker.Controllers
{
    public class AutomationController
    {
        private readonly MainViewModel viewModel;
        private CancellationTokenSource cts;
        private readonly ApplicationDetector detector = new ApplicationDetector();

        // SendInput related structs and constants
        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private const uint INPUT_MOUSE = 0;
        private const uint INPUT_KEYBOARD = 1;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        private struct RECT { public int Left, Top, Right, Bottom; }

        public AutomationController(MainViewModel viewModel)
        {
            this.viewModel = viewModel;
        }

        public async Task StartAutomation()
        {
            if (cts != null) return; // Already running
            cts = new CancellationTokenSource();
            var localCts = cts;

            viewModel.UpdateStatus("Running", "Green");

            try
            {
                var settings = new Settings
                {
                    ClickScope = Properties.Settings.Default.ClickScope,
                    TargetApplication = Properties.Settings.Default.TargetApplication,
                    ActionType = Properties.Settings.Default.ActionType,
                    MouseButton = Properties.Settings.Default.MouseButton,
                    ClickType = Properties.Settings.Default.ClickType,
                    MouseMode = Properties.Settings.Default.MouseMode,
                    ClickMode = Properties.Settings.Default.ClickMode,
                    ClickDuration = Properties.Settings.Default.ClickDuration,
                    MouseHoldDuration = Properties.Settings.Default.MouseHoldDuration,
                    HoldMode = Properties.Settings.Default.HoldMode,
                    KeyboardKey = (Key)Properties.Settings.Default.KeyboardKey,
                    KeyboardMode = Properties.Settings.Default.KeyboardMode,
                    KeyboardHoldDuration = Properties.Settings.Default.KeyboardHoldDuration,
                    TriggerKey = (Key)Properties.Settings.Default.TriggerKey,
                    TriggerKeyModifiers = (ModifierKeys)Properties.Settings.Default.TriggerKeyModifiers,
                    Interval = Properties.Settings.Default.Interval,
                    Mode = Properties.Settings.Default.Mode,
                    TotalDuration = Properties.Settings.Default.TotalDuration,
                    Theme = Properties.Settings.Default.Theme,
                    IsTopmost = Properties.Settings.Default.IsTopmost
                };

                if (settings.ClickScope == "Restricted" && !ValidateTargetApplication(settings))
                {
                    StopAutomation("Target application not active");
                    return;
                }

                if (settings.ActionType == "Mouse" && settings.MouseMode == "Hold" && settings.HoldMode == "HoldDuration" && settings.MouseHoldDuration > settings.Interval)
                {
                    StopAutomation("Mouse hold duration must be less than or equal to interval");
                    return;
                }
                if (settings.ActionType == "Keyboard" && settings.KeyboardMode == "Hold" && settings.KeyboardHoldDuration != TimeSpan.Zero && settings.KeyboardHoldDuration > settings.Interval)
                {
                    StopAutomation("Keyboard hold duration must be less than or equal to interval");
                    return;
                }

                // Increase minimum interval to 100ms to prevent overly rapid input
                TimeSpan effectiveInterval = settings.Interval.TotalMilliseconds < 100 ? TimeSpan.FromMilliseconds(100) : settings.Interval;

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                bool mouseButtonHeld = false;

                viewModel.StartTimers(); // Start the timers for decrementing displays

                while (!localCts.IsCancellationRequested)
                {
                    var cycleStartTime = stopwatch.Elapsed;

                    if (settings.ClickScope == "Restricted")
                        await PerformRestrictedAction(settings);
                    else
                    {
                        if (settings.ActionType == "Mouse" && settings.MouseMode == "Hold" && settings.HoldMode == "ConstantHold")
                        {
                            if (!mouseButtonHeld)
                            {
                                MouseEventDown(settings);
                                mouseButtonHeld = true;
                            }
                        }
                        else
                        {
                            if (mouseButtonHeld)
                            {
                                MouseEventUp(settings);
                                mouseButtonHeld = false;
                            }
                            await PerformGlobalAction(settings);
                        }
                    }

                    if (settings.ActionType == "Mouse" && settings.MouseMode == "Click" && settings.ClickMode == "Duration" && settings.ClickDuration != TimeSpan.Zero && stopwatch.Elapsed >= settings.ClickDuration)
                    {
                        break;
                    }
                    if (settings.ActionType == "Keyboard" && settings.KeyboardMode == "Press" && settings.Mode == "Timer" && stopwatch.Elapsed >= settings.TotalDuration)
                    {
                        break;
                    }

                    var cycleElapsedTime = stopwatch.Elapsed - cycleStartTime;
                    var remainingCycleTime = effectiveInterval - cycleElapsedTime;
                    if (remainingCycleTime.TotalMilliseconds > 0)
                    {
                        await Task.Delay(remainingCycleTime, localCts.Token);
                    }
                }

                if (mouseButtonHeld)
                {
                    MouseEventUp(settings);
                }

                StopAutomation("Automation completed");
            }
            catch (TaskCanceledException)
            {
                StopAutomation("Automation stopped");
            }
            catch (Exception ex)
            {
                StopAutomation($"Error: {ex.Message}");
            }
        }

        public void StopAutomation(string message = "Not running")
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
            viewModel.StopTimers(); // Stop the timers when automation ends
            viewModel.UpdateStatus(message, "Red");
        }

        private bool ValidateTargetApplication(Settings settings)
        {
            var process = detector.GetProcessByName(settings.TargetApplication);
            return process != null && !IsIconic(process.MainWindowHandle);
        }

        private async Task PerformGlobalAction(Settings settings)
        {
            if (settings.ActionType == "Mouse")
            {
                if (settings.MouseMode == "Click")
                {
                    MouseEventDown(settings);
                    await Task.Delay(10); // Small delay to ensure click is registered
                    MouseEventUp(settings);
                    if (settings.ClickType == "Double")
                    {
                        await Task.Delay(50); // Delay between clicks for double click
                        MouseEventDown(settings);
                        await Task.Delay(10);
                        MouseEventUp(settings);
                    }
                }
                else if (settings.HoldMode == "HoldDuration")
                {
                    Console.WriteLine($"Holding mouse for {settings.MouseHoldDuration.TotalMilliseconds} ms, Interval: {settings.Interval.TotalMilliseconds} ms");
                    MouseEventDown(settings);
                    await Task.Delay(settings.MouseHoldDuration);
                    MouseEventUp(settings);
                }
            }
            else
            {
                if (settings.KeyboardMode == "Press")
                {
                    KeybdEvent((byte)KeyInterop.VirtualKeyFromKey(settings.KeyboardKey), 0);
                    await Task.Delay(50); // Increased delay to prevent buffering issues
                    KeybdEvent((byte)KeyInterop.VirtualKeyFromKey(settings.KeyboardKey), 2);
                }
                else
                {
                    Console.WriteLine($"Holding keyboard for {settings.KeyboardHoldDuration.TotalMilliseconds} ms, Interval: {settings.Interval.TotalMilliseconds} ms");
                    KeybdEvent((byte)KeyInterop.VirtualKeyFromKey(settings.KeyboardKey), 0);
                    await Task.Delay(settings.KeyboardHoldDuration);
                    KeybdEvent((byte)KeyInterop.VirtualKeyFromKey(settings.KeyboardKey), 2);
                }
            }
        }

        private async Task PerformRestrictedAction(Settings settings)
        {
            var process = detector.GetProcessByName(settings.TargetApplication);
            if (process == null || IsIconic(process.MainWindowHandle))
            {
                StopAutomation("Target application not active");
                return;
            }

            GetClientRect(process.MainWindowHandle, out RECT rect);
            int x = (rect.Right - rect.Left) / 2;
            int y = (rect.Bottom - rect.Top) / 2;

            if (settings.ActionType == "Mouse")
            {
                // For restricted mode, we need to move the cursor to the target position
                // Set cursor position (not implemented here, as SendInput can directly simulate clicks)
                if (settings.MouseMode == "Click")
                {
                    MouseEventDown(settings);
                    await Task.Delay(10);
                    MouseEventUp(settings);
                    if (settings.ClickType == "Double")
                    {
                        await Task.Delay(50);
                        MouseEventDown(settings);
                        await Task.Delay(10);
                        MouseEventUp(settings);
                    }
                }
                else if (settings.HoldMode == "HoldDuration")
                {
                    Console.WriteLine($"Holding mouse (restricted) for {settings.MouseHoldDuration.TotalMilliseconds} ms, Interval: {settings.Interval.TotalMilliseconds} ms");
                    MouseEventDown(settings);
                    await Task.Delay(settings.MouseHoldDuration);
                    MouseEventUp(settings);
                }
            }
            else
            {
                // Keyboard input remains the same for restricted mode
                if (settings.KeyboardMode == "Press")
                {
                    KeybdEvent((byte)KeyInterop.VirtualKeyFromKey(settings.KeyboardKey), 0);
                    await Task.Delay(50); // Increased delay to prevent buffering issues
                    KeybdEvent((byte)KeyInterop.VirtualKeyFromKey(settings.KeyboardKey), 2);
                }
                else
                {
                    Console.WriteLine($"Holding keyboard (restricted) for {settings.KeyboardHoldDuration.TotalMilliseconds} ms, Interval: {settings.Interval.TotalMilliseconds} ms");
                    KeybdEvent((byte)KeyInterop.VirtualKeyFromKey(settings.KeyboardKey), 0);
                    await Task.Delay(settings.KeyboardHoldDuration);
                    KeybdEvent((byte)KeyInterop.VirtualKeyFromKey(settings.KeyboardKey), 2);
                }
            }
        }

        private void MouseEventDown(Settings settings)
        {
            INPUT[] inputs = new INPUT[1];
            inputs[0].type = INPUT_MOUSE;
            inputs[0].u.mi.dx = 0;
            inputs[0].u.mi.dy = 0;
            inputs[0].u.mi.mouseData = 0;
            inputs[0].u.mi.time = 0;
            inputs[0].u.mi.dwExtraInfo = IntPtr.Zero;

            switch (settings.MouseButton)
            {
                case "Left":
                    inputs[0].u.mi.dwFlags = MOUSEEVENTF_LEFTDOWN;
                    break;
                case "Right":
                    inputs[0].u.mi.dwFlags = MOUSEEVENTF_RIGHTDOWN;
                    break;
                case "Middle":
                    inputs[0].u.mi.dwFlags = MOUSEEVENTF_MIDDLEDOWN;
                    break;
            }

            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private void MouseEventUp(Settings settings)
        {
            INPUT[] inputs = new INPUT[1];
            inputs[0].type = INPUT_MOUSE;
            inputs[0].u.mi.dx = 0;
            inputs[0].u.mi.dy = 0;
            inputs[0].u.mi.mouseData = 0;
            inputs[0].u.mi.time = 0;
            inputs[0].u.mi.dwExtraInfo = IntPtr.Zero;

            switch (settings.MouseButton)
            {
                case "Left":
                    inputs[0].u.mi.dwFlags = MOUSEEVENTF_LEFTUP;
                    break;
                case "Right":
                    inputs[0].u.mi.dwFlags = MOUSEEVENTF_RIGHTUP;
                    break;
                case "Middle":
                    inputs[0].u.mi.dwFlags = MOUSEEVENTF_MIDDLEUP;
                    break;
            }

            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private void KeybdEvent(byte key, uint flags)
        {
            INPUT[] inputs = new INPUT[1];
            inputs[0].type = INPUT_KEYBOARD;
            inputs[0].u.ki.wVk = key;
            inputs[0].u.ki.wScan = 0;
            inputs[0].u.ki.dwFlags = flags;
            inputs[0].u.ki.time = 0;
            inputs[0].u.ki.dwExtraInfo = IntPtr.Zero;

            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}