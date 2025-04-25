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

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        private const uint WM_LBUTTONDOWN = 0x0201;
        private const uint WM_LBUTTONUP = 0x0202;
        private const uint WM_RBUTTONDOWN = 0x0204;
        private const uint WM_RBUTTONUP = 0x0205;
        private const uint WM_KEYDOWN = 0x0100;
        private const uint WM_KEYUP = 0x0101;

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
                    Theme = Properties.Settings.Default.Theme
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

                TimeSpan effectiveInterval = settings.Interval.TotalMilliseconds < 1 ? TimeSpan.FromMilliseconds(1) : settings.Interval;

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
                                uint down = settings.MouseButton == "Left" ? 0x0002U : 0x0008U;
                                MouseEvent(down);
                                mouseButtonHeld = true;
                            }
                        }
                        else
                        {
                            if (mouseButtonHeld)
                            {
                                uint up = settings.MouseButton == "Left" ? 0x0004U : 0x0010U;
                                MouseEvent(up);
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
                    uint up = settings.MouseButton == "Left" ? 0x0004U : 0x0010U;
                    MouseEvent(up);
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
                uint down = settings.MouseButton == "Left" ? 0x0002U : 0x0008U;
                uint up = settings.MouseButton == "Left" ? 0x0004U : 0x0010U;
                if (settings.MouseMode == "Click")
                {
                    MouseEvent(down);
                    MouseEvent(up);
                }
                else if (settings.HoldMode == "HoldDuration")
                {
                    Console.WriteLine($"Holding mouse for {settings.MouseHoldDuration.TotalMilliseconds} ms, Interval: {settings.Interval.TotalMilliseconds} ms");
                    MouseEvent(down);
                    await Task.Delay(settings.MouseHoldDuration);
                    MouseEvent(up);
                }
            }
            else
            {
                if (settings.KeyboardMode == "Press")
                {
                    KeybdEvent((byte)KeyInterop.VirtualKeyFromKey(settings.KeyboardKey), 0);
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

            IntPtr hWnd = process.MainWindowHandle;
            if (settings.ActionType == "Mouse")
            {
                GetClientRect(hWnd, out RECT rect);
                int x = (rect.Right - rect.Left) / 2;
                int y = (rect.Bottom - rect.Top) / 2;
                IntPtr lParam = (IntPtr)((y << 16) | (x & 0xFFFF));
                uint down = settings.MouseButton == "Left" ? WM_LBUTTONDOWN : WM_RBUTTONDOWN;
                uint up = settings.MouseButton == "Left" ? WM_LBUTTONUP : WM_RBUTTONUP;

                if (settings.MouseMode == "Click")
                {
                    PostMessage(hWnd, down, IntPtr.Zero, lParam);
                    PostMessage(hWnd, up, IntPtr.Zero, lParam);
                }
                else if (settings.HoldMode == "HoldDuration")
                {
                    Console.WriteLine($"Holding mouse (restricted) for {settings.MouseHoldDuration.TotalMilliseconds} ms, Interval: {settings.Interval.TotalMilliseconds} ms");
                    PostMessage(hWnd, down, IntPtr.Zero, lParam);
                    await Task.Delay(settings.MouseHoldDuration);
                    PostMessage(hWnd, up, IntPtr.Zero, lParam);
                }
            }
            else
            {
                IntPtr wParam = (IntPtr)KeyInterop.VirtualKeyFromKey(settings.KeyboardKey);
                if (settings.KeyboardMode == "Press")
                {
                    PostMessage(hWnd, WM_KEYDOWN, wParam, IntPtr.Zero);
                    PostMessage(hWnd, WM_KEYUP, wParam, IntPtr.Zero);
                }
                else
                {
                    Console.WriteLine($"Holding keyboard (restricted) for {settings.KeyboardHoldDuration.TotalMilliseconds} ms, Interval: {settings.Interval.TotalMilliseconds} ms");
                    PostMessage(hWnd, WM_KEYDOWN, wParam, IntPtr.Zero);
                    await Task.Delay(settings.KeyboardHoldDuration);
                    PostMessage(hWnd, WM_KEYUP, wParam, IntPtr.Zero);
                }
            }
        }

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, IntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, IntPtr dwExtraInfo);

        private void MouseEvent(uint flags) => mouse_event(flags, 0, 0, 0, IntPtr.Zero);
        private void KeybdEvent(byte key, uint flags) => keybd_event(key, 0, flags, IntPtr.Zero);
    }
}