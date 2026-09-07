using Avalonia.Threading;
using AutoClacker.Core.Abstractions;
using AutoClacker.Core.Domain;
using AutoClacker.Core.Session;
using AutoClacker.Core.Settings;

namespace AutoClacker.App.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private readonly IPlatformServices _platform;
    private readonly SettingsStore _store;
    private readonly AppSettings _settings;
    private readonly ClickSession _session;
    private readonly Timer _saveTimer;
    private readonly ILog _log;
    private CancellationTokenSource? _pendingStartCts;
    private string _status = "Stopped";
    private bool _running;
    private bool _capturing;
    private string _captureTarget = "";
    private string _hotkeyDisplay = "F6";
    private string _actionLimitText = "";
    private bool _disposed;

    public MainViewModel(IPlatformServices platform, SettingsStore? store = null, ILog? log = null)
    {
        _platform = platform;
        _log = log ?? NullLog.Instance;
        _store = store ?? new SettingsStore(log: _log);
        _settings = _store.Load();
        _session = new ClickSession(_platform.Input, () => _settings, _log);
        _saveTimer = new Timer(_ => _store.Save(_settings), null, Timeout.Infinite, Timeout.Infinite);
        _hotkeyDisplay = _settings.TriggerKey;
        _actionLimitText = _settings.ActionLimit is int n && n > 0 ? n.ToString() : "";
        _log.WritesEnabled = _settings.EnableDebugLog;

        _session.RemainingChanged += OnSessionRemainingChanged;
        _session.Stopped += OnSessionStopped;
        _platform.Hotkey.Pressed += OnHotkeyPressed;

        if (!_platform.Capabilities.InputAvailable)
            Status = _platform.Capabilities.UnavailableReason ?? "Input unavailable";
    }

    public string Platform => _platform.PlatformId;
    public PlatformCapabilities Capabilities => _platform.Capabilities;
    public bool CanStart => _platform.Capabilities.InputAvailable;
    public bool ShowPermissionHint => !_platform.Capabilities.InputAvailable;
    public bool SupportsDebugConsole => _platform.Capabilities.SupportsDebugConsole;

    public IReadOnlyList<MouseButton> MouseButtons { get; } =
        [MouseButton.Left, MouseButton.Right, MouseButton.Middle];

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public bool Running
    {
        get => _running;
        private set
        {
            if (SetProperty(ref _running, value))
                OnPropertyChanged(nameof(ToggleLabel));
        }
    }

    public string ToggleLabel => Running ? "Stop" : "Start";
    public bool Capturing => _capturing;

    public bool IsMouseMode
    {
        get => _settings.Mode == AutomationMode.Mouse;
        set
        {
            var mode = value ? AutomationMode.Mouse : AutomationMode.Keyboard;
            if (_settings.Mode == mode) return;
            _settings.Mode = mode;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsKeyboardMode));
            QueueSave();
        }
    }

    public bool IsKeyboardMode
    {
        get => _settings.Mode == AutomationMode.Keyboard;
        set => IsMouseMode = !value;
    }

    public MouseButton SelectedMouseButton
    {
        get => _settings.MouseButton;
        set
        {
            if (_settings.MouseButton == value) return;
            _settings.MouseButton = value;
            OnPropertyChanged();
            QueueSave();
        }
    }

    public bool IsSingleClick
    {
        get => _settings.ClickKind == ClickKind.Single;
        set
        {
            if (value) SetClickKind(ClickKind.Single);
        }
    }

    public bool IsDoubleClick
    {
        get => _settings.ClickKind == ClickKind.Double;
        set
        {
            if (value) SetClickKind(ClickKind.Double);
        }
    }

    void SetClickKind(ClickKind kind)
    {
        if (_settings.ClickKind == kind) return;
        _settings.ClickKind = kind;
        OnPropertyChanged(nameof(IsSingleClick));
        OnPropertyChanged(nameof(IsDoubleClick));
        QueueSave();
    }

    public string KbKey
    {
        get => _settings.KeyboardKey;
        set
        {
            if (_settings.KeyboardKey == value) return;
            _settings.KeyboardKey = value;
            OnPropertyChanged();
            QueueSave();
        }
    }

    public string TriggerKey
    {
        get => _settings.TriggerKey;
        set
        {
            if (_settings.TriggerKey == value) return;
            _settings.TriggerKey = value;
            OnPropertyChanged();
            QueueSave();
            RegisterHotkey();
        }
    }

    public string HotkeyDisplay
    {
        get => _hotkeyDisplay;
        private set => SetProperty(ref _hotkeyDisplay, value);
    }

    public int Interval
    {
        get => _settings.IntervalMs;
        set
        {
            var v = Math.Clamp(value, AppSettings.MinIntervalMs, AppSettings.MaxIntervalMs);
            if (_settings.IntervalMs == v) return;
            _settings.IntervalMs = v;
            OnPropertyChanged();
            QueueSave();
        }
    }

    public bool ActionLimitEnabled
    {
        get => _settings.ActionLimitEnabled;
        set
        {
            if (_settings.ActionLimitEnabled == value) return;
            _settings.ActionLimitEnabled = value;
            OnPropertyChanged();
            QueueSave();
        }
    }

    public string ActionLimitText
    {
        get => _actionLimitText;
        set
        {
            value ??= "";
            if (_actionLimitText == value) return;
            _actionLimitText = value;
            _settings.ActionLimit = int.TryParse(value.Trim(), out var n) && n > 0 ? n : null;
            OnPropertyChanged();
            QueueSave();
        }
    }

    public bool ShowRemaining => _session.Remaining.HasValue;

    public string RemainingDisplay =>
        _session.Remaining is int n ? $"Remaining: {n}" : "";

    public bool ShowDebugConsole
    {
        get => _settings.ShowDebugConsole;
        set
        {
            if (_settings.ShowDebugConsole == value) return;
            _settings.ShowDebugConsole = value;
            OnPropertyChanged();
            QueueSave();
        }
    }

    public bool EnableDebugLog
    {
        get => _settings.EnableDebugLog;
        set
        {
            if (_settings.EnableDebugLog == value) return;
            _settings.EnableDebugLog = value;
            _log.WritesEnabled = value;
            OnPropertyChanged();
            QueueSave();
        }
    }

    public void InitializeHotkey() => RegisterHotkey();

    public void RegisterHotkey()
    {
        var token = _settings.TriggerKeyToken;
        bool ok = _platform.Hotkey.TryRegister(token);
        HotkeyDisplay = ok ? token.Value : $"{token.Value} (unavailable)";
        _log.Info($"Hotkey '{token}' registered={ok}");
    }

    public void StartCapture(string target)
    {
        _captureTarget = target;
        _capturing = true;
        OnPropertyChanged(nameof(Capturing));
    }

    public string? CaptureKey(string rawKeyName)
    {
        if (!_capturing) return null;
        if (!KeyToken.TryParse(rawKeyName, out var key))
        {
            _log.Info($"CaptureKey: Unsupported '{rawKeyName}'");
            return null;
        }

        if (_captureTarget == "kb") KbKey = key.Value;
        else if (_captureTarget == "trigger") TriggerKey = key.Value;

        _capturing = false;
        OnPropertyChanged(nameof(Capturing));
        return key.Value;
    }

    void OnHotkeyPressed() => Dispatcher.UIThread.Post(Toggle);

    public void Toggle()
    {
        if (CancelPendingStart()) { Status = CanStart ? "Stopped" : (Capabilities.UnavailableReason ?? "Unavailable"); return; }
        if (Running) Stop();
        else Start();
    }

    public async Task ToggleWithCountdownAsync(int countdownMs = 2000)
    {
        if (CancelPendingStart()) { Status = CanStart ? "Stopped" : (Capabilities.UnavailableReason ?? "Unavailable"); return; }
        if (Running) { Stop(); return; }
        if (!CanStart)
        {
            Status = Capabilities.UnavailableReason ?? "Input unavailable";
            return;
        }

        var cts = new CancellationTokenSource();
        _pendingStartCts = cts;
        Status = $"Starting in {countdownMs / 1000}s...";
        try
        {
            await Task.Delay(countdownMs, cts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            if (ReferenceEquals(_pendingStartCts, cts)) _pendingStartCts = null;
            cts.Dispose();
        }
        Start();
    }

    bool CancelPendingStart()
    {
        var pending = _pendingStartCts;
        _pendingStartCts = null;
        if (pending is null) return false;
        pending.Cancel();
        return true;
    }

    public void Start()
    {
        if (Running) return;
        if (!_session.TryStart(_platform.Capabilities.InputAvailable, _platform.Capabilities.UnavailableReason))
        {
            Status = _platform.Capabilities.UnavailableReason ?? "Input unavailable";
            Running = false;
            return;
        }

        Running = true;
        Status = "Running";
        NotifyRemaining();
    }

    public void Stop()
    {
        CancelPendingStart();
        if (!Running && !_session.IsRunning) return;
        _session.Stop();
        Running = false;
        Status = CanStart ? "Stopped" : (Capabilities.UnavailableReason ?? "Unavailable");
        NotifyRemaining();
    }

    void OnSessionRemainingChanged() => Dispatcher.UIThread.Post(() =>
    {
        if (_disposed) return;
        NotifyRemaining();
    });

    void OnSessionStopped() => Dispatcher.UIThread.Post(() =>
    {
        if (_disposed) return;
        // A restart may have begun before this posted callback ran.
        if (_session.IsRunning) return;
        Running = false;
        NotifyRemaining();
        if (_session.Remaining == 0)
            Status = "Finished";
        else
            Status = CanStart ? "Stopped" : (Capabilities.UnavailableReason ?? "Unavailable");
    });

    void NotifyRemaining()
    {
        OnPropertyChanged(nameof(RemainingDisplay));
        OnPropertyChanged(nameof(ShowRemaining));
    }

    void QueueSave() => _saveTimer.Change(500, Timeout.Infinite);

    public void Dispose()
    {
        if (_disposed) return;
        _session.RemainingChanged -= OnSessionRemainingChanged;
        _session.Stopped -= OnSessionStopped;
        _platform.Hotkey.Pressed -= OnHotkeyPressed;
        _disposed = true;
        Stop();
        _session.Dispose();
        _saveTimer.Dispose();
        _store.Save(_settings);
        _platform.Dispose();
        GC.SuppressFinalize(this);
    }
}
