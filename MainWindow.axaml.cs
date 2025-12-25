using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using AutoClacker.ViewModels;
using AutoClacker.Services;

namespace AutoClacker;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private GlobalHotkey? _globalHotkey;
    private Slider? _slider; private TextBlock? _intLbl, _statusLbl, _kbLbl, _hotkeyLbl; private Border? _statusBorder;
    private Button? _toggleBtn, _setKeyBtn; private ComboBox? _mouseCombo;
    private RadioButton? _single, _double, _mouseMode, _kbMode; private StackPanel? _mousePanel, _kbPanel;

    public MainWindow()
    {
        Logger.Init();
        Logger.Log("MainWindow constructor called");
        
        InitializeComponent();
        _vm = new MainViewModel();
        DataContext = _vm;
        Loaded += OnLoaded;
        KeyDown += OnKeyDown;
        Closed += OnClosed;
        
        Logger.Log($"MainWindow: ViewModel created, TriggerKey={_vm.TriggerKey}, Mode={(_vm.IsMouseMode ? "Mouse" : "Keyboard")}");
    }

    void OnLoaded(object? s, RoutedEventArgs e)
    {
        Logger.Log("MainWindow.OnLoaded called");
        
        _slider = this.FindControl<Slider>("IntervalSlider");
        _intLbl = this.FindControl<TextBlock>("IntervalLabel");
        _statusLbl = this.FindControl<TextBlock>("StatusLabel");
        _kbLbl = this.FindControl<TextBlock>("KeyboardKeyLabel");
        _statusBorder = this.FindControl<Border>("StatusBorder");
        _toggleBtn = this.FindControl<Button>("ToggleButton");
        _setKeyBtn = this.FindControl<Button>("SetKeyButton");
        _mouseCombo = this.FindControl<ComboBox>("MouseButtonCombo");
        _single = this.FindControl<RadioButton>("SingleClick");
        _double = this.FindControl<RadioButton>("DoubleClick");
        _mouseMode = this.FindControl<RadioButton>("MouseModeRadio");
        _kbMode = this.FindControl<RadioButton>("KeyboardModeRadio");
        _mousePanel = this.FindControl<StackPanel>("MouseSettings");
        _kbPanel = this.FindControl<StackPanel>("KeyboardSettings");
        _hotkeyLbl = this.FindControl<TextBlock>("HotkeyLabel");

        if (_kbLbl != null) _kbLbl.Text = _vm.KbKey;
        if (_hotkeyLbl != null) _hotkeyLbl.Text = _vm.TriggerKey;
        if (_slider != null) { _slider.Value = _vm.Interval; _slider.PropertyChanged += (_, a) => { if (a.Property.Name == "Value") { _intLbl!.Text = $"Interval: {(int)_slider.Value}ms"; _vm.Interval = (int)_slider.Value; } }; }
        if (_intLbl != null) _intLbl.Text = $"Interval: {_vm.Interval}ms";
        if (_vm.IsMouseMode == false && _kbMode != null) { _kbMode.IsChecked = true; SetMode(false); }
        if (_mouseMode != null) _mouseMode.IsCheckedChanged += (_, _) => SetMode(_mouseMode.IsChecked == true);
        if (_kbMode != null) _kbMode.IsCheckedChanged += (_, _) => SetMode(_kbMode.IsChecked != true);
        
        // Register global hotkey (works even when app not focused)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _globalHotkey = new GlobalHotkey();
            _globalHotkey.OnHotkeyPressed += OnGlobalHotkey;
            _globalHotkey.Register(_vm.TriggerKey);
            Logger.Log($"Global hotkey registered for {_vm.TriggerKey}");
        }
        
        Logger.Log($"OnLoaded complete. Press {_vm.TriggerKey} anywhere to start/stop!");
    }

    void OnGlobalHotkey()
    {
        Logger.Log("Global hotkey pressed!");
        Dispatcher.UIThread.Post(() =>
        {
            ToggleFromHotkey();
        });
    }

    void OnClosed(object? s, EventArgs e)
    {
        Logger.Log("MainWindow.OnClosed called");
        _vm.Stop();
        _globalHotkey?.Dispose();
    }

    void SetMode(bool mouse)
    {
        Logger.Log($"SetMode called: mouse={mouse}");
        if (_mousePanel != null) _mousePanel.IsVisible = mouse;
        if (_kbPanel != null) _kbPanel.IsVisible = !mouse;
        _vm.IsMouseMode = mouse;
    }
    
    void OnKeyDown(object? s, KeyEventArgs e)
    {
        Logger.Log($"OnKeyDown: Key={e.Key}, Capturing={_vm.Capturing}");
        
        if (_vm.Capturing)
        {
            Logger.Log($"Capturing key: {e.Key}");
            _vm.CaptureKey(e.Key.ToString());
            if (_kbLbl != null && _vm.Target == "kb") _kbLbl.Text = e.Key.ToString();
            if (_setKeyBtn != null) _setKeyBtn.Content = "Set Key";
            e.Handled = true;
        }
    }
    
    void SetKeyButton_Click(object? s, RoutedEventArgs e)
    {
        Logger.Log("SetKeyButton_Click - starting capture");
        _vm.StartCapture("kb");
        if (_setKeyBtn != null) _setKeyBtn.Content = "Press any key...";
    }
    
    async void ToggleButton_Click(object? s, RoutedEventArgs e)
    {
        Logger.Log("ToggleButton_Click called");
        if (_mouseCombo?.SelectedItem is ComboBoxItem i)
        {
            _vm.MouseBtn = i.Content?.ToString() ?? "Left";
            Logger.Log($"MouseBtn set to: {_vm.MouseBtn}");
        }
        _vm.ClickType = _double?.IsChecked == true ? "Double" : "Single";
        Logger.Log($"ClickType set to: {_vm.ClickType}");
        
        if (!_vm.Running)
        {
            // Starting - add delay so user can move cursor away
            UpdateUIState(true, "Starting in 2s...");
            Logger.Log("Starting in 2 seconds - move cursor away!");
            await Task.Delay(2000);
            if (!_vm.Running) // Check if cancelled during delay
            {
                _vm.Start();
                UpdateUIState(true, "Running");
            }
        }
        else
        {
            _vm.Stop();
            UpdateUIState(false, "Stopped");
        }
    }

    void ToggleFromHotkey()
    {
        Logger.Log($"ToggleFromHotkey: Running={_vm.Running}");
        _vm.Toggle();
        UpdateUIState(_vm.Running, _vm.Status);
    }

    void UpdateUIState(bool running, string status)
    {
        if (_statusLbl != null) _statusLbl.Text = status;
        if (_toggleBtn != null) _toggleBtn.Content = running ? "Stop" : "Start";
        if (_statusBorder != null) _statusBorder.Background = new SolidColorBrush(running ? Color.Parse("#4CAF50") : Color.Parse("#F44336"));
        Logger.Log($"UI updated: Status={status}");
    }

    void SettingsButton_Click(object? s, RoutedEventArgs e)
    {
        Logger.Log("Opening SettingsWindow");
        new SettingsWindow(_vm, OnTriggerKeyChanged).ShowDialog(this);
    }

    void OnTriggerKeyChanged(string newKey)
    {
        Logger.Log($"Trigger key changed to {newKey}, re-registering hotkey");
        _globalHotkey?.Register(newKey);
        if (_hotkeyLbl != null) _hotkeyLbl.Text = newKey;
    }
}