using AutoClacker.Core.Abstractions;
using AutoClacker.Core.Domain;
using AutoClacker.Core.Session;
using AutoClacker.Core.Settings;

namespace AutoClacker.Core.Tests;

public class ClickSessionTests
{
    sealed class FakeInput : IInputSimulation
    {
        public int Clicks;
        public int KeyPresses;
        public void MouseClick(MouseButton button, ClickKind kind) => Interlocked.Increment(ref Clicks);
        public void KeyPress(KeyToken key) => Interlocked.Increment(ref KeyPresses);
        public void Dispose() { }
    }

    static ClickSession CreateSession(FakeInput input, AppSettings settings) =>
        new(input, () => settings, delay: (ms, ct) => Task.Delay(8, ct));

    static async Task WaitUntil(Func<bool> condition, int timeoutMs = 2000)
    {
        var start = Environment.TickCount64;
        while (Environment.TickCount64 - start < timeoutMs)
        {
            if (condition()) return;
            await Task.Delay(10);
        }

        throw new TimeoutException("Condition was not met within the timeout.");
    }

    [Fact]
    public async Task TryStart_runs_loop_and_stop_cancels()
    {
        var input = new FakeInput();
        var settings = new AppSettings { IntervalMs = 10, Mode = AutomationMode.Mouse };
        var session = new ClickSession(input, () => settings, delay: async (ms, ct) => await Task.Delay(5, ct));

        Assert.True(session.TryStart(inputAvailable: true));
        await Task.Delay(80);
        session.Stop();
        await Task.Delay(40);

        Assert.True(input.Clicks >= 1);
        Assert.False(session.IsRunning);
        session.Dispose();
    }

    [Fact]
    public void TryStart_blocked_when_input_unavailable()
    {
        var input = new FakeInput();
        var settings = new AppSettings();
        using var session = new ClickSession(input, () => settings);

        Assert.False(session.TryStart(inputAvailable: false, unavailableReason: "no display"));
        Assert.Equal(0, input.Clicks);
        Assert.Equal(0, input.KeyPresses);
        Assert.False(session.IsRunning);
    }

    [Fact]
    public async Task Mouse_mode_increments_only_mouse_clicks()
    {
        var input = new FakeInput();
        var settings = new AppSettings { IntervalMs = 10, Mode = AutomationMode.Mouse };
        using var session = CreateSession(input, settings);

        Assert.True(session.TryStart(inputAvailable: true));
        await WaitUntil(() => input.Clicks >= 3);
        session.Stop();
        await WaitUntil(() => !session.IsRunning);

        Assert.True(input.Clicks >= 3);
        Assert.Equal(0, input.KeyPresses);
    }

    [Fact]
    public async Task Keyboard_mode_increments_only_key_presses()
    {
        var input = new FakeInput();
        var settings = new AppSettings
        {
            IntervalMs = 10,
            Mode = AutomationMode.Keyboard,
            KeyboardKey = "SPACE",
        };
        using var session = CreateSession(input, settings);

        Assert.True(session.TryStart(inputAvailable: true));
        await WaitUntil(() => input.KeyPresses >= 3);
        session.Stop();
        await WaitUntil(() => !session.IsRunning);

        Assert.True(input.KeyPresses >= 3);
        Assert.Equal(0, input.Clicks);
    }

    [Fact]
    public async Task Unchecked_action_limit_runs_until_stop()
    {
        var input = new FakeInput();
        var settings = new AppSettings
        {
            IntervalMs = 10,
            Mode = AutomationMode.Mouse,
            ActionLimitEnabled = false,
            ActionLimit = 5,
        };
        using var session = CreateSession(input, settings);

        Assert.True(session.TryStart(inputAvailable: true));
        await WaitUntil(() => input.Clicks > 5);
        Assert.True(session.IsRunning);
        Assert.Null(session.Remaining);

        var before = input.Clicks;
        await WaitUntil(() => input.Clicks > before);
        Assert.True(session.IsRunning);

        session.Stop();
        await WaitUntil(() => !session.IsRunning);
        Assert.True(input.Clicks > 5);
    }

    [Fact]
    public async Task Enabled_null_action_limit_runs_until_stop()
    {
        var input = new FakeInput();
        var settings = new AppSettings
        {
            IntervalMs = 10,
            Mode = AutomationMode.Mouse,
            ActionLimitEnabled = true,
            ActionLimit = null,
        };
        using var session = CreateSession(input, settings);

        Assert.True(session.TryStart(inputAvailable: true));
        await WaitUntil(() => input.Clicks >= 3);
        Assert.True(session.IsRunning);
        Assert.Null(session.Remaining);

        var before = input.Clicks;
        await WaitUntil(() => input.Clicks > before);
        Assert.True(session.IsRunning);

        session.Stop();
        await WaitUntil(() => !session.IsRunning);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Enabled_non_positive_action_limit_runs_until_stop(int limit)
    {
        var input = new FakeInput();
        var settings = new AppSettings
        {
            IntervalMs = 10,
            Mode = AutomationMode.Mouse,
            ActionLimitEnabled = true,
            ActionLimit = limit,
        };
        using var session = CreateSession(input, settings);

        Assert.True(session.TryStart(inputAvailable: true));
        await WaitUntil(() => input.Clicks >= 3);
        Assert.True(session.IsRunning);
        Assert.Null(session.Remaining);

        var before = input.Clicks;
        await WaitUntil(() => input.Clicks > before);
        Assert.True(session.IsRunning);

        session.Stop();
        await WaitUntil(() => !session.IsRunning);
    }

    [Fact]
    public async Task Finite_action_limit_stops_after_exactly_n_actions()
    {
        var input = new FakeInput();
        var settings = new AppSettings
        {
            IntervalMs = 10,
            Mode = AutomationMode.Mouse,
            ActionLimitEnabled = true,
            ActionLimit = 3,
        };
        using var session = CreateSession(input, settings);

        Assert.True(session.TryStart(inputAvailable: true));
        await WaitUntil(() => !session.IsRunning);
        Assert.Equal(3, input.Clicks);
        Assert.Equal(0, input.KeyPresses);
        Assert.Equal(0, session.Remaining);

        await Task.Delay(80);
        Assert.Equal(3, input.Clicks);
        Assert.Equal(0, input.KeyPresses);
        Assert.False(session.IsRunning);
        Assert.Equal(0, session.Remaining);
    }
}
