using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using AutoClacker.Models;
using AutoClacker.Controllers;
using AutoClacker.Utilities;
using System.Threading.Tasks;
using System.Threading;

namespace AutoClacker.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly Settings settings;
        private AutomationController automationController;
        private readonly ApplicationDetector applicationDetector;
        private HotkeyManager hotkeyManager;
        private bool isRunning;
        private string statusText = "Not running";
        private string statusColor = "Red";
        private Key capturedKey;
        private bool isSettingToggleKey;
        private bool isSettingKeyboardKey;
        private List<string> runningApplications;
        private List<string> mouseButtonOptions;
        private List<string> clickTypeOptions;
        private Window window;
        private Task currentAutomationTask;
        private readonly object toggleLock = new object(); // Prevent concurrent ToggleAutomation calls

        // Single timer for UI updates (100ms interval)
        private readonly DispatcherTimer uiUpdateTimer;

        // Remaining times (updated by AutomationController)
        private TimeSpan remainingClickDuration;
        private TimeSpan remainingPressTimer;
        private TimeSpan remainingHoldDuration;
        private TimeSpan remainingMouseHoldDuration;

        // Remaining time properties for display (in numbers)
        private int remainingClickDurationMin;
        private int remainingClickDurationSec;
        private int remainingClickDurationMs;
        private int remainingPressTimerMin;
        private int remainingPressTimerSec;
        private int remainingPressTimerMs;
        private int remainingHoldDurationMin;
        private int remainingHoldDurationSec;
        private int remainingHoldDurationMs;
        private int remainingMouseHoldDurationMin;
        private int remainingMouseHoldDurationSec;
        private int remainingMouseHoldDurationMs;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainViewModel()
        {
            settings = new Settings
            {
                ClickScope = Properties.Settings.Default.ClickScope ?? "Global",
                TargetApplication = Properties.Settings.Default.TargetApplication ?? "",
                ActionType = Properties.Settings.Default.ActionType ?? "Mouse",
                MouseButton = Properties.Settings.Default.MouseButton ?? "Left",
                ClickType = Properties.Settings.Default.ClickType ?? "Single",
                MouseMode = Properties.Settings.Default.MouseMode ?? "Click",
                ClickMode = Properties.Settings.Default.ClickMode ?? "Constant",
                ClickDuration = Properties.Settings.Default.ClickDuration,
                MouseHoldDuration = Properties.Settings.Default.MouseHoldDuration,
                HoldMode = Properties.Settings.Default.HoldMode ?? "ConstantHold",
                MousePhysicalHoldMode = Properties.Settings.Default.MousePhysicalHoldMode,
                KeyboardKey = (Key)Properties.Settings.Default.KeyboardKey,
                KeyboardMode = Properties.Settings.Default.KeyboardMode ?? "Press",
                KeyboardHoldDuration = Properties.Settings.Default.KeyboardHoldDuration,
                KeyboardPhysicalHoldMode = Properties.Settings.Default.KeyboardPhysicalHoldMode,
                TriggerKey = (Key)Properties.Settings.Default.TriggerKey,
                TriggerKeyModifiers = (ModifierKeys)Properties.Settings.Default.TriggerKeyModifiers,
                Interval = Properties.Settings.Default.Interval,
                Mode = Properties.Settings.Default.Mode ?? "Constant",
                TotalDuration = Properties.Settings.Default.TotalDuration,
                Theme = Properties.Settings.Default.Theme ?? "Light",
                IsTopmost = Properties.Settings.Default.IsTopmost
            };

            automationController = new AutomationController(this);
            applicationDetector = new ApplicationDetector();
            RunningApplications = applicationDetector.GetRunningApplications();
            MouseButtonOptions = new List<string> { "Left", "Right", "Middle" };
            ClickTypeOptions = new List<string> { "Single", "Double" };
            ToggleAutomationCommand = new RelayCommand(async o => await ToggleAutomation(o));
            SetTriggerKeyCommand = new RelayCommand(async o => await SetTriggerKey(o));
            SetKeyCommand = new RelayCommand(async o => await SetKey(o));
            ResetSettingsCommand = new RelayCommand(ResetSettings);
            SetConstantCommand = new RelayCommand(SetConstant);
            SetHoldDurationCommand = new RelayCommand(SetHoldDuration);
            RefreshApplicationsCommand = new RelayCommand(RefreshApplications);

            // Initialize UI update timer with 100ms interval
            uiUpdateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            uiUpdateTimer.Tick += (s, e) => UpdateRemainingTimesDisplay();

            OnPropertyChanged(nameof(TriggerKeyDisplay));
            OnPropertyChanged(nameof(KeyboardKeyDisplay));
            OnPropertyChanged(nameof(IsMouseMode));
            OnPropertyChanged(nameof(IsKeyboardMode));
            OnPropertyChanged(nameof(IsClickModeVisible));
            OnPropertyChanged(nameof(IsHoldModeVisible));
            OnPropertyChanged(nameof(IsPressModeVisible));
            OnPropertyChanged(nameof(IsHoldModeVisibleKeyboard));
            OnPropertyChanged(nameof(IsClickDurationMode));
            OnPropertyChanged(nameof(IsHoldDurationMode));
            OnPropertyChanged(nameof(IsKeyboardHoldDurationMode));
            OnPropertyChanged(nameof(IsTimerMode));
            OnPropertyChanged(nameof(IsRestrictedMode));
            Console.WriteLine($"Initial MouseMode: {MouseMode}, ClickMode: {ClickMode}, HoldMode: {HoldMode}, KeyboardMode: {KeyboardMode}");
        }

        public MainViewModel(Window window) : this()
        {
            this.window = window;
        }

        public void InitializeHotkeyManager(Window window)
        {
            this.window = window;
            hotkeyManager = new HotkeyManager(window, this);
            hotkeyManager.RegisterTriggerHotkey(settings.TriggerKey, settings.TriggerKeyModifiers);
            window.Topmost = settings.IsTopmost; // Set initial Topmost state
        }

        // Methods to update remaining times (called by AutomationController)
        public async void UpdateRemainingClickDuration(TimeSpan remaining)
        {
            remainingClickDuration = remaining;
            if (remaining <= TimeSpan.Zero)
            {
                remainingClickDuration = TimeSpan.Zero;
                await Task.Delay(100); // Allow UI to update before stopping
                automationController?.StopAutomation("Click Duration completed");
            }
        }

        public async void UpdateRemainingPressTimer(TimeSpan remaining)
        {
            remainingPressTimer = remaining;
            if (remaining <= TimeSpan.Zero)
            {
                remainingPressTimer = TimeSpan.Zero;
                await Task.Delay(100); // Allow UI to update before stopping
                automationController?.StopAutomation("Press Timer completed");
            }
        }

        public async void UpdateRemainingHoldDuration(TimeSpan remaining)
        {
            remainingHoldDuration = remaining;
            if (remaining <= TimeSpan.Zero)
            {
                remainingHoldDuration = TimeSpan.Zero;
                await Task.Delay(100); // Allow UI to update before stopping
                automationController?.StopAutomation("Hold Duration completed");
            }
            UpdateRemainingTimesDisplay(); // Force UI refresh
        }

        public async void UpdateRemainingMouseHoldDuration(TimeSpan remaining)
        {
            remainingMouseHoldDuration = remaining;
            if (remaining <= TimeSpan.Zero)
            {
                remainingMouseHoldDuration = TimeSpan.Zero;
                await Task.Delay(100); // Allow UI to update before stopping
                automationController?.StopAutomation("Mouse Hold Duration completed");
            }
            UpdateRemainingTimesDisplay(); // Force UI refresh
        }

        // UI update method (called every 100ms)
        private void UpdateRemainingTimesDisplay()
        {
            RemainingClickDurationMin = remainingClickDuration.Minutes;
            RemainingClickDurationSec = remainingClickDuration.Seconds;
            RemainingClickDurationMs = remainingClickDuration.Milliseconds;

            RemainingPressTimerMin = remainingPressTimer.Minutes;
            RemainingPressTimerSec = remainingPressTimer.Seconds;
            RemainingPressTimerMs = remainingPressTimer.Milliseconds;

            RemainingHoldDurationMin = remainingHoldDuration.Minutes;
            RemainingHoldDurationSec = remainingHoldDuration.Seconds;
            RemainingHoldDurationMs = remainingHoldDuration.Milliseconds;

            RemainingMouseHoldDurationMin = remainingMouseHoldDuration.Minutes;
            RemainingMouseHoldDurationSec = remainingMouseHoldDuration.Seconds;
            RemainingMouseHoldDurationMs = remainingMouseHoldDuration.Milliseconds;

            // Force UI refresh for hold timers
            OnPropertyChanged(nameof(RemainingHoldDurationMin));
            OnPropertyChanged(nameof(RemainingHoldDurationSec));
            OnPropertyChanged(nameof(RemainingHoldDurationMs));
            OnPropertyChanged(nameof(RemainingMouseHoldDurationMin));
            OnPropertyChanged(nameof(RemainingMouseHoldDurationSec));
            OnPropertyChanged(nameof(RemainingMouseHoldDurationMs));
        }

        // Remaining time properties for display
        public int RemainingClickDurationMin
        {
            get => remainingClickDurationMin;
            private set { remainingClickDurationMin = value; OnPropertyChanged(nameof(RemainingClickDurationMin)); }
        }

        public int RemainingClickDurationSec
        {
            get => remainingClickDurationSec;
            private set { remainingClickDurationSec = value; OnPropertyChanged(nameof(RemainingClickDurationSec)); }
        }

        public int RemainingClickDurationMs
        {
            get => remainingClickDurationMs;
            private set { remainingClickDurationMs = value; OnPropertyChanged(nameof(RemainingClickDurationMs)); }
        }

        public int RemainingPressTimerMin
        {
            get => remainingPressTimerMin;
            private set { remainingPressTimerMin = value; OnPropertyChanged(nameof(RemainingPressTimerMin)); }
        }

        public int RemainingPressTimerSec
        {
            get => remainingPressTimerSec;
            private set { remainingPressTimerSec = value; OnPropertyChanged(nameof(RemainingPressTimerSec)); }
        }

        public int RemainingPressTimerMs
        {
            get => remainingPressTimerMs;
            private set { remainingPressTimerMs = value; OnPropertyChanged(nameof(RemainingPressTimerMs)); }
        }

        public int RemainingHoldDurationMin
        {
            get => remainingHoldDurationMin;
            private set { remainingHoldDurationMin = value; OnPropertyChanged(nameof(RemainingHoldDurationMin)); }
        }

        public int RemainingHoldDurationSec
        {
            get => remainingHoldDurationSec;
            private set { remainingHoldDurationSec = value; OnPropertyChanged(nameof(RemainingHoldDurationSec)); }
        }

        public int RemainingHoldDurationMs
        {
            get => remainingHoldDurationMs;
            private set { remainingHoldDurationMs = value; OnPropertyChanged(nameof(RemainingHoldDurationMs)); }
        }

        public int RemainingMouseHoldDurationMin
        {
            get => remainingMouseHoldDurationMin;
            private set { remainingMouseHoldDurationMin = value; OnPropertyChanged(nameof(RemainingMouseHoldDurationMin)); }
        }

        public int RemainingMouseHoldDurationSec
        {
            get => remainingMouseHoldDurationSec;
            private set { remainingMouseHoldDurationSec = value; OnPropertyChanged(nameof(RemainingMouseHoldDurationSec)); }
        }

        public int RemainingMouseHoldDurationMs
        {
            get => remainingMouseHoldDurationMs;
            private set { remainingMouseHoldDurationMs = value; OnPropertyChanged(nameof(RemainingMouseHoldDurationMs)); }
        }

        public void StartTimers()
        {
            Console.WriteLine("StartTimers called.");
            // Initialize remaining times
            if (settings.ActionType == "Mouse" && settings.MouseMode == "Click" && settings.ClickMode == "Duration")
            {
                remainingClickDuration = settings.ClickDuration;
            }
            if (settings.ActionType == "Keyboard" && settings.KeyboardMode == "Press" && settings.Mode == "Timer")
            {
                remainingPressTimer = settings.TotalDuration;
            }
            if (settings.ActionType == "Keyboard" && settings.KeyboardMode == "Hold" && settings.KeyboardHoldDuration != TimeSpan.Zero)
            {
                remainingHoldDuration = settings.KeyboardHoldDuration;
            }
            if (settings.ActionType == "Mouse" && settings.MouseMode == "Hold" && settings.HoldMode == "HoldDuration")
            {
                remainingMouseHoldDuration = settings.MouseHoldDuration;
            }

            // Start the UI update timer
            uiUpdateTimer.Start();
            Console.WriteLine("UI update timer started.");
        }

        public void StopTimers()
        {
            Console.WriteLine("StopTimers called.");
            uiUpdateTimer.Stop();
            remainingClickDuration = TimeSpan.Zero;
            remainingPressTimer = TimeSpan.Zero;
            remainingHoldDuration = TimeSpan.Zero;
            remainingMouseHoldDuration = TimeSpan.Zero;

            RemainingClickDurationMin = 0;
            RemainingClickDurationSec = 0;
            RemainingClickDurationMs = 0;
            RemainingPressTimerMin = 0;
            RemainingPressTimerSec = 0;
            RemainingPressTimerMs = 0;
            RemainingHoldDurationMin = 0;
            RemainingHoldDurationSec = 0;
            RemainingHoldDurationMs = 0;
            RemainingMouseHoldDurationMin = 0;
            RemainingMouseHoldDurationSec = 0;
            RemainingMouseHoldDurationMs = 0;
        }

        public void UpdateStatus(string text, string color)
        {
            StatusText = text;
            StatusColor = color;
            IsRunning = text == "Running";
        }

        public Settings CurrentSettings => settings; // Expose settings for AutomationController

        public string ClickScope
        {
            get => settings.ClickScope;
            set
            {
                settings.ClickScope = value;
                OnPropertyChanged(nameof(ClickScope));
                OnPropertyChanged(nameof(IsRestrictedMode));
                Properties.Settings.Default.ClickScope = value;
                Properties.Settings.Default.Save();
            }
        }

        public string TargetApplication
        {
            get => settings.TargetApplication;
            set
            {
                settings.TargetApplication = value;
                OnPropertyChanged(nameof(TargetApplication));
                Properties.Settings.Default.TargetApplication = value;
                Properties.Settings.Default.Save();
            }
        }

        public string ActionType
        {
            get => settings.ActionType;
            set
            {
                settings.ActionType = value;
                OnPropertyChanged(nameof(ActionType));
                OnPropertyChanged(nameof(IsMouseMode));
                OnPropertyChanged(nameof(IsKeyboardMode));
                Properties.Settings.Default.ActionType = value;
                Properties.Settings.Default.Save();
            }
        }

        public string MouseButton
        {
            get => settings.MouseButton;
            set
            {
                settings.MouseButton = value;
                OnPropertyChanged(nameof(MouseButton));
                Properties.Settings.Default.MouseButton = value;
                Properties.Settings.Default.Save();
            }
        }

        public List<string> MouseButtonOptions
        {
            get => mouseButtonOptions;
            private set { mouseButtonOptions = value; OnPropertyChanged(nameof(MouseButtonOptions)); }
        }

        public string ClickType
        {
            get => settings.ClickType;
            set
            {
                settings.ClickType = value;
                OnPropertyChanged(nameof(ClickType));
                Properties.Settings.Default.ClickType = value;
                Properties.Settings.Default.Save();
            }
        }

        public List<string> ClickTypeOptions
        {
            get => clickTypeOptions;
            private set { clickTypeOptions = value; OnPropertyChanged(nameof(ClickTypeOptions)); }
        }

        public string MouseMode
        {
            get => settings.MouseMode;
            set
            {
                settings.MouseMode = value;
                OnPropertyChanged(nameof(MouseMode));
                OnPropertyChanged(nameof(IsClickModeVisible));
                OnPropertyChanged(nameof(IsHoldModeVisible));
                OnPropertyChanged(nameof(IsClickDurationMode));
                OnPropertyChanged(nameof(IsHoldDurationMode));
                Properties.Settings.Default.MouseMode = value;
                Properties.Settings.Default.Save();
                Console.WriteLine($"MouseMode changed to: {value}, IsClickModeVisible: {IsClickModeVisible}, IsHoldModeVisible: {IsHoldModeVisible}");
            }
        }

        public string ClickMode
        {
            get => settings.ClickMode;
            set
            {
                settings.ClickMode = value;
                OnPropertyChanged(nameof(ClickMode));
                OnPropertyChanged(nameof(IsClickDurationMode));
                Properties.Settings.Default.ClickMode = value;
                Properties.Settings.Default.Save();
                Console.WriteLine($"ClickMode changed to: {value}, IsClickDurationMode: {IsClickDurationMode}");
            }
        }

        public int ClickDurationMinutes
        {
            get => settings.ClickDuration.Minutes;
            set
            {
                settings.ClickDuration = new TimeSpan(0, 0, value, settings.ClickDuration.Seconds, settings.ClickDuration.Milliseconds);
                OnPropertyChanged(nameof(ClickDurationMinutes));
                Properties.Settings.Default.ClickDuration = settings.ClickDuration;
                Properties.Settings.Default.Save();
            }
        }

        public int ClickDurationSeconds
        {
            get => settings.ClickDuration.Seconds;
            set
            {
                settings.ClickDuration = new TimeSpan(0, 0, settings.ClickDuration.Minutes, value, settings.ClickDuration.Milliseconds);
                OnPropertyChanged(nameof(ClickDurationSeconds));
                Properties.Settings.Default.ClickDuration = settings.ClickDuration;
                Properties.Settings.Default.Save();
            }
        }

        public int ClickDurationMilliseconds
        {
            get => settings.ClickDuration.Milliseconds;
            set
            {
                settings.ClickDuration = new TimeSpan(0, 0, settings.ClickDuration.Minutes, settings.ClickDuration.Seconds, value);
                OnPropertyChanged(nameof(ClickDurationMilliseconds));
                Properties.Settings.Default.ClickDuration = settings.ClickDuration;
                Properties.Settings.Default.Save();
            }
        }

        public int MouseHoldDurationMinutes
        {
            get => settings.MouseHoldDuration.Minutes;
            set
            {
                settings.MouseHoldDuration = new TimeSpan(0, 0, value, settings.MouseHoldDuration.Seconds, settings.MouseHoldDuration.Milliseconds);
                OnPropertyChanged(nameof(MouseHoldDurationMinutes));
                Properties.Settings.Default.MouseHoldDuration = settings.MouseHoldDuration;
                Properties.Settings.Default.Save();
            }
        }

        public int MouseHoldDurationSeconds
        {
            get => settings.MouseHoldDuration.Seconds;
            set
            {
                settings.MouseHoldDuration = new TimeSpan(0, 0, settings.MouseHoldDuration.Minutes, value, settings.MouseHoldDuration.Milliseconds);
                OnPropertyChanged(nameof(MouseHoldDurationSeconds));
                Properties.Settings.Default.MouseHoldDuration = settings.MouseHoldDuration;
                Properties.Settings.Default.Save();
            }
        }

        public int MouseHoldDurationMilliseconds
        {
            get => settings.MouseHoldDuration.Milliseconds;
            set
            {
                settings.MouseHoldDuration = new TimeSpan(0, 0, settings.MouseHoldDuration.Minutes, settings.MouseHoldDuration.Seconds, value);
                OnPropertyChanged(nameof(MouseHoldDurationMilliseconds));
                Properties.Settings.Default.MouseHoldDuration = settings.MouseHoldDuration;
                Properties.Settings.Default.Save();
            }
        }

        public string HoldMode
        {
            get => settings.HoldMode;
            set
            {
                settings.HoldMode = value;
                OnPropertyChanged(nameof(HoldMode));
                OnPropertyChanged(nameof(IsHoldDurationMode));
                Properties.Settings.Default.HoldMode = value;
                Properties.Settings.Default.Save();
                Console.WriteLine($"HoldMode changed to: {value}, IsHoldDurationMode: {IsHoldDurationMode}");
            }
        }

        public bool MousePhysicalHoldMode
        {
            get => settings.MousePhysicalHoldMode;
            set
            {
                settings.MousePhysicalHoldMode = value;
                OnPropertyChanged(nameof(MousePhysicalHoldMode));
                Properties.Settings.Default.MousePhysicalHoldMode = value;
                Properties.Settings.Default.Save();
            }
        }

        public Key KeyboardKey
        {
            get => settings.KeyboardKey;
            set
            {
                settings.KeyboardKey = value;
                OnPropertyChanged(nameof(KeyboardKey));
                OnPropertyChanged(nameof(KeyboardKeyDisplay));
                Properties.Settings.Default.KeyboardKey = (int)value;
                Properties.Settings.Default.Save();
            }
        }

        public string KeyboardKeyDisplay => settings.KeyboardKey.ToString();

        public string KeyboardMode
        {
            get => settings.KeyboardMode;
            set
            {
                settings.KeyboardMode = value;
                OnPropertyChanged(nameof(KeyboardMode));
                OnPropertyChanged(nameof(IsPressModeVisible));
                OnPropertyChanged(nameof(IsHoldModeVisibleKeyboard));
                OnPropertyChanged(nameof(IsKeyboardHoldDurationMode));
                Properties.Settings.Default.KeyboardMode = value;
                Properties.Settings.Default.Save();
                Console.WriteLine($"KeyboardMode changed to: {value}, IsPressModeVisible: {IsPressModeVisible}, IsHoldModeVisibleKeyboard: {IsHoldModeVisibleKeyboard}");
            }
        }

        public int KeyboardHoldDurationMinutes
        {
            get => settings.KeyboardHoldDuration.Minutes;
            set
            {
                settings.KeyboardHoldDuration = new TimeSpan(0, 0, value, settings.KeyboardHoldDuration.Seconds, settings.KeyboardHoldDuration.Milliseconds);
                OnPropertyChanged(nameof(KeyboardHoldDurationMinutes));
                OnPropertyChanged(nameof(IsKeyboardHoldDurationMode));
                Properties.Settings.Default.KeyboardHoldDuration = settings.KeyboardHoldDuration;
                Properties.Settings.Default.Save();
            }
        }

        public int KeyboardHoldDurationSeconds
        {
            get => settings.KeyboardHoldDuration.Seconds;
            set
            {
                settings.KeyboardHoldDuration = new TimeSpan(0, 0, settings.KeyboardHoldDuration.Minutes, value, settings.KeyboardHoldDuration.Milliseconds);
                OnPropertyChanged(nameof(KeyboardHoldDurationSeconds));
                OnPropertyChanged(nameof(IsKeyboardHoldDurationMode));
                Properties.Settings.Default.KeyboardHoldDuration = settings.KeyboardHoldDuration;
                Properties.Settings.Default.Save();
            }
        }

        public int KeyboardHoldDurationMilliseconds
        {
            get => settings.KeyboardHoldDuration.Milliseconds;
            set
            {
                settings.KeyboardHoldDuration = new TimeSpan(0, 0, settings.KeyboardHoldDuration.Minutes, settings.KeyboardHoldDuration.Seconds, value);
                OnPropertyChanged(nameof(KeyboardHoldDurationMilliseconds));
                OnPropertyChanged(nameof(IsKeyboardHoldDurationMode));
                Properties.Settings.Default.KeyboardHoldDuration = settings.KeyboardHoldDuration;
                Properties.Settings.Default.Save();
            }
        }

        public bool KeyboardPhysicalHoldMode
        {
            get => settings.KeyboardPhysicalHoldMode;
            set
            {
                settings.KeyboardPhysicalHoldMode = value;
                OnPropertyChanged(nameof(KeyboardPhysicalHoldMode));
                Properties.Settings.Default.KeyboardPhysicalHoldMode = value;
                Properties.Settings.Default.Save();
            }
        }

        public Key TriggerKey
        {
            get => settings.TriggerKey;
            set
            {
                settings.TriggerKey = value;
                OnPropertyChanged(nameof(TriggerKey));
                OnPropertyChanged(nameof(TriggerKeyDisplay));
                Properties.Settings.Default.TriggerKey = (int)value;
                Properties.Settings.Default.Save();
                hotkeyManager?.RegisterTriggerHotkey(value, settings.TriggerKeyModifiers);
            }
        }

        public string TriggerKeyDisplay => settings.TriggerKey.ToString();

        public ModifierKeys TriggerKeyModifiers
        {
            get => settings.TriggerKeyModifiers;
            set
            {
                settings.TriggerKeyModifiers = value;
                OnPropertyChanged(nameof(TriggerKeyModifiers));
                Properties.Settings.Default.TriggerKeyModifiers = (int)value;
                Properties.Settings.Default.Save();
                hotkeyManager?.RegisterTriggerHotkey(settings.TriggerKey, value);
            }
        }

        public int IntervalMinutes
        {
            get => settings.Interval.Minutes;
            set
            {
                settings.Interval = new TimeSpan(0, 0, value, settings.Interval.Seconds, settings.Interval.Milliseconds);
                OnPropertyChanged(nameof(IntervalMinutes));
                Properties.Settings.Default.Interval = settings.Interval;
                Properties.Settings.Default.Save();
            }
        }

        public int IntervalSeconds
        {
            get => settings.Interval.Seconds;
            set
            {
                settings.Interval = new TimeSpan(0, 0, settings.Interval.Minutes, value, settings.Interval.Milliseconds);
                OnPropertyChanged(nameof(IntervalSeconds));
                Properties.Settings.Default.Interval = settings.Interval;
                Properties.Settings.Default.Save();
            }
        }

        public int IntervalMilliseconds
        {
            get => settings.Interval.Milliseconds;
            set
            {
                settings.Interval = new TimeSpan(0, 0, settings.Interval.Minutes, settings.Interval.Seconds, value);
                OnPropertyChanged(nameof(IntervalMilliseconds));
                Properties.Settings.Default.Interval = settings.Interval;
                Properties.Settings.Default.Save();
            }
        }

        public string Mode
        {
            get => settings.Mode;
            set
            {
                settings.Mode = value;
                OnPropertyChanged(nameof(Mode));
                OnPropertyChanged(nameof(IsTimerMode));
                Properties.Settings.Default.Mode = value;
                Properties.Settings.Default.Save();
            }
        }

        public int TotalDurationMinutes
        {
            get => settings.TotalDuration.Minutes;
            set
            {
                settings.TotalDuration = new TimeSpan(0, 0, value, settings.TotalDuration.Seconds, settings.TotalDuration.Milliseconds);
                OnPropertyChanged(nameof(TotalDurationMinutes));
                Properties.Settings.Default.TotalDuration = settings.TotalDuration;
                Properties.Settings.Default.Save();
            }
        }

        public int TotalDurationSeconds
        {
            get => settings.TotalDuration.Seconds;
            set
            {
                settings.TotalDuration = new TimeSpan(0, 0, settings.TotalDuration.Minutes, value, settings.TotalDuration.Milliseconds);
                OnPropertyChanged(nameof(TotalDurationSeconds));
                Properties.Settings.Default.TotalDuration = settings.TotalDuration;
                Properties.Settings.Default.Save();
            }
        }

        public int TotalDurationMilliseconds
        {
            get => settings.TotalDuration.Milliseconds;
            set
            {
                settings.TotalDuration = new TimeSpan(0, 0, settings.TotalDuration.Minutes, settings.TotalDuration.Seconds, value);
                OnPropertyChanged(nameof(TotalDurationMilliseconds));
                Properties.Settings.Default.TotalDuration = settings.TotalDuration;
                Properties.Settings.Default.Save();
            }
        }

        public string Theme
        {
            get => settings.Theme;
            set
            {
                settings.Theme = value;
                OnPropertyChanged(nameof(Theme));
                Properties.Settings.Default.Theme = value;
                Properties.Settings.Default.Save();
            }
        }

        public bool IsTopmost
        {
            get => settings.IsTopmost;
            set
            {
                settings.IsTopmost = value;
                OnPropertyChanged(nameof(IsTopmost));
                Properties.Settings.Default.IsTopmost = value;
                Properties.Settings.Default.Save();
                if (System.Windows.Application.Current.MainWindow != null)
                {
                    System.Windows.Application.Current.MainWindow.Topmost = value;
                }
            }
        }

        public List<string> RunningApplications
        {
            get => runningApplications;
            private set { runningApplications = value; OnPropertyChanged(nameof(RunningApplications)); }
        }

        public bool IsRunning
        {
            get => isRunning;
            private set { isRunning = value; OnPropertyChanged(nameof(IsRunning)); }
        }

        public string StatusText
        {
            get => statusText;
            set { statusText = value; OnPropertyChanged(nameof(StatusText)); }
        }

        public string StatusColor
        {
            get => statusColor;
            set { statusColor = value; OnPropertyChanged(nameof(StatusColor)); }
        }

        public bool IsMouseMode => ActionType == "Mouse";
        public bool IsKeyboardMode => ActionType == "Keyboard";
        public bool IsClickModeVisible => MouseMode == "Click";
        public bool IsHoldModeVisible => MouseMode == "Hold";
        public bool IsClickDurationMode => MouseMode == "Click" && ClickMode == "Duration";
        public bool IsHoldDurationMode => MouseMode == "Hold" && HoldMode == "HoldDuration";
        public bool IsPressModeVisible => KeyboardMode == "Press";
        public bool IsHoldModeVisibleKeyboard => KeyboardMode == "Hold";
        public bool IsKeyboardHoldDurationMode => KeyboardMode == "Hold" && settings.KeyboardHoldDuration != TimeSpan.Zero;
        public bool IsTimerMode => Mode == "Timer";
        public bool IsRestrictedMode => ClickScope == "Restricted";

        public RelayCommand ToggleAutomationCommand { get; }
        public RelayCommand SetTriggerKeyCommand { get; }
        public RelayCommand SetKeyCommand { get; }
        public RelayCommand ResetSettingsCommand { get; }
        public RelayCommand SetConstantCommand { get; }
        public RelayCommand SetHoldDurationCommand { get; }
        public RelayCommand RefreshApplicationsCommand { get; }

        private async Task ToggleAutomation(object _)
        {
            // Prevent concurrent ToggleAutomation calls
            if (!Monitor.TryEnter(toggleLock))
            {
                Console.WriteLine("ToggleAutomation already in progress, skipping.");
                return;
            }

            try
            {
                Console.WriteLine($"ToggleAutomation called. IsRunning: {IsRunning}, CurrentAutomationTask: {(currentAutomationTask != null ? "Running" : "Null")}");
                if (IsRunning)
                {
                    if (automationController != null)
                    {
                        automationController.StopAutomation();
                        if (currentAutomationTask != null)
                        {
                            try
                            {
                                await Task.Delay(100); // Async wait for task completion
                                await currentAutomationTask; // Wait for the automation to fully stop
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error waiting for automation to stop: {ex.Message}");
                            }
                        }
                        currentAutomationTask = null;
                    }
                }
                else
                {
                    if (currentAutomationTask != null)
                    {
                        Console.WriteLine("Previous automation task still running, waiting for it to stop.");
                        try
                        {
                            await Task.Delay(100); // Async wait for task completion
                            await currentAutomationTask;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error waiting for previous automation to stop: {ex.Message}");
                        }
                    }
                    if (automationController != null)
                    {
                        StartTimers(); // Ensure timers start before automation
                        currentAutomationTask = automationController.StartAutomation();
                        try
                        {
                            await currentAutomationTask; // Wait for StartAutomation to complete
                        }
                        catch (Exception ex)
                        {
                            System.Windows.MessageBox.Show($"Automation failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        finally
                        {
                            currentAutomationTask = null; // Clear the task reference when done
                        }
                    }
                    else
                    {
                        Console.WriteLine("AutomationController is null, cannot start automation.");
                    }
                }
            }
            finally
            {
                Monitor.Exit(toggleLock);
            }
        }

        private async Task SetTriggerKey(object _)
        {
            try
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    isSettingToggleKey = true;
                    isSettingKeyboardKey = false;
                    capturedKey = Key.None;
                    var dialog = new Window
                    {
                        Title = "Set Toggle Key",
                        Content = new System.Windows.Controls.TextBlock
                        {
                            Text = "Press a key to set the toggle key. Press Esc to cancel.",
                            Margin = new Thickness(10)
                        },
                        Width = 300,
                        Height = 100,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = System.Windows.Application.Current.MainWindow
                    };
                    dialog.Show();
                    dialog.KeyDown += (s, e) =>
                    {
                        if (e.Key == Key.Escape)
                        {
                            capturedKey = Key.None;
                            isSettingToggleKey = false;
                            dialog.Close();
                            return;
                        }
                        capturedKey = e.Key;
                        if (e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) || e.KeyboardDevice.IsKeyDown(Key.RightCtrl))
                            TriggerKeyModifiers |= ModifierKeys.Control;
                        if (e.KeyboardDevice.IsKeyDown(Key.LeftShift) || e.KeyboardDevice.IsKeyDown(Key.RightShift))
                            TriggerKeyModifiers |= ModifierKeys.Shift;
                        if (e.KeyboardDevice.IsKeyDown(Key.LeftAlt) || e.KeyboardDevice.IsKeyDown(Key.RightAlt))
                            TriggerKeyModifiers |= ModifierKeys.Alt;
                        TriggerKey = capturedKey;
                        isSettingToggleKey = false;
                        dialog.Close();
                    };
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SetTriggerKey: {ex.Message}");
                System.Windows.MessageBox.Show($"Failed to set toggle key: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SetKey(object _)
        {
            try
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    isSettingKeyboardKey = true;
                    isSettingToggleKey = false;
                    capturedKey = Key.None;
                    var dialog = new Window
                    {
                        Title = "Set Key",
                        Content = new System.Windows.Controls.TextBlock
                        {
                            Text = "Press a key to set the keyboard key. Press Esc to cancel.",
                            Margin = new Thickness(10)
                        },
                        Width = 300,
                        Height = 100,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = System.Windows.Application.Current.MainWindow
                    };
                    dialog.Show();
                    dialog.KeyDown += (s, e) =>
                    {
                        if (e.Key == Key.Escape)
                        {
                            capturedKey = Key.None;
                            isSettingKeyboardKey = false;
                            dialog.Close();
                            return;
                        }
                        capturedKey = e.Key;
                        KeyboardKey = capturedKey;
                        OnPropertyChanged(nameof(KeyboardKeyDisplay));
                        isSettingKeyboardKey = false;
                        dialog.Close();
                    };
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SetKey: {ex.Message}");
                System.Windows.MessageBox.Show($"Failed to set key: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetConstant(object _)
        {
            settings.KeyboardHoldDuration = TimeSpan.Zero;
            OnPropertyChanged(nameof(IsKeyboardHoldDurationMode));
            Properties.Settings.Default.KeyboardHoldDuration = settings.KeyboardHoldDuration;
            Properties.Settings.Default.Save();
        }

        private void SetHoldDuration(object _)
        {
            if (settings.KeyboardHoldDuration == TimeSpan.Zero)
            {
                settings.KeyboardHoldDuration = TimeSpan.FromSeconds(1);
                KeyboardHoldDurationMinutes = settings.KeyboardHoldDuration.Minutes;
                KeyboardHoldDurationSeconds = settings.KeyboardHoldDuration.Seconds;
                KeyboardHoldDurationMilliseconds = settings.KeyboardHoldDuration.Milliseconds;
            }
            OnPropertyChanged(nameof(IsKeyboardHoldDurationMode));
        }

        private void RefreshApplications(object _)
        {
            var apps = applicationDetector.GetRunningApplications();
            Console.WriteLine($"Found {apps.Count} running applications: {string.Join(", ", apps)}");
            RunningApplications = apps;
        }

        private void ResetSettings(object _)
        {
            // Stop any ongoing automation
            if (IsRunning)
            {
                automationController?.StopAutomation();
                if (currentAutomationTask != null)
                {
                    try
                    {
                        currentAutomationTask.Wait(); // Wait for the automation to fully stop
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error waiting for automation to stop during reset: {ex.Message}");
                    }
                }
                currentAutomationTask = null;
            }

            // Reset settings to default
            Properties.Settings.Default.Reset();

            // Explicitly set all settings to defaults to avoid nulls
            settings.ClickScope = "Global";
            settings.TargetApplication = "";
            settings.ActionType = "Mouse";
            settings.MouseButton = "Left";
            settings.ClickType = "Single";
            settings.MouseMode = "Click";
            settings.ClickMode = "Constant";
            settings.ClickDuration = TimeSpan.Zero;
            settings.MouseHoldDuration = TimeSpan.FromSeconds(1);
            settings.HoldMode = "ConstantHold";
            settings.MousePhysicalHoldMode = false;
            settings.KeyboardKey = Key.Space; // Default to a valid key
            settings.KeyboardMode = "Press";
            settings.KeyboardHoldDuration = TimeSpan.Zero;
            settings.KeyboardPhysicalHoldMode = false;
            settings.TriggerKey = Key.F5;
            settings.TriggerKeyModifiers = ModifierKeys.None;
            settings.Interval = TimeSpan.FromSeconds(2);
            settings.Mode = "Constant";
            settings.TotalDuration = TimeSpan.Zero;
            settings.Theme = "Light";
            settings.IsTopmost = false;

            // Persist all settings to Properties.Settings.Default
            Properties.Settings.Default.ClickScope = settings.ClickScope;
            Properties.Settings.Default.TargetApplication = settings.TargetApplication;
            Properties.Settings.Default.ActionType = settings.ActionType;
            Properties.Settings.Default.MouseButton = settings.MouseButton;
            Properties.Settings.Default.ClickType = settings.ClickType;
            Properties.Settings.Default.MouseMode = settings.MouseMode;
            Properties.Settings.Default.ClickMode = settings.ClickMode;
            Properties.Settings.Default.ClickDuration = settings.ClickDuration;
            Properties.Settings.Default.MouseHoldDuration = settings.MouseHoldDuration;
            Properties.Settings.Default.HoldMode = settings.HoldMode;
            Properties.Settings.Default.MousePhysicalHoldMode = settings.MousePhysicalHoldMode;
            Properties.Settings.Default.KeyboardKey = (int)settings.KeyboardKey;
            Properties.Settings.Default.KeyboardMode = settings.KeyboardMode;
            Properties.Settings.Default.KeyboardHoldDuration = settings.KeyboardHoldDuration;
            Properties.Settings.Default.KeyboardPhysicalHoldMode = settings.KeyboardPhysicalHoldMode;
            Properties.Settings.Default.TriggerKey = (int)settings.TriggerKey;
            Properties.Settings.Default.TriggerKeyModifiers = (int)settings.TriggerKeyModifiers;
            Properties.Settings.Default.Interval = settings.Interval;
            Properties.Settings.Default.Mode = settings.Mode;
            Properties.Settings.Default.TotalDuration = settings.TotalDuration;
            Properties.Settings.Default.Theme = settings.Theme;
            Properties.Settings.Default.IsTopmost = settings.IsTopmost;
            Properties.Settings.Default.Save();

            // Reinitialize HotkeyManager and AutomationController with default settings
            if (window != null)
            {
                hotkeyManager?.Dispose();
                hotkeyManager = new HotkeyManager(window, this);
                hotkeyManager.RegisterTriggerHotkey(settings.TriggerKey, settings.TriggerKeyModifiers);
            }

            // Reinitialize AutomationController to ensure it has the updated settings
            automationController = new AutomationController(this);

            // Notify UI of all property changes
            OnPropertyChanged(nameof(ClickScope));
            OnPropertyChanged(nameof(TargetApplication));
            OnPropertyChanged(nameof(ActionType));
            OnPropertyChanged(nameof(MouseButton));
            OnPropertyChanged(nameof(ClickType));
            OnPropertyChanged(nameof(MouseMode));
            OnPropertyChanged(nameof(ClickMode));
            OnPropertyChanged(nameof(ClickDurationMinutes));
            OnPropertyChanged(nameof(ClickDurationSeconds));
            OnPropertyChanged(nameof(ClickDurationMilliseconds));
            OnPropertyChanged(nameof(MouseHoldDurationMinutes));
            OnPropertyChanged(nameof(MouseHoldDurationSeconds));
            OnPropertyChanged(nameof(MouseHoldDurationMilliseconds));
            OnPropertyChanged(nameof(HoldMode));
            OnPropertyChanged(nameof(MousePhysicalHoldMode));
            OnPropertyChanged(nameof(KeyboardKey));
            OnPropertyChanged(nameof(KeyboardKeyDisplay));
            OnPropertyChanged(nameof(KeyboardMode));
            OnPropertyChanged(nameof(KeyboardHoldDurationMinutes));
            OnPropertyChanged(nameof(KeyboardHoldDurationSeconds));
            OnPropertyChanged(nameof(KeyboardHoldDurationMilliseconds));
            OnPropertyChanged(nameof(KeyboardPhysicalHoldMode));
            OnPropertyChanged(nameof(TriggerKey));
            OnPropertyChanged(nameof(TriggerKeyDisplay));
            OnPropertyChanged(nameof(TriggerKeyModifiers));
            OnPropertyChanged(nameof(IntervalMinutes));
            OnPropertyChanged(nameof(IntervalSeconds));
            OnPropertyChanged(nameof(IntervalMilliseconds));
            OnPropertyChanged(nameof(Mode));
            OnPropertyChanged(nameof(TotalDurationMinutes));
            OnPropertyChanged(nameof(TotalDurationSeconds));
            OnPropertyChanged(nameof(TotalDurationMilliseconds));
            OnPropertyChanged(nameof(Theme));
            OnPropertyChanged(nameof(IsTopmost));
            OnPropertyChanged(nameof(IsMouseMode));
            OnPropertyChanged(nameof(IsKeyboardMode));
            OnPropertyChanged(nameof(IsClickModeVisible));
            OnPropertyChanged(nameof(IsHoldModeVisible));
            OnPropertyChanged(nameof(IsClickDurationMode));
            OnPropertyChanged(nameof(IsHoldDurationMode));
            OnPropertyChanged(nameof(IsPressModeVisible));
            OnPropertyChanged(nameof(IsHoldModeVisibleKeyboard));
            OnPropertyChanged(nameof(IsKeyboardHoldDurationMode));
            OnPropertyChanged(nameof(IsTimerMode));
            OnPropertyChanged(nameof(IsRestrictedMode));

            Console.WriteLine("Settings reset to default, HotkeyManager and AutomationController reinitialized.");
        }

        public void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (e.OriginalSource is System.Windows.Controls.TextBox)
            {
                return;
            }

            if (isSettingToggleKey)
            {
                if (e.Key == Key.Escape)
                {
                    capturedKey = Key.None;
                    isSettingToggleKey = false;
                    return;
                }
                capturedKey = e.Key;
                if (e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) || e.KeyboardDevice.IsKeyDown(Key.RightCtrl))
                    TriggerKeyModifiers |= ModifierKeys.Control;
                if (e.KeyboardDevice.IsKeyDown(Key.LeftShift) || e.KeyboardDevice.IsKeyDown(Key.RightShift))
                    TriggerKeyModifiers |= ModifierKeys.Shift;
                if (e.KeyboardDevice.IsKeyDown(Key.LeftAlt) || e.KeyboardDevice.IsKeyDown(Key.RightAlt))
                    TriggerKeyModifiers |= ModifierKeys.Alt;
                TriggerKey = capturedKey;
                isSettingToggleKey = false;
            }
            else if (isSettingKeyboardKey)
            {
                if (e.Key == Key.Escape)
                {
                    capturedKey = Key.None;
                    isSettingKeyboardKey = false;
                    return;
                }
                capturedKey = e.Key;
                KeyboardKey = capturedKey;
                OnPropertyChanged(nameof(KeyboardKeyDisplay));
                isSettingKeyboardKey = false;
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Func<object, Task> executeAsync;
        private readonly Func<object, bool> canExecute;

        public RelayCommand(Func<object, Task> executeAsync, Func<object, bool> canExecute = null)
        {
            this.executeAsync = executeAsync;
            this.canExecute = canExecute;
        }

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
            : this(o => Task.Run(() => execute(o)), canExecute)
        {
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => canExecute == null || canExecute(parameter);

        public async void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                await executeAsync(parameter);
            }
        }
    }
}