using System.Text.Json;
using Microsoft.Win32;
using System.Windows.Forms;

namespace CPUIdleStabliser.Infra
{
    public class UserSettings
    {
        public double TargetTotalPercent { get; set; } = 3.0;
        public bool EcoMode { get; set; } = false;
        public bool StartWithWindows { get; set; } = false;
    }

    public static class SettingsManager
    {
        private static readonly string AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CPUIdleStabliser");
        private static readonly string SettingsFile = Path.Combine(AppDataPath, "settings.json");

        public static UserSettings Load()
        {
            if (!File.Exists(SettingsFile)) return new UserSettings();

            try
            {
                string json = File.ReadAllText(SettingsFile);
                return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
            }
            catch
            {
                return new UserSettings();
            }
        }

        public static void Save(UserSettings settings)
        {
            try
            {
                Directory.CreateDirectory(AppDataPath);
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFile, json);
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to save settings: {ex.Message}");
            }
        }

        public static void SetAutostart(bool enable)
        {
            const string runKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(runKey, true);
                if (key != null)
                {
                    if (enable) key.SetValue("CPUIdleStabliser", Application.ExecutablePath);
                    else
                    {
                        // Clean up both
                        if (key.GetValue("RyzenIdleStabiliser") != null) key.DeleteValue("RyzenIdleStabiliser", false);
                        if (key.GetValue("CPUIdleStabliser") != null) key.DeleteValue("CPUIdleStabliser", false);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to set autostart: {ex.Message}");
            }
        }
    }
}
