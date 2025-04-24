using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AutoClicker.Utilities;

namespace AutoClicker
{
    public class AutomationController
    {
        private MainForm form;
        private CancellationTokenSource cts;
        private ApplicationDetector detector = new ApplicationDetector();

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

        public AutomationController(MainForm form)
        {
            this.form = form;
        }

        public async Task StartAutomation(Settings settings)
        {
            if (cts != null) return; // Already running
            cts = new CancellationTokenSource();
            var localCts = cts; // Store a local reference to avoid null issues

            form.Invoke((MethodInvoker)(() => form.UpdateStatus("Running", Color.Green)));

            try
            {
                if (settings.ClickScope == "Restricted" && !ValidateTargetApplication(settings))
                {
                    StopAutomation("Target application not active");
                    return;
                }

                if (settings.ActionType == "Mouse" && settings.MouseHoldDuration > settings.Interval ||
                    settings.ActionType == "Keyboard" && settings.KeyboardHoldDuration > settings.Interval)
                {
                    StopAutomation("Hold duration must be less than or equal to interval");
                    return;
                }

                // Ensure interval is at least 1ms
                TimeSpan effectiveInterval = settings.Interval.TotalMilliseconds < 1 ? TimeSpan.FromMilliseconds(1) : settings.Interval;

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                while (!localCts.IsCancellationRequested)
                {
                    if (settings.ClickScope == "Restricted")
                        await PerformRestrictedAction(settings);
                    else
                        await PerformGlobalAction(settings);

                    if (settings.Mode == "Timer" && stopwatch.Elapsed >= settings.TotalDuration)
                        break;

                    await Task.Delay(effectiveInterval, localCts.Token);
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
            form.Invoke((MethodInvoker)(() => form.UpdateStatus(message, Color.Red)));
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
                else
                {
                    MouseEvent(down);
                    await Task.Delay(settings.MouseHoldDuration);
                    MouseEvent(up);
                    await Task.Delay(settings.Interval - settings.MouseHoldDuration);
                }
            }
            else
            {
                if (settings.KeyboardMode == "Press")
                {
                    KeybdEvent((byte)settings.KeyboardKey, 0);
                    KeybdEvent((byte)settings.KeyboardKey, 2);
                }
                else
                {
                    KeybdEvent((byte)settings.KeyboardKey, 0);
                    await Task.Delay(settings.KeyboardHoldDuration);
                    KeybdEvent((byte)settings.KeyboardKey, 2);
                    await Task.Delay(settings.Interval - settings.KeyboardHoldDuration);
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
                else
                {
                    PostMessage(hWnd, down, IntPtr.Zero, lParam);
                    await Task.Delay(settings.MouseHoldDuration);
                    PostMessage(hWnd, up, IntPtr.Zero, lParam);
                    await Task.Delay(settings.Interval - settings.MouseHoldDuration);
                }
            }
            else
            {
                IntPtr wParam = (IntPtr)settings.KeyboardKey;
                if (settings.KeyboardMode == "Press")
                {
                    PostMessage(hWnd, WM_KEYDOWN, wParam, IntPtr.Zero);
                    PostMessage(hWnd, WM_KEYUP, wParam, IntPtr.Zero);
                }
                else
                {
                    PostMessage(hWnd, WM_KEYDOWN, wParam, IntPtr.Zero);
                    await Task.Delay(settings.KeyboardHoldDuration);
                    PostMessage(hWnd, WM_KEYUP, wParam, IntPtr.Zero);
                    await Task.Delay(settings.Interval - settings.KeyboardHoldDuration);
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