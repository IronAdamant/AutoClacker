using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AutoClacker.Models;
using AutoClacker.ViewModels;
using AutoClacker.Utilities;
using System.Windows.Input;
using System.Diagnostics;

namespace AutoClacker.Controllers
{
    public class AutomationController
    {
        private readonly MainViewModel viewModel;
        private CancellationTokenSource cts;
        private readonly ApplicationDetector detector = new ApplicationDetector();
        private bool mouseButtonHeld;
        private bool keyboardKeyHeld;
        private Task currentAutomationTask;
        private readonly object automationLock = new object();
        private volatile bool isRunning;
        private TaskCompletionSource<bool> taskCompletionSource;

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
            this.viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }

        public async Task StartAutomation()
        {
            Console.WriteLine("StartAutomation called.");
            lock (automationLock)
            {
                if (isRunning)
                {
                    Console.WriteLine("Automation already running. Stopping previous automation.");
                    StopAutomation("Previous automation stopped to start a new one.");
                    if (taskCompletionSource != null)
                    {
                        try
                        {
                            taskCompletionSource.Task.Wait();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error waiting for previous automation task to stop: {ex.Message}");
                        }
                    }
                }

                cts = new CancellationTokenSource();
                isRunning = true;
                taskCompletionSource = new TaskCompletionSource<bool>();
            }

            var token = cts.Token;
            currentAutomationTask = Task.Run(async () =>
            {
                try
                {
                    viewModel.UpdateStatus("Running", "Green");

                    if (!ValidateSettings())
                    {
                        StopAutomation("Invalid settings detected");
                        return;
                    }

                    Settings settings = viewModel.CurrentSettings;
                    if (settings.ClickScope == "Restricted" && !ValidateTargetApplication(settings))
                    {
                        StopAutomation("Target application not active");
                        return;
                    }

                    TimeSpan effectiveInterval = settings.Interval.TotalMilliseconds < 100 ? TimeSpan.FromMilliseconds(100) : settings.Interval;

                    mouseButtonHeld = false;
                    keyboardKeyHeld = false;

                    var stopwatch = Stopwatch.StartNew();
                    bool shouldBreak = false;

                    while (!token.IsCancellationRequested && !shouldBreak)
                    {
                        var cycleStartTime = stopwatch.Elapsed;

                        if (settings.ClickScope == "Restricted")
                        {
                            await PerformRestrictedAction(settings, stopwatch, token);
                        }
                        else
                        {
                            if (settings.ActionType == "Mouse" && settings.MouseMode == "Hold" && settings.HoldMode == "ConstantHold")
                            {
                                if (!mouseButtonHeld)
                                {
                                    if (settings.MousePhysicalHoldMode)
                                        await SimulatePhysicalMouseHold(settings, token, stopwatch);
                                    else
                                        MouseEventDown(settings);
                                    mouseButtonHeld = true;
                                }
                            }
                            else if (settings.ActionType == "Keyboard" && settings.KeyboardMode == "Hold" && settings.KeyboardHoldDuration == TimeSpan.Zero)
                            {
                                if (!keyboardKeyHeld)
                                {
                                    if (settings.KeyboardPhysicalHoldMode)
                                        await SimulatePhysicalKeyboardHold(settings, token, stopwatch);
                                    else
                                        KeybdEvent((byte)KeyInterop.VirtualKeyFromKey(settings.KeyboardKey), 0);
                                    keyboardKeyHeld = true;
                                }
                            }
                            else
                            {
                                if (mouseButtonHeld)
                                {
                                    MouseEventUp(settings);
                                    mouseButtonHeld = false;
                                }
                                if (keyboardKeyHeld)
                                {
                                    KeybdEvent((byte)KeyInterop.VirtualKeyFromKey(settings.KeyboardKey), 2);
                                    keyboardKeyHeld = false;
                                }
                                await PerformGlobalAction(settings, stopwatch, token);
                            }
                        }

                        // Update remaining times
                        if (settings.ActionType == "Mouse" && settings.MouseMode == "Click" && settings.ClickMode == "Duration" && settings.ClickDuration != TimeSpan.Zero)
                        {
                            var remainingClickDuration = settings.ClickDuration - stopwatch.Elapsed;
                            viewModel.UpdateRemainingClickDuration(remainingClickDuration);
                            if (remainingClickDuration <= TimeSpan.Zero)
                            {
                                shouldBreak = true;
                            }
                        }
                        if (settings.ActionType == "Keyboard" && settings.KeyboardMode == "Press" && settings.Mode == "Timer" && stopwatch.Elapsed >= settings.TotalDuration)
                        {
                            var remainingPressTimer = settings.TotalDuration - stopwatch.Elapsed;
                            viewModel.UpdateRemainingPressTimer(remainingPressTimer);
                            if (remainingPressTimer <= TimeSpan.Zero)
                            {
                                shouldBreak = true;
                            }
                        }

                        var cycleElapsedTime = stopwatch.Elapsed - cycleStartTime;
                        var remainingCycleTime = effectiveInterval - cycleElapsedTime;
                        if (remainingCycleTime.TotalMilliseconds > 0)
                        {
                            await Task.Delay(remainingCycleTime, token);
                        }
                    }

                    if (mouseButtonHeld)
                    {
                        MouseEventUp(settings);
                        mouseButtonHeld = false;
                    }
                    if (keyboardKeyHeld)
                    {
                        KeybdEvent((byte)KeyInterop.VirtualKeyFromKey(settings.KeyboardKey), 2);
                        keyboardKeyHeld = false;
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
                finally
                {
                    lock (automationLock)
                    {
                        isRunning = false;
                        taskCompletionSource?.SetResult(true);
                    }
                }
            }, token);

            await currentAutomationTask;
        }

        public void StopAutomation(string message = "Not running")
        {
            Console.WriteLine($"StopAutomation called with message: {message}");
            lock (automationLock)
            {
                if (cts != null)
                {
                    cts.Cancel();
                    cts.Dispose();
                    cts = null;
                }

                if (mouseButtonHeld)
                {
                    MouseEventUp(viewModel.CurrentSettings ?? new Settings { MouseButton = "Left" });
                    mouseButtonHeld = false;
                }
                if (keyboardKeyHeld)
                {
                    KeybdEvent((byte)KeyInterop.VirtualKeyFromKey((Key)Properties.Settings.Default.KeyboardKey), 2);
                    keyboardKeyHeld = false;
                }

                viewModel?.StopTimers();
                viewModel?.UpdateStatus(message, "Red");
                isRunning = false;
                taskCompletionSource?.SetResult(true);
                taskCompletionSource = null;
            }
        }

        private bool ValidateSettings()
        {
            var settings = viewModel.CurrentSettings;
            if (settings == null)
            {
                Console.WriteLine("Settings object is null.");
                return false;
            }

            // Validate and set defaults for all properties
            if (string.IsNullOrEmpty(settings.ActionType))
            {
                Console.WriteLine("ActionType is null or empty. Setting to default 'Mouse'.");
                settings.ActionType = "Mouse";
            }
            if (string.IsNullOrEmpty(settings.MouseButton))
            {
                Console.WriteLine("MouseButton is null or empty. Setting to default 'Left'.");
                settings.MouseButton = "Left";
            }
            if (string.IsNullOrEmpty(settings.ClickType))
            {
                Console.WriteLine("ClickType is null or empty. Setting to default 'Single'.");
                settings.ClickType = "Single";
            }
            if (string.IsNullOrEmpty(settings.MouseMode))
            {
                Console.WriteLine("MouseMode is null or empty. Setting to default 'Click'.");
                settings.MouseMode = "Click";
            }
            if (string.IsNullOrEmpty(settings.ClickMode))
            {
                Console.WriteLine("ClickMode is null or empty. Setting to default 'Constant'.");
                settings.ClickMode = "Constant";
            }
            if (string.IsNullOrEmpty(settings.HoldMode))
            {
                Console.WriteLine("HoldMode is null or empty. Setting to default 'ConstantHold'.");
                settings.HoldMode = "ConstantHold";
            }
            if (string.IsNullOrEmpty(settings.KeyboardMode))
            {
                Console.WriteLine("KeyboardMode is null or empty. Setting to default 'Press'.");
                settings.KeyboardMode = "Press";
            }
            if (string.IsNullOrEmpty(settings.Mode))
            {
                Console.WriteLine("Mode is null or empty. Setting to default 'Constant'.");
                settings.Mode = "Constant";
            }
            if (settings.MouseHoldDuration == TimeSpan.Zero)
            {
                Console.WriteLine("MouseHoldDuration is zero. Setting to default '1 second'.");
                settings.MouseHoldDuration = TimeSpan.FromSeconds(1);
            }

            return true;
        }

        private bool ValidateTargetApplication(Settings settings)
        {
            var process = detector.GetProcessByName(settings.TargetApplication);
            bool isValid = process != null && !IsIconic(process.MainWindowHandle);
            Console.WriteLine($"ValidateTargetApplication: Target={settings.TargetApplication}, IsValid={isValid}");
            return isValid;
        }

        private async Task PerformGlobalAction(Settings settings, Stopwatch stopwatch, CancellationToken token)
        {
            Console.WriteLine("PerformGlobalAction called.");
            if (settings == null)
            {
                Console.WriteLine("settings is null in PerformGlobalAction. Stopping automation.");
                StopAutomation("Error: Settings is null");
                return;
            }

            if (settings.ActionType == "Mouse")
            {
                if (settings.MouseMode == "Click")
                {
                    Console.WriteLine("Performing mouse click.");
                    MouseEventDown(settings);
                    await Task.Delay(10, token);
                    MouseEventUp(settings);
                    if (settings.ClickType == "Double")
                    {
                        Console.WriteLine("Performing double click.");
                        await Task.Delay(50, token);
                        MouseEventDown(settings);
                        await Task.Delay(10, token);
                        MouseEventUp(settings);
                    }
                }
                else if (settings.HoldMode == "HoldDuration")
                {
                    Console.WriteLine($"Holding mouse for {settings.MouseHoldDuration.TotalMilliseconds} ms.");
                    if (settings.MousePhysicalHoldMode)
                    {
                        await SimulatePhysicalMouseHold(settings, token, stopwatch, settings.MouseHoldDuration);
                    }
                    else
                    {
                        TimeSpan holdDuration = settings.MouseHoldDuration.TotalMilliseconds < 500 ? TimeSpan.FromMilliseconds(500) : settings.MouseHoldDuration;
                        MouseEventDown(settings);
                        var holdStartTime = stopwatch.Elapsed;
                        while (stopwatch.Elapsed - holdStartTime < holdDuration && !token.IsCancellationRequested)
                        {
                            var remainingMouseHoldDuration = holdDuration - (stopwatch.Elapsed - holdStartTime);
                            if (viewModel != null)
                            {
                                viewModel.UpdateRemainingMouseHoldDuration(remainingMouseHoldDuration);
                            }
                            else
                            {
                                Console.WriteLine("viewModel is null in PerformGlobalAction (Mouse Hold). Stopping automation.");
                                StopAutomation("Error: ViewModel is null");
                                break;
                            }
                            await Task.Delay(1, token);
                        }
                        MouseEventUp(settings);
                    }
                }
            }
            else
            {
                if (settings.KeyboardMode == "Press")
                {
                    Console.WriteLine("Performing keyboard press.");
                    KeybdEvent((byte)KeyInterop.VirtualKeyFromKey(settings.KeyboardKey), 0);
                    await Task.Delay(50, token);
                    KeybdEvent((byte)KeyInterop.VirtualKeyFromKey(settings.KeyboardKey), 2);
                }
                else if (settings.KeyboardHoldDuration != TimeSpan.Zero)
                {
                    Console.WriteLine($"Holding keyboard for {settings.KeyboardHoldDuration.TotalMilliseconds} ms.");
                    if (settings.KeyboardPhysicalHoldMode)
                    {
                        await SimulatePhysicalKeyboardHold(settings, token, stopwatch, settings.KeyboardHoldDuration);
                    }
                    else
                    {
                        TimeSpan holdDuration = settings.KeyboardHoldDuration.TotalMilliseconds < 500 ? TimeSpan.FromMilliseconds(500) : settings.KeyboardHoldDuration;
                        KeybdEvent((byte)KeyInterop.VirtualKeyFromKey(settings.KeyboardKey), 0);
                        var holdStartTime = stopwatch.Elapsed;
                        while (stopwatch.Elapsed - holdStartTime < holdDuration && !token.IsCancellationRequested)
                        {
                            var remainingHoldDuration = holdDuration - (stopwatch.Elapsed - holdStartTime);
                            if (viewModel != null)
                            {
                                viewModel.UpdateRemainingHoldDuration(remainingHoldDuration);
                            }
                            else
                            {
                                Console.WriteLine("viewModel is null in PerformGlobalAction (Keyboard Hold). Stopping automation.");
                                StopAutomation("Error: ViewModel is null");
                                break;
                            }
                            await Task.Delay(1, token);
                        }
                        KeybdEvent((byte)KeyInterop.VirtualKeyFromKey(settings.KeyboardKey), 2);
                    }
                }
            }
        }

        private async Task PerformRestrictedAction(Settings settings, Stopwatch stopwatch, CancellationToken token)
        {
            Console.WriteLine("PerformRestrictedAction called.");
            if (settings == null)
            {
                Console.WriteLine("settings is null in PerformRestrictedAction. Stopping automation.");
                StopAutomation("Error: Settings is null");
                return;
            }

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
                if (settings.MouseMode == "Click")
                {
                    Console.WriteLine("Performing restricted mouse click.");
                    MouseEventDown(settings);
                    await Task.Delay(10, token);
                    MouseEventUp(settings);
                    if (settings.ClickType == "Double")
                    {
                        Console.WriteLine("Performing restricted double click.");
                        await Task.Delay(50, token);
                        MouseEventDown(settings);
                        await Task.Delay(10, token);
                        MouseEventUp(settings);
                    }
                }
                else if (settings.HoldMode == "HoldDuration")
                {
                    Console.WriteLine($"Holding mouse (restricted) for {settings.MouseHoldDuration.TotalMilliseconds} ms.");
                    if (settings.MousePhysicalHoldMode)
                    {
                        await SimulatePhysicalMouseHold(settings, token, stopwatch, settings.MouseHoldDuration);
                    }
                    else
                    {
                        TimeSpan holdDuration = settings.MouseHoldDuration.TotalMilliseconds < 500 ? TimeSpan.FromMilliseconds(500) : settings.MouseHoldDuration;
                        MouseEventDown(settings);
                        var holdStartTime = stopwatch.Elapsed;
                        while (stopwatch.Elapsed - holdStartTime < holdDuration && !token.IsCancellationRequested)
                        {
                            var remainingMouseHoldDuration = holdDuration - (stopwatch.Elapsed - holdStartTime);
                            if (viewModel != null)
                            {
                                viewModel.UpdateRemainingMouseHoldDuration(remainingMouseHoldDuration);
                            }
                            else
                            {
                                Console.WriteLine("viewModel is null in PerformRestrictedAction (Mouse Hold). Stopping automation.");
                                StopAutomation("Error: ViewModel is null");
                                break;
                            }
                            await Task.Delay(1, token);
                        }
                        MouseEventUp(settings);
                    }
                }
                else if (settings.HoldMode == "ConstantHold")
                {
                    if (settings.MousePhysicalHoldMode)
                    {
                        await SimulatePhysicalMouseHold(settings, token, stopwatch);
                    }
                    else
                    {
                        MouseEventDown(settings);
                    }
                }
            }
            else
            {
                if (settings.KeyboardMode == "Press")
                {
                    Console.WriteLine("Performing restricted keyboard press.");
                    KeybdEvent((byte)KeyInterop.VirtualKeyFromKey(settings.KeyboardKey), 0);
                    await Task.Delay(50, token);
                    KeybdEvent((byte)KeyInterop.VirtualKeyFromKey(settings.KeyboardKey), 2);
                }
                else if (settings.KeyboardHoldDuration != TimeSpan.Zero)
                {
                    Console.WriteLine($"Holding keyboard (restricted) for {settings.KeyboardHoldDuration.TotalMilliseconds} ms.");
                    if (settings.KeyboardPhysicalHoldMode)
                    {
                        await SimulatePhysicalKeyboardHold(settings, token, stopwatch, settings.KeyboardHoldDuration);
                    }
                    else
                    {
                        TimeSpan holdDuration = settings.KeyboardHoldDuration.TotalMilliseconds < 500 ? TimeSpan.FromMilliseconds(500) : settings.KeyboardHoldDuration;
                        KeybdEvent((byte)KeyInterop.VirtualKeyFromKey(settings.KeyboardKey), 0);
                        var holdStartTime = stopwatch.Elapsed;
                        while (stopwatch.Elapsed - holdStartTime < holdDuration && !token.IsCancellationRequested)
                        {
                            var remainingHoldDuration = holdDuration - (stopwatch.Elapsed - holdStartTime);
                            if (viewModel != null)
                            {
                                viewModel.UpdateRemainingHoldDuration(remainingHoldDuration);
                            }
                            else
                            {
                                Console.WriteLine("viewModel is null in PerformRestrictedAction (Keyboard Hold). Stopping automation.");
                                StopAutomation("Error: ViewModel is null");
                                break;
                            }
                            await Task.Delay(1, token);
                        }
                        KeybdEvent((byte)KeyInterop.VirtualKeyFromKey(settings.KeyboardKey), 2);
                    }
                }
                else
                {
                    if (settings.KeyboardPhysicalHoldMode)
                    {
                        await SimulatePhysicalKeyboardHold(settings, token, stopwatch);
                    }
                    else
                    {
                        KeybdEvent((byte)KeyInterop.VirtualKeyFromKey(settings.KeyboardKey), 0);
                    }
                }
            }
        }

        private async Task SimulatePhysicalMouseHold(Settings settings, CancellationToken cancellationToken, Stopwatch stopwatch, TimeSpan? duration = null)
        {
            Console.WriteLine("SimulatePhysicalMouseHold called.");
            if (settings == null)
            {
                Console.WriteLine("settings is null in SimulatePhysicalMouseHold. Stopping automation.");
                StopAutomation("Error: Settings is null");
                return;
            }

            var startTime = stopwatch.Elapsed;
            TimeSpan holdDuration = duration ?? TimeSpan.MaxValue;

            while (!cancellationToken.IsCancellationRequested && (stopwatch.Elapsed - startTime) < holdDuration)
            {
                MouseEventDown(settings);
                if (duration.HasValue)
                {
                    var remainingMouseHoldDuration = holdDuration - (stopwatch.Elapsed - startTime);
                    if (viewModel != null)
                    {
                        viewModel.UpdateRemainingMouseHoldDuration(remainingMouseHoldDuration);
                    }
                    else
                    {
                        Console.WriteLine("viewModel is null in SimulatePhysicalMouseHold. Stopping automation.");
                        StopAutomation("Error: ViewModel is null");
                        break;
                    }
                }
                await Task.Delay(50, cancellationToken);
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                MouseEventUp(settings);
            }
        }

        private async Task SimulatePhysicalKeyboardHold(Settings settings, CancellationToken cancellationToken, Stopwatch stopwatch, TimeSpan? duration = null)
        {
            Console.WriteLine("SimulatePhysicalKeyboardHold called.");
            if (settings == null)
            {
                Console.WriteLine("settings is null in SimulatePhysicalKeyboardHold. Stopping automation.");
                StopAutomation("Error: Settings is null");
                return;
            }

            var startTime = stopwatch.Elapsed;
            TimeSpan holdDuration = duration ?? TimeSpan.MaxValue;

            while (!cancellationToken.IsCancellationRequested && (stopwatch.Elapsed - startTime) < holdDuration)
            {
                KeybdEvent((byte)KeyInterop.VirtualKeyFromKey(settings.KeyboardKey), 0);
                if (duration.HasValue)
                {
                    var remainingHoldDuration = holdDuration - (stopwatch.Elapsed - startTime);
                    if (viewModel != null)
                    {
                        viewModel.UpdateRemainingHoldDuration(remainingHoldDuration);
                    }
                    else
                    {
                        Console.WriteLine("viewModel is null in SimulatePhysicalKeyboardHold. Stopping automation.");
                        StopAutomation("Error: ViewModel is null");
                        break;
                    }
                }
                await Task.Delay(50, cancellationToken);
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                KeybdEvent((byte)KeyInterop.VirtualKeyFromKey(settings.KeyboardKey), 2);
            }
        }

        private void MouseEventDown(Settings settings)
        {
            if (settings == null)
            {
                Console.WriteLine("settings is null in MouseEventDown. Cannot proceed.");
                return;
            }

            if (string.IsNullOrEmpty(settings.MouseButton))
            {
                Console.WriteLine("MouseButton is null or empty in MouseEventDown. Using default 'Left'.");
                settings.MouseButton = "Left";
            }

            Console.WriteLine($"MouseEventDown called for {settings.MouseButton}.");
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
                default:
                    Console.WriteLine($"Invalid MouseButton value '{settings.MouseButton}' in MouseEventDown. Using default 'Left'.");
                    inputs[0].u.mi.dwFlags = MOUSEEVENTF_LEFTDOWN;
                    break;
            }

            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private void MouseEventUp(Settings settings)
        {
            if (settings == null)
            {
                Console.WriteLine("settings is null in MouseEventUp. Cannot proceed.");
                return;
            }

            if (string.IsNullOrEmpty(settings.MouseButton))
            {
                Console.WriteLine("MouseButton is null or empty in MouseEventUp. Using default 'Left'.");
                settings.MouseButton = "Left";
            }

            Console.WriteLine($"MouseEventUp called for {settings.MouseButton}.");
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
                default:
                    Console.WriteLine($"Invalid MouseButton value '{settings.MouseButton}' in MouseEventUp. Using default 'Left'.");
                    inputs[0].u.mi.dwFlags = MOUSEEVENTF_LEFTUP;
                    break;
            }

            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private void KeybdEvent(byte key, uint flags)
        {
            Console.WriteLine($"KeybdEvent called: Key={key}, Flags={flags}.");
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