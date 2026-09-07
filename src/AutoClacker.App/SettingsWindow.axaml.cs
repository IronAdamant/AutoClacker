using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AutoClacker.App.ViewModels;

namespace AutoClacker.App;

public partial class SettingsWindow : Window
{
    private readonly MainViewModel _vm;
    private bool _capturingTrigger;

    public SettingsWindow(MainViewModel vm)
    {
        _vm = vm;
        DataContext = _vm;
        InitializeComponent();
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
    }

    void SetTriggerButton_Click(object? s, RoutedEventArgs e)
    {
        _capturingTrigger = true;
        _vm.StartCapture("trigger");
        SetTriggerButton.Content = "Press any key...";
    }

    void OnPreviewKeyDown(object? s, KeyEventArgs e)
    {
        if (!_capturingTrigger && !_vm.Capturing) return;
        var captured = _vm.CaptureKey(e.Key.ToString());
        if (captured is null)
        {
            SetTriggerButton.Content = "Unsupported key…";
        }
        else
        {
            SetTriggerButton.Content = "Set Hotkey";
            _capturingTrigger = false;
        }
        e.Handled = true;
    }

    void Close_Click(object? s, RoutedEventArgs e) => Close();
}
