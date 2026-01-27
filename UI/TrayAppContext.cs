using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using CPUIdleStabilizer.Core;
using CPUIdleStabilizer.Infra;

namespace CPUIdleStabilizer.UI
{
    public class TrayAppContext : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;
        private readonly LoadController _controller;
        private readonly UserSettings _settings;
        private readonly System.Windows.Forms.Timer _statusTimer;
        private SettingsForm? _settingsForm;

        public TrayAppContext(LoadController controller, UserSettings settings, bool startHidden, bool isAutostart)
        {
            _controller = controller;
            _settings = settings;

            // Initialize Tray Icon with a custom icon from PNG if available
            _trayIcon = new NotifyIcon()
            {
                Icon = GetAppIcon(),
                Text = "CPUIdleStabilizer",
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

            // Always start the load if it was an autostart (regardless of minimized state)
            // or if we started hidden (legacy behavior, but still valid)
            if (isAutostart || startHidden)
            {
                if (_settings.TargetTotalPercent > 0 && _settings.StartWithWindows)
                {
                    _controller.Start(_settings.TargetTotalPercent, _settings.EcoMode);
                    Logger.Log($"App started load automatically. Autostart: {isAutostart}, Hidden: {startHidden}, Target: {_settings.TargetTotalPercent}%");
                }
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
            menu.Items.Add("About", null, (s, e) => new AboutForm(_trayIcon.Icon).ShowDialog());
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
            SettingsManager.SetAutostart(_settings.StartWithWindows, _settings.StartMinimized);
            Logger.Log($"Autostart toggled: {_settings.StartWithWindows}");
        }

        private void UpdateTooltip(object? sender, MouseEventArgs e)
        {
            _trayIcon.Text = $"CPUIdleStabilizer\n" +
                            $"Target: {_settings.TargetTotalPercent}%\n" +
                            $"Cores: {_controller.CoreCount}\n" +
                            $"Status: {(_controller.IsRunning ? "Running" : "Stopped")}";
        }

        private void CheckStatus()
        {
            // Simple check - logic can be refined later if needed
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        private Icon GetAppIcon()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                // Ensure the resource name matches the file location and namespace
                // Assuming the file is in the root and default namespace is CPUIdleStabilizer
                using var stream = assembly.GetManifestResourceStream("CPUIdleStabilizer.assets.app_icon.ico");
                if (stream != null)
                {
                    return new Icon(stream);
                }
                Logger.Log("Embedded icon resource 'CPUIdleStabilizer.assets.app_icon.ico' not found.");
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
                _settingsForm = new SettingsForm(_controller, _settings, _trayIcon.Icon);
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
