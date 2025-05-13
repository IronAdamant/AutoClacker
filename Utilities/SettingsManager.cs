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
                using (var memoryStream = new MemoryStream())
                {
                    var serializer = new DataContractJsonSerializer(typeof(Settings));
                    serializer.WriteObject(memoryStream, settings);
                    memoryStream.Position = 0;

                    // Read the JSON string and format it
                    using (var reader = new StreamReader(memoryStream))
                    {
                        string jsonString = reader.ReadToEnd();
                        // Convert to formatted JSON
                        using (var fileStream = new FileStream(SettingsFilePath, FileMode.Create, FileAccess.Write))
                        using (var writer = new StreamWriter(fileStream))
                        {
                            writer.Write(FormatJson(jsonString));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        private static string FormatJson(string json)
        {
            int indentation = 0;
            const int indentSize = 2;
            var builder = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];

                if (c == '"' && (i == 0 || json[i - 1] != '\\'))
                {
                    inQuotes = !inQuotes;
                    builder.Append(c);
                    continue;
                }

                if (inQuotes)
                {
                    builder.Append(c);
                    continue;
                }

                switch (c)
                {
                    case '{':
                    case '[':
                        builder.Append(c);
                        builder.AppendLine();
                        indentation += indentSize;
                        builder.Append(new string(' ', indentation));
                        break;
                    case '}':
                    case ']':
                        builder.AppendLine();
                        indentation -= indentSize;
                        builder.Append(new string(' ', indentation));
                        builder.Append(c);
                        break;
                    case ',':
                        builder.Append(c);
                        builder.AppendLine();
                        builder.Append(new string(' ', indentation));
                        break;
                    case ':':
                        builder.Append(c);
                        builder.Append(' ');
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }

            return builder.ToString();
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