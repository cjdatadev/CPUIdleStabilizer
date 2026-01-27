using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Diagnostics;

using Microsoft.Win32;
using System.Windows.Forms;

namespace CPUIdleStabilizer.Infra
{
    public class UserSettings
    {
        public double TargetTotalPercent { get; set; } = 3.0;
        public bool EcoMode { get; set; } = false;
        public bool StartWithWindows { get; set; } = false;
        public bool StartMinimized { get; set; } = false;
    }

    [JsonSerializable(typeof(UserSettings))]
    internal partial class UserSettingsContext : JsonSerializerContext
    {
    }

    public static class SettingsManager
    {
        private static readonly string AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CPUIdleStabilizer");
        private static readonly string SettingsFile = Path.Combine(AppDataPath, "settings.json");
        public static readonly string InstallFolder = Path.Combine(AppDataPath, "bin");

        public static bool IsRunningFromInstallFolder => 
            string.Equals(Path.GetDirectoryName(Application.ExecutablePath), InstallFolder, StringComparison.OrdinalIgnoreCase);

        public static UserSettings Load()
        {
            if (File.Exists(SettingsFile))
            {
                try
                {
                    string json = File.ReadAllText(SettingsFile);
                    return JsonSerializer.Deserialize(json, UserSettingsContext.Default.UserSettings) ?? new UserSettings();
                }
                catch
                {
                    return new UserSettings();
                }
            }

            // Migration check: If new settings don't exist, check the old folder
            string oldAppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RyzenIdleStabiliser");
            string oldSettingsFile = Path.Combine(oldAppDataPath, "settings.json");

            if (File.Exists(oldSettingsFile))
            {
                try
                {
                    Logger.Log("Migrating settings from RyzenIdleStabiliser to CPUIdleStabilizer.");
                    string json = File.ReadAllText(oldSettingsFile);
                    var settings = JsonSerializer.Deserialize(json, UserSettingsContext.Default.UserSettings);
                    if (settings != null)
                    {
                        Save(settings); // Save to new location
                        return settings;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Migration failed: {ex.Message}");
                }
            }

            return new UserSettings();
        }

        public static void Save(UserSettings settings)
        {
            try
            {
                if (!Directory.Exists(AppDataPath)) Directory.CreateDirectory(AppDataPath);
                string json = JsonSerializer.Serialize(settings, UserSettingsContext.Default.UserSettings);
                File.WriteAllText(SettingsFile, json);
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to save settings: {ex.Message}");
            }
        }

        public static void SetAutostart(bool enable, bool minimized, string? customExePath = null)
        {
            const string runKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
            const string appName = "CPUIdleStabilizer";
            string[] legacyNames = { "RyzenIdleStabiliser", "CPUIdleStabliser" };

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(runKey, true);
                if (key != null)
                {
                    // Clean up all legacy names
                    foreach (var legacy in legacyNames)
                    {
                        if (key.GetValue(legacy) != null) key.DeleteValue(legacy, false);
                    }

                    if (enable)
                    {
                        // Prefer the stable install path if it exists, even if we're running from elsewhere
                        string? exePath = customExePath;
                        if (string.IsNullOrEmpty(exePath))
                        {
                            string currentExeName = Path.GetFileName(Environment.ProcessPath ?? Application.ExecutablePath);
                            string stablePath = Path.Combine(InstallFolder, currentExeName);
                            
                            if (File.Exists(stablePath))
                            {
                                exePath = stablePath;
                            }
                            else
                            {
                                exePath = Environment.ProcessPath ?? Application.ExecutablePath;
                            }
                        }

                        string command = $"\"{exePath}\" --autostart";
                        if (minimized) command += " --minimized";
                        key.SetValue(appName, command);
                    }
                    else
                    {
                        if (key.GetValue(appName) != null) key.DeleteValue(appName, false);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to set autostart: {ex.Message}");
            }
        }

        public static void CleanupLegacyData(bool includeCurrent = false)
        {
            string appDataRoot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string[] foldersToClean = { 
                Path.Combine(appDataRoot, "RyzenIdleStabiliser"), 
                Path.Combine(appDataRoot, "CPUIdleStabliser") 
            };

            foreach (var folder in foldersToClean)
            {
                try
                {
                    if (Directory.Exists(folder))
                    {
                        Logger.Log($"Cleaning up legacy folder: {folder}");
                        Directory.Delete(folder, true);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Failed to delete legacy folder {folder}: {ex.Message}");
                }
            }

            // Cleanup registry
            const string runKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
            string[] keysToClean = { "RyzenIdleStabiliser", "CPUIdleStabliser" };

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(runKey, true);
                if (key != null)
                {
                    foreach (var k in keysToClean)
                    {
                        if (key.GetValue(k) != null)
                        {
                            Logger.Log($"Cleaning up legacy registry key: {k}");
                            key.DeleteValue(k, false);
                        }
                    }

                    if (includeCurrent)
                    {
                        if (key.GetValue("CPUIdleStabilizer") != null)
                        {
                            Logger.Log("Removing autostart entry for CPUIdleStabilizer");
                            key.DeleteValue("CPUIdleStabilizer", false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Registry cleanup failed: {ex.Message}");
            }

            if (includeCurrent)
            {
                try
                {
                    if (Directory.Exists(AppDataPath))
                    {
                        Logger.Log($"Cleaning up current AppData folder: {AppDataPath}");
                        // We can't delete the folder if we are running from it (bin folder inside it)
                        // but we can delete the settings file and other contents
                        if (File.Exists(SettingsFile)) File.Delete(SettingsFile);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"Current AppData cleanup failed: {ex.Message}");
                }
            }
        }

        public static void Uninstall()
        {
            Logger.Log("User initiated uninstallation.");
            CleanupLegacyData(true);
            SelfDestruct();
        }

        private static void SelfDestruct()
        {
            try
            {
                string batchFile = Path.Combine(Path.GetTempPath(), "uninstall_cpuidle.bat");
                string exePath = Application.ExecutablePath;
                string exeDir = Path.GetDirectoryName(exePath) ?? "";
                string appDataFolder = AppDataPath;
                string batchContent = $@"
@echo off
setlocal
timeout /t 2 /nobreak > nul
:retry_appdata
if exist ""{appDataFolder}"" (
    rd /s /q ""{appDataFolder}""
    if exist ""{appDataFolder}"" (
        timeout /t 1 /nobreak > nul
        goto retry_appdata
    )
)
del ""%~f0""
";
                File.WriteAllText(batchFile, batchContent);

                Process.Start(new ProcessStartInfo
                {
                    FileName = batchFile,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                
                Logger.Log("Self-destruct script scheduled.");
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to schedule self-destruct: {ex.Message}");
            }
        }
    }
}
