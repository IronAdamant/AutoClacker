using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AutoClacker.ViewModels;
using AutoClacker.Services;
using AutoClacker.Models;

namespace AutoClacker;

public partial class SettingsWindow : Window
{
    readonly MainViewModel _vm;
    readonly Action<string>? _onTriggerKeyChanged;
    Button? _trigBtn; TextBlock? _trigLbl; CheckBox? _debugCheck;
    bool _capTrig;

    public SettingsWindow(MainViewModel vm, Action<string>? onTriggerKeyChanged = null)
    {
        InitializeComponent();
        _vm = vm;
        _onTriggerKeyChanged = onTriggerKeyChanged;
        Loaded += OnLoad;
        KeyDown += OnKey;
    }

    void OnLoad(object? s, RoutedEventArgs e)
    {
        _trigBtn = this.FindControl<Button>("SetTriggerButton");
        _trigLbl = this.FindControl<TextBlock>("TriggerKeyLabel");
        _debugCheck = this.FindControl<CheckBox>("DebugConsoleCheckbox");
        if (_trigLbl != null) _trigLbl.Text = _vm.TriggerKey;
        if (_debugCheck != null) _debugCheck.IsChecked = _vm.ShowDebugConsole;
    }

    void OnKey(object? s, KeyEventArgs e)
    {
        if (_capTrig)
        {
            var k = e.Key.ToString();
            Logger.Log($"SettingsWindow: Trigger key changed to {k}");
            _vm.TriggerKey = k;
            if (_trigLbl != null) _trigLbl.Text = k;
            if (_trigBtn != null) _trigBtn.Content = "Set Hotkey";
            _capTrig = false;
            _onTriggerKeyChanged?.Invoke(k);
            e.Handled = true;
        }
    }

    void SetTriggerButton_Click(object? s, RoutedEventArgs e)
    {
        _capTrig = true;
        if (_trigBtn != null) _trigBtn.Content = "Press any key...";
    }

    void DebugConsoleCheckbox_Click(object? s, RoutedEventArgs e)
    {
        _vm.ShowDebugConsole = _debugCheck?.IsChecked ?? false;
        Logger.Log($"ShowDebugConsole set to {_vm.ShowDebugConsole}");
    }

    void CloseButton_Click(object? s, RoutedEventArgs e) => Close();
}
