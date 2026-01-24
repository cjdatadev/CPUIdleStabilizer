using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using CPUIdleStabliser.Core;
using CPUIdleStabliser.Infra;

namespace CPUIdleStabliser.UI
{
    public class TrayAppContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly LoadController _controller;
        private readonly UserSettings _settings;
        private readonly System.Windows.Forms.Timer _statusTimer;
        private SettingsForm? _settingsForm;

        public TrayAppContext(LoadController controller, UserSettings settings, bool startHidden)
        {
            _controller = controller;
            _settings = settings;

            // Initialize Tray Icon with a custom icon from PNG if available
            _trayIcon = new NotifyIcon()
            {
                Icon = GetAppIcon(),
                Text = "CPUIdleStabliser",
                ContextMenuStrip = CreateContextMenu(),
                Visible = true
            };

            _trayIcon.DoubleClick += (s, e) => ShowSettings();
            _trayIcon.MouseMove += UpdateTooltip;

            // Simple timer to ensure workers are running if they should be (after manual start)
            _statusTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            _statusTimer.Tick += (s, e) => CheckStatus();
            _statusTimer.Start();

            if (!startHidden)
            {
                ShowSettings();
            }
            else if (_settings.TargetTotalPercent > 0 && _settings.StartWithWindows)
            {
                // Only auto-start if we are starting hidden AND it's enabled
                _controller.Start(_settings.TargetTotalPercent, _settings.EcoMode);
                Logger.Log($"App auto-started from hidden. Target: {settings.TargetTotalPercent}%");
            }
        }

        private ContextMenuStrip CreateContextMenu()
        {
            var menu = new ContextMenuStrip();

            var startStopItem = new ToolStripMenuItem(_controller.IsRunning ? "Stop" : "Start", null, (s, e) => ToggleRunning());
            menu.Items.Add(startStopItem);

            var targetMenu = new ToolStripMenuItem("Set Target Load");
            for (int i = 1; i <= 5; i++)
            {
                int val = i;
                targetMenu.DropDownItems.Add($"{val}%", null, (s, e) => SetTarget(val));
            }
            targetMenu.DropDownItems.Add("10%", null, (s, e) => SetTarget(10));
            targetMenu.DropDownItems.Add(new ToolStripSeparator());
            targetMenu.DropDownItems.Add("Custom...", null, (s, e) => ShowSettings());
            menu.Items.Add(targetMenu);

            var ecoMenu = new ToolStripMenuItem("Eco Mode", null, (s, e) => ToggleEcoMode());
            ecoMenu.Checked = _settings.EcoMode;
            menu.Items.Add(ecoMenu);

            var autostartMenu = new ToolStripMenuItem("Start with Windows", null, (s, e) => ToggleAutostart());
            autostartMenu.Checked = _settings.StartWithWindows;
            menu.Items.Add(autostartMenu);

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Open Logs", null, (s, e) => Process.Start("notepad.exe", Logger.GetLogPath()));
            menu.Items.Add("About", null, (s, e) => MessageBox.Show("CPUIdleStabliser v1.0\n\nMaintains a low steady CPU load to prevent CPU idle instability.", "About"));
            menu.Items.Add("Exit", null, (s, e) => Exit());

            menu.Opening += (s, e) => 
            {
                startStopItem.Text = _controller.IsRunning ? "Stop" : "Start";
                ecoMenu.Checked = _settings.EcoMode;
                autostartMenu.Checked = _settings.StartWithWindows;
            };

            return menu;
        }

        private void ToggleRunning()
        {
            if (_controller.IsRunning) _controller.Stop();
            else _controller.Start(_settings.TargetTotalPercent, _settings.EcoMode);
            Logger.Log(_controller.IsRunning ? "Load started manually." : "Load stopped manually.");
        }

        private void SetTarget(double target)
        {
            _settings.TargetTotalPercent = target;
            SettingsManager.Save(_settings);
            _controller.UpdateSettings(target, _settings.EcoMode);
            Logger.Log($"Target updated to {target}%.");
        }

        private void ToggleEcoMode()
        {
            _settings.EcoMode = !_settings.EcoMode;
            SettingsManager.Save(_settings);
            _controller.UpdateSettings(_settings.TargetTotalPercent, _settings.EcoMode);
            Logger.Log($"Eco Mode toggled: {_settings.EcoMode}");
        }

        private void ToggleAutostart()
        {
            _settings.StartWithWindows = !_settings.StartWithWindows;
            SettingsManager.Save(_settings);
            SetAutostart(_settings.StartWithWindows);
            Logger.Log($"Autostart toggled: {_settings.StartWithWindows}");
        }

        private void SetAutostart(bool enable)
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
                         // Clean up both old and new keys if present
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

        private void UpdateTooltip(object? sender, MouseEventArgs e)
        {
            _trayIcon.Text = $"CPUIdleStabliser\n" +
                            $"Target: {_settings.TargetTotalPercent}%\n" +
                            $"Cores: {_controller.CoreCount}\n" +
                            $"Status: {(_controller.IsRunning ? "Running" : "Stopped")}";
        }

        private void CheckStatus()
        {
            // Re-start if it should be running but isn't
            if (_settings.TargetTotalPercent > 0 && !_controller.IsRunning && _controller.IsRunning /* wait, logic flaw in previous code? */)
            {
               // This logic was "If expected to be running but isn't".
               // How do we know if it *expected* to be running? We don't verify explicitly other than user intent.
               // Previous logic: if (_settings.TargetTotalPercent > 0 && !_controller.IsRunning) -> This implies ALWAYS running if target > 0?
               // That seems wrong if we have a Stop button.
               // I'll leave it as is if it was intended behavior, but user added a Stop button recently.
               // If user manually stopped, we shouldn't auto-start.
               // So I'll remove this aggressive auto-restart logic unless I track "ExpectedRunning" state.
               // For now, I'll remove it to avoid zombie restarts.
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        private Icon GetAppIcon()
        {
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_icon.ico");
                if (File.Exists(iconPath))
                {
                    return new Icon(iconPath);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Icon load failed: {ex.Message}. Using fallback.");
            }
            return SystemIcons.Shield; 
        }
        private void ShowSettings()
        {
            if (_settingsForm == null || _settingsForm.IsDisposed)
            {
                // Fix argument order
                _settingsForm = new SettingsForm(_controller, _settings);
            }
            
            if (!_settingsForm.Visible)
            {
                _settingsForm.Show();
            }
            _settingsForm.BringToFront();
        }
        private void Exit()
        {
            _trayIcon.Visible = false;
            _controller.Stop();
            Logger.Log("App exiting.");
            Application.Exit();
        }
    }
}
