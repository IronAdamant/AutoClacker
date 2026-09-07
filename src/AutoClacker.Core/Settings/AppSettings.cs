using System.Text.Json;
using System.Text.Json.Serialization;
using AutoClacker.Core.Abstractions;
using AutoClacker.Core.Domain;

namespace AutoClacker.Core.Settings;

public sealed class AppSettings
{
    public const int MinIntervalMs = 10;
    public const int MaxIntervalMs = 2000;

    public AutomationMode Mode { get; set; } = AutomationMode.Mouse;
    public MouseButton MouseButton { get; set; } = MouseButton.Left;
    public ClickKind ClickKind { get; set; } = ClickKind.Single;
    public string KeyboardKey { get; set; } = "SPACE";
    public int IntervalMs { get; set; } = 100;
    public string TriggerKey { get; set; } = "F6";
    public bool ShowDebugConsole { get; set; }
    public bool EnableDebugLog { get; set; }
    public bool ActionLimitEnabled { get; set; }
    public int? ActionLimit { get; set; }

    [JsonIgnore]
    public bool HasFiniteActionLimit => ActionLimitEnabled && ActionLimit is int n && n > 0;

    [JsonIgnore]
    public KeyToken KeyboardKeyToken =>
        KeyToken.TryParse(KeyboardKey, out var t) ? t : new KeyToken("SPACE");

    [JsonIgnore]
    public KeyToken TriggerKeyToken =>
        KeyToken.TryParse(TriggerKey, out var t) ? t : new KeyToken("F6");

    public void Clamp() =>
        IntervalMs = Math.Clamp(IntervalMs, MinIntervalMs, MaxIntervalMs);
}

public sealed class SettingsStore
{
    private readonly string _path;
    private readonly ILog _log;

    public SettingsStore(string? path = null, ILog? log = null)
    {
        // Use System.IO.Path explicitly — do not name a member "Path" (shadows Path.Combine).
        _path = path ?? System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AutoClacker", "settings.json");
        _log = log ?? NullLog.Instance;
    }

    /// <summary>Filesystem path of the settings JSON file.</summary>
    public string FilePath => _path;

    public AppSettings Load()
    {
        var s = new AppSettings();
        try
        {
            if (File.Exists(_path))
            {
                var json = File.ReadAllText(_path);
                s = JsonSerializer.Deserialize(json, AppSettingsJsonContext.Default.AppSettings) ?? new();
            }
        }
        catch (Exception ex)
        {
            _log.Info($"Settings.Load failed, using defaults: {ex.Message}");
            s = new AppSettings();
        }

        s.Clamp();
        // Normalize key tokens in stored form
        if (KeyToken.TryParse(s.KeyboardKey, out var kb)) s.KeyboardKey = kb.Value;
        if (KeyToken.TryParse(s.TriggerKey, out var tr)) s.TriggerKey = tr.Value;
        return s;
    }

    public void Save(AppSettings settings)
    {
        try
        {
            settings.Clamp();
            var dir = System.IO.Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(_path, JsonSerializer.Serialize(settings, AppSettingsJsonContext.Default.AppSettings));
        }
        catch (Exception ex)
        {
            _log.Info($"Settings.Save failed: {ex.Message}");
        }
    }
}

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(AppSettings))]
public partial class AppSettingsJsonContext : JsonSerializerContext;
