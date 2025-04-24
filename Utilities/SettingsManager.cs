using System;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace AutoClicker.Utilities
{
    public static class SettingsManager
    {
        private static readonly string SettingsFile = Path.Combine(Application.StartupPath, "configs", "settings.json");

        public static Settings LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    string json = File.ReadAllText(SettingsFile);
                    return JsonConvert.DeserializeObject<Settings>(json) ?? Settings.Default;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}. Using defaults.");
            }
            return Settings.Default;
        }

        public static void SaveSettings(Settings settings)
        {
            try
            {
                // Ensure the 'configs' directory exists
                string directory = Path.GetDirectoryName(SettingsFile);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(SettingsFile, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}");
            }
        }
    }
}