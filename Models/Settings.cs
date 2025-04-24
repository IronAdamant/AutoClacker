using System;
using Newtonsoft.Json;
using System.Windows.Forms;

namespace AutoClicker
{
    public class Settings
    {
        public string ClickScope { get; set; } = "Global";
        public string TargetApplication { get; set; } = "";
        public string ActionType { get; set; } = "Mouse";
        public string MouseButton { get; set; } = "Left";
        public string MouseMode { get; set; } = "Click";
        public TimeSpan MouseHoldDuration { get; set; } = TimeSpan.FromSeconds(1); // Removed hours
        public Keys KeyboardKey { get; set; } = Keys.None;
        public string KeyboardMode { get; set; } = "Press";
        public TimeSpan KeyboardHoldDuration { get; set; } = TimeSpan.Zero; // Removed hours
        public Keys TriggerKey { get; set; } = Keys.F5;
        public int TriggerKeyModifiers { get; set; } = 0; // For Ctrl, Shift, Alt
        public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(1); // Removed hours
        public string Mode { get; set; } = "Constant";
        public TimeSpan TotalDuration { get; set; } = TimeSpan.Zero; // Removed hours
        public string Theme { get; set; } = "Light";

        [JsonIgnore]
        public static Settings Default => new Settings();
    }
}