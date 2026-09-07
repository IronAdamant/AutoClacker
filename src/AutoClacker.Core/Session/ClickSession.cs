using AutoClacker.Core.Abstractions;
using AutoClacker.Core.Domain;
using AutoClacker.Core.Settings;

namespace AutoClacker.Core.Session;

/// <summary>
/// Platform-agnostic automation loop. Call from any thread; does not touch UI.
/// </summary>
public sealed class ClickSession : IDisposable
{
    private readonly IInputSimulation _input;
    private readonly ILog _log;
    private readonly Func<AppSettings> _settings;
    private readonly Func<int, CancellationToken, Task> _delay;
    private CancellationTokenSource? _cts;
    private bool _disposed;

    public ClickSession(
        IInputSimulation input,
        Func<AppSettings> settings,
        ILog? log = null,
        Func<int, CancellationToken, Task>? delay = null)
    {
        _input = input;
        _settings = settings;
        _log = log ?? NullLog.Instance;
        _delay = delay ?? ((ms, ct) => Task.Delay(ms, ct));
    }

    public bool IsRunning { get; private set; }

    /// <summary>Null means unlimited. When finite, remaining actions including not-yet-issued ones; 0 means finished.</summary>
    public int? Remaining { get; private set; }

    public event Action? RemainingChanged;
    public event Action? Stopped;

    /// <summary>
    /// Starts the loop if input is available. Returns false when blocked (already running or input unavailable).
    /// </summary>
    public bool TryStart(bool inputAvailable, string? unavailableReason = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (IsRunning) return false;
        if (!inputAvailable)
        {
            _log.Info($"ClickSession.TryStart blocked: {unavailableReason ?? "input unavailable"}");
            return false;
        }

        var old = _cts;
        var cts = new CancellationTokenSource();
        _cts = cts;
        old?.Dispose();

        IsRunning = true;
        var s = _settings();
        Remaining = s.HasFiniteActionLimit ? s.ActionLimit : null;
        RemainingChanged?.Invoke();
        var limit = s.HasFiniteActionLimit ? s.ActionLimit.ToString() : "infinite";
        _log.Info($"ClickSession.Start Mode={s.Mode} Interval={s.IntervalMs} Limit={limit} Key={s.KeyboardKey}");
        _ = Task.Run(() => LoopAsync(cts));
        return true;
    }

    public void Stop()
    {
        if (!IsRunning) return;
        IsRunning = false;
        _cts?.Cancel();
        _log.Info("ClickSession.Stop");
    }

    async Task LoopAsync(CancellationTokenSource cts)
    {
        var token = cts.Token;
        int count = 0;
        var modeLabel = "Mouse";
        try
        {
            while (!token.IsCancellationRequested)
            {
                count++;
                var s = _settings();
                modeLabel = s.Mode.ToString();

                if (s.Mode == AutomationMode.Mouse)
                    _input.MouseClick(s.MouseButton, s.ClickKind);
                else
                    _input.KeyPress(s.KeyboardKeyToken);

                if (count % 10 == 0 || count == 1)
                    _log.UpdateCounter(modeLabel, count);

                if (Remaining is int rem)
                {
                    Remaining = rem - 1;
                    RemainingChanged?.Invoke();
                    if (Remaining == 0)
                        break;
                }

                await _delay(s.IntervalMs, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
        catch (Exception ex)
        {
            _log.Info($"ClickSession loop error: {ex}");
        }
        finally
        {
            _log.PrintSummary(modeLabel, count);
            _log.Info($"ClickSession loop ended after {count} iterations");
            if (ReferenceEquals(_cts, cts))
                IsRunning = false;
            Stopped?.Invoke();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        Stop();
        _cts?.Dispose();
        _cts = null;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
