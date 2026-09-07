namespace AutoClacker.Core.Abstractions;

public interface ILog
{
    void Info(string message);
    void UpdateCounter(string mode, int count);
    void PrintSummary(string mode, int count);

    /// <summary>When false, Info does not write the debug log file.</summary>
    bool WritesEnabled { get; set; }
}

/// <summary>No-op log for tests and when diagnostics are off.</summary>
public sealed class NullLog : ILog
{
    public static readonly NullLog Instance = new();
    public void Info(string message) { }
    public void UpdateCounter(string mode, int count) { }
    public void PrintSummary(string mode, int count) { }
    public bool WritesEnabled { get; set; }
}
