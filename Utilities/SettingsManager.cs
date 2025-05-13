using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Input;
using AutoClacker.Models;

namespace AutoClacker.Utilities
{
    public static class SettingsManager
    {
        private static readonly string SettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public static Settings LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                {
                    return CreateDefaultSettings();
                }

                using (var stream = new FileStream(SettingsFilePath, FileMode.Open, FileAccess.Read))
                {
                    var serializer = new DataContractJsonSerializer(typeof(Settings));
                    return (Settings)serializer.ReadObject(stream);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}. Creating default settings.");
                return CreateDefaultSettings();
            }
        }

        public static void SaveSettings(Settings settings)
        {
            try
            {
                using (var stream = new FileStream(SettingsFilePath, FileMode.Create, FileAccess.Write))
                {
                    var serializer = new DataContractJsonSerializer(typeof(Settings));
                    serializer.WriteObject(stream, settings);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        private static Settings CreateDefaultSettings()
        {
            var defaultSettings = new Settings
            {
                ClickScope = "Global",
                TargetApplication = "",
                ActionType = "Mouse",
                MouseButton = "Left",
                ClickType = "Single",
                MouseMode = "Click",
                ClickMode = "Constant",
                ClickDuration = TimeSpan.Zero,
                MouseHoldDuration = TimeSpan.FromSeconds(1),
                HoldMode = "HoldDuration",
                MousePhysicalHoldMode = false,
                KeyboardKey = Key.None,
                KeyboardMode = "Press",
                KeyboardHoldDuration = TimeSpan.Zero,
                KeyboardPhysicalHoldMode = false,
                TriggerKey = Key.F5,
                TriggerKeyModifiers = ModifierKeys.None,
                Interval = TimeSpan.FromSeconds(2),
                Mode = "Constant",
                TotalDuration = TimeSpan.Zero,
                Theme = "Light",
                IsTopmost = false
            };

            SaveSettings(defaultSettings);
            return defaultSettings;
        }
    }
}