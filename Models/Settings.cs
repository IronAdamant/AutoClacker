using System;
using System.IO;
using System.Text.Json;

namespace AutoClacker.Models
{
    public class Settings
    {
        static readonly string Path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoClacker", "settings.json");
        public string Mode { get; set; } = "Mouse";
        public string MouseButton { get; set; } = "Left";
        public string ClickType { get; set; } = "Single";
        public string KeyboardKey { get; set; } = "Space";
        public int IntervalMs { get; set; } = 100;
        public string TriggerKey { get; set; } = "F6";
        public bool ShowDebugConsole { get; set; } = false;

        public void Save() { try { var d = System.IO.Path.GetDirectoryName(Path); if (!string.IsNullOrEmpty(d) && !Directory.Exists(d)) Directory.CreateDirectory(d); File.WriteAllText(Path, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true })); } catch { } }
        public static Settings Load() { try { if (File.Exists(Path)) return JsonSerializer.Deserialize<Settings>(File.ReadAllText(Path)) ?? new(); } catch { } return new(); }
    }
}
