using AutoClacker.Core.Domain;
using AutoClacker.Core.Settings;

namespace AutoClacker.Core.Tests;

public class SettingsStoreTests
{
    [Fact]
    public void Load_clamps_interval_and_round_trips()
    {
        var path = Path.Combine(Path.GetTempPath(), "autoclocker-test-" + Guid.NewGuid() + ".json");
        try
        {
            var store = new SettingsStore(path);
            var s = new AppSettings
            {
                IntervalMs = 99999,
                Mode = AutomationMode.Keyboard,
                MouseButton = MouseButton.Right,
                ClickKind = ClickKind.Double,
                KeyboardKey = "a",
                TriggerKey = "f6",
            };
            store.Save(s);

            var loaded = store.Load();
            Assert.Equal(AppSettings.MaxIntervalMs, loaded.IntervalMs);
            Assert.Equal(AutomationMode.Keyboard, loaded.Mode);
            Assert.Equal(MouseButton.Right, loaded.MouseButton);
            Assert.Equal(ClickKind.Double, loaded.ClickKind);
            Assert.Equal("A", loaded.KeyboardKey);
            Assert.Equal("F6", loaded.TriggerKey);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Load_missing_file_returns_defaults()
    {
        var path = Path.Combine(Path.GetTempPath(), "autoclocker-missing-" + Guid.NewGuid() + ".json");
        var loaded = new SettingsStore(path).Load();
        Assert.Equal(100, loaded.IntervalMs);
        Assert.Equal(AutomationMode.Mouse, loaded.Mode);
        Assert.Equal("F6", loaded.TriggerKey);
    }

    [Fact]
    public void EnableDebugLog_round_trips()
    {
        var path = Path.Combine(Path.GetTempPath(), "autoclocker-test-" + Guid.NewGuid() + ".json");
        try
        {
            var store = new SettingsStore(path);
            store.Save(new AppSettings { EnableDebugLog = true });
            var loaded = store.Load();
            Assert.True(loaded.EnableDebugLog);

            store.Save(new AppSettings { EnableDebugLog = false });
            loaded = store.Load();
            Assert.False(loaded.EnableDebugLog);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Action_limit_enabled_and_count_round_trip()
    {
        var path = Path.Combine(Path.GetTempPath(), "autoclocker-test-" + Guid.NewGuid() + ".json");
        try
        {
            var store = new SettingsStore(path);
            var s = new AppSettings
            {
                ActionLimitEnabled = true,
                ActionLimit = 42,
            };
            store.Save(s);

            var loaded = store.Load();
            Assert.True(loaded.ActionLimitEnabled);
            Assert.Equal(42, loaded.ActionLimit);
            Assert.True(loaded.HasFiniteActionLimit);

            var json = File.ReadAllText(path);
            Assert.DoesNotContain("HasFiniteActionLimit", json);
            Assert.DoesNotContain("KeyboardKeyToken", json);
            Assert.DoesNotContain("TriggerKeyToken", json);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
