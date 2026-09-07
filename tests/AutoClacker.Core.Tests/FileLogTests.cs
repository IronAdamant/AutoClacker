using AutoClacker.Core.Logging;

namespace AutoClacker.Core.Tests;

public class FileLogTests
{
    static string TempLogPath() =>
        Path.Combine(Path.GetTempPath(), "autoclocker-log-" + Guid.NewGuid() + ".log");

    [Fact]
    public void Disabled_info_does_not_write_the_debug_log()
    {
        var path = TempLogPath();
        try
        {
            var log = new FileLog(path) { WritesEnabled = false };
            log.Init();
            log.Info("must-not-appear");
            log.DisposeWriter();

            if (File.Exists(path))
            {
                var text = File.ReadAllText(path);
                Assert.DoesNotContain("must-not-appear", text);
                Assert.DoesNotContain("Log started", text);
            }
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Enabled_info_writes_the_logged_message()
    {
        var path = TempLogPath();
        try
        {
            var log = new FileLog(path) { WritesEnabled = true };
            log.Init();
            log.Info("hello-debug-log");
            log.DisposeWriter();

            Assert.True(File.Exists(path));
            var text = File.ReadAllText(path);
            Assert.Contains("hello-debug-log", text);
            Assert.Contains(DateTime.Now.ToString("yyyy-MM-dd"), text);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Disabling_after_init_stops_further_info_writes()
    {
        var path = TempLogPath();
        try
        {
            var log = new FileLog(path) { WritesEnabled = true };
            log.Init();
            log.Info("before-disable");
            log.WritesEnabled = false;
            log.Info("after-disable");
            log.DisposeWriter();

            var text = File.ReadAllText(path);
            Assert.Contains("before-disable", text);
            Assert.DoesNotContain("after-disable", text);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
