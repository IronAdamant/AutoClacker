using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using AutoClacker.App.ViewModels;

namespace AutoClacker.App;

public partial class MainWindow : Window
{
    private static readonly IBrush RunningBrush = new SolidColorBrush(Color.Parse("#4CAF50"));
    private static readonly IBrush StoppedBrush = new SolidColorBrush(Color.Parse("#F44336"));
    private static readonly IBrush UnavailableBrush = new SolidColorBrush(Color.Parse("#FF9800"));

    private readonly MainViewModel _vm;

    public MainWindow(MainViewModel vm)
    {
        _vm = vm;
        DataContext = _vm;
        InitializeComponent();

        _vm.PropertyChanged += OnViewModelPropertyChanged;
        Loaded += OnLoaded;
        Closed += OnClosed;
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
        UpdateStatusBrush();
    }

    void OnLoaded(object? s, RoutedEventArgs e)
    {
        _vm.InitializeHotkey();
    }

    void OnViewModelPropertyChanged(object? s, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.Running) or nameof(MainViewModel.Status) or nameof(MainViewModel.CanStart))
            UpdateStatusBrush();
    }

    void UpdateStatusBrush()
    {
        if (!_vm.CanStart) StatusBorder.Background = UnavailableBrush;
        else StatusBorder.Background = _vm.Running ? RunningBrush : StoppedBrush;
    }

    void OnClosed(object? s, EventArgs e)
    {
        _vm.PropertyChanged -= OnViewModelPropertyChanged;
        _vm.Dispose();
    }

    void OnPreviewKeyDown(object? s, KeyEventArgs e)
    {
        if (_vm.Capturing)
        {
            var captured = _vm.CaptureKey(e.Key.ToString());
            SetKeyButton.Content = captured is null ? "Unsupported key…" : "Set Key";
            e.Handled = true;
            return;
        }

        // Injected keys go to the focused app. If AutoClacker is frontmost they
        // would otherwise flood this window and freeze AppKit (see 2026-09-07 hang).
        if (_vm.Running)
            e.Handled = true;
    }

    void SetKeyButton_Click(object? s, RoutedEventArgs e)
    {
        _vm.StartCapture("kb");
        SetKeyButton.Content = "Press any key...";
    }

    async void ToggleButton_Click(object? s, RoutedEventArgs e)
    {
        try { await _vm.ToggleWithCountdownAsync(); }
        catch { /* logged in session */ }
    }

    async void SettingsButton_Click(object? s, RoutedEventArgs e) =>
        await new SettingsWindow(_vm).ShowDialog(this);
}
