using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AutoClacker.Models;
using AutoClacker.Controllers;
using AutoClacker.Utilities;
using System.Threading.Tasks;

namespace AutoClacker.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly Settings settings;
        private readonly AutomationController automationController;
        private HotkeyManager hotkeyManager;
        private readonly ApplicationDetector applicationDetector;
        private bool isRunning;
        private string statusText = "Not running";
        private string statusColor = "Red";
        private Key capturedKey;
        private bool isSettingToggleKey;
        private bool isSettingKeyboardKey;
        private List<string> runningApplications;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainViewModel()
        {
            settings = new Settings
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

            automationController = new AutomationController(this);
            applicationDetector = new ApplicationDetector();
            RunningApplications = applicationDetector.GetRunningApplications();
            ToggleAutomationCommand = new RelayCommand(ToggleAutomation);
            SetTriggerKeyCommand = new RelayCommand(SetTriggerKey);
            SetKeyCommand = new RelayCommand(SetKey);
            ResetSettingsCommand = new RelayCommand(ResetSettings);
            SetConstantCommand = new RelayCommand(SetConstant);
            SetHoldDurationCommand = new RelayCommand(SetHoldDuration);
            RefreshApplicationsCommand = new RelayCommand(RefreshApplications);

            OnPropertyChanged(nameof(TriggerKeyDisplay));
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
        }

        public void InitializeHotkeyManager(Window window)
        {
            hotkeyManager = new HotkeyManager(window, this);
            hotkeyManager.RegisterTriggerHotkey(settings.TriggerKey, settings.TriggerKeyModifiers);
        }

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

        public string KeyboardKeyDisplay => KeyboardKey == Key.None ? "None" : KeyboardKey.ToString();

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

        public string TriggerKeyDisplay => TriggerKey == Key.F5 ? "F5" : "Not Set";

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

        public ICommand ToggleAutomationCommand { get; }
        public ICommand SetTriggerKeyCommand { get; }
        public ICommand SetKeyCommand { get; }
        public ICommand ResetSettingsCommand { get; }
        public ICommand SetConstantCommand { get; }
        public ICommand SetHoldDurationCommand { get; }
        public ICommand RefreshApplicationsCommand { get; }

        private void ToggleAutomation(object parameter)
        {
            if (IsRunning)
            {
                automationController.StopAutomation();
            }
            else
            {
                var task = automationController.StartAutomation();
                task.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        MessageBox.Show($"Automation failed: {t.Exception?.InnerException?.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        private void SetTriggerKey(object parameter)
        {
            isSettingToggleKey = true;
            isSettingKeyboardKey = false;
            capturedKey = Key.None;
            var dialog = new Window
            {
                Title = "Set Toggle Key",
                Content = new TextBlock
                {
                    Text = "Press a key to set the toggle key. Press Esc to cancel.",
                    Margin = new Thickness(10)
                },
                Width = 300,
                Height = 100,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
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
        }

        private void SetKey(object parameter)
        {
            isSettingKeyboardKey = true;
            isSettingToggleKey = false;
            capturedKey = Key.None;
            var dialog = new Window
            {
                Title = "Set Key",
                Content = new TextBlock
                {
                    Text = "Press a key to set the keyboard key. Press Esc to cancel.",
                    Margin = new Thickness(10)
                },
                Width = 300,
                Height = 100,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow
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
        }

        private void SetConstant(object parameter)
        {
            settings.KeyboardHoldDuration = TimeSpan.Zero;
            OnPropertyChanged(nameof(IsKeyboardHoldDurationMode));
            Properties.Settings.Default.KeyboardHoldDuration = settings.KeyboardHoldDuration;
            Properties.Settings.Default.Save();
        }

        private void SetHoldDuration(object parameter)
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

        private void RefreshApplications(object parameter)
        {
            var apps = applicationDetector.GetRunningApplications();
            Console.WriteLine($"Found {apps.Count} running applications: {string.Join(", ", apps)}");
            RunningApplications = apps;
        }

        private void ResetSettings(object parameter)
        {
            Properties.Settings.Default.Reset();
            settings.ClickScope = Properties.Settings.Default.ClickScope;
            settings.TargetApplication = Properties.Settings.Default.TargetApplication;
            settings.ActionType = Properties.Settings.Default.ActionType;
            settings.MouseButton = Properties.Settings.Default.MouseButton;
            settings.MouseMode = Properties.Settings.Default.MouseMode;
            settings.ClickMode = Properties.Settings.Default.ClickMode;
            settings.ClickDuration = Properties.Settings.Default.ClickDuration;
            settings.MouseHoldDuration = Properties.Settings.Default.MouseHoldDuration;
            settings.HoldMode = Properties.Settings.Default.HoldMode;
            settings.KeyboardKey = (Key)Properties.Settings.Default.KeyboardKey;
            settings.KeyboardMode = Properties.Settings.Default.KeyboardMode;
            settings.KeyboardHoldDuration = Properties.Settings.Default.KeyboardHoldDuration;
            settings.TriggerKey = (Key)Properties.Settings.Default.TriggerKey;
            settings.TriggerKeyModifiers = (ModifierKeys)Properties.Settings.Default.TriggerKeyModifiers;
            settings.Interval = Properties.Settings.Default.Interval;
            settings.Mode = Properties.Settings.Default.Mode;
            settings.TotalDuration = Properties.Settings.Default.TotalDuration;
            settings.Theme = Properties.Settings.Default.Theme;

            OnPropertyChanged(nameof(ClickScope));
            OnPropertyChanged(nameof(TargetApplication));
            OnPropertyChanged(nameof(ActionType));
            OnPropertyChanged(nameof(MouseButton));
            OnPropertyChanged(nameof(MouseMode));
            OnPropertyChanged(nameof(ClickMode));
            OnPropertyChanged(nameof(ClickDurationMinutes));
            OnPropertyChanged(nameof(ClickDurationSeconds));
            OnPropertyChanged(nameof(ClickDurationMilliseconds));
            OnPropertyChanged(nameof(MouseHoldDurationMinutes));
            OnPropertyChanged(nameof(MouseHoldDurationSeconds));
            OnPropertyChanged(nameof(MouseHoldDurationMilliseconds));
            OnPropertyChanged(nameof(HoldMode));
            OnPropertyChanged(nameof(KeyboardKey));
            OnPropertyChanged(nameof(KeyboardKeyDisplay));
            OnPropertyChanged(nameof(KeyboardMode));
            OnPropertyChanged(nameof(KeyboardHoldDurationMinutes));
            OnPropertyChanged(nameof(KeyboardHoldDurationSeconds));
            OnPropertyChanged(nameof(KeyboardHoldDurationMilliseconds));
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

            hotkeyManager?.RegisterTriggerHotkey(settings.TriggerKey, settings.TriggerKeyModifiers);
        }

        public void UpdateStatus(string text, string color)
        {
            StatusText = text;
            StatusColor = color;
            IsRunning = text == "Running";
        }

        public void OnKeyDown(KeyEventArgs e)
        {
            if (e.OriginalSource is TextBox)
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
        private readonly Action<object> execute;
        private readonly Func<object, bool> canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => canExecute == null || canExecute(parameter);

        public void Execute(object parameter) => execute(parameter);
    }
}