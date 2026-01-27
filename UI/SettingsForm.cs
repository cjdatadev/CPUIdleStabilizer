using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using CPUIdleStabilizer.Core;
using CPUIdleStabilizer.Infra;

namespace CPUIdleStabilizer.UI
{
    public class SettingsForm : Form
    {
        private readonly LoadController _controller;
        private readonly UserSettings _settings;

        private Label _statusLabel;
        private Label _cpuUsageLabel;
        private TrackBar _targetSlider;
        private Label _targetLabel;
        private CheckBox _ecoCheckBox;
        private CheckBox _autostartCheckBox;
        private CheckBox _startMinimizedCheckBox;
        private Button _toggleButton;
        private Button _aboutButton;
        private Button _hideButton;

        private System.Windows.Forms.Timer _uiTimer;

        public SettingsForm(LoadController controller, UserSettings settings, Icon appIcon)
        {
            _controller = controller;
            _settings = settings;

            InitializeComponent();
            
            this.Icon = appIcon;
            
            // Initial UI Update
            UpdateStatus();

            // Check for autostart migration if enabled but not installed
            if (_settings.StartWithWindows && !SettingsManager.IsRunningFromInstallFolder)
            {
                this.Load += (s, e) => {
                    // Slight delay to ensure form is visible
                    System.Windows.Forms.Timer migrationDelay = new System.Windows.Forms.Timer { Interval = 500 };
                    migrationDelay.Tick += (st, et) => {
                        migrationDelay.Stop();
                        HandleAutostartChanged();
                    };
                    migrationDelay.Start();
                };
            }
            
            // UI Update Timer
            _uiTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _uiTimer.Tick += (s, e) => UpdateCpuUsage();
            _uiTimer.Start();
        }

        private void InitializeComponent()
        {
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.MinimumSize = new Size(450, 0);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
            this.Text = $"CPUIdleStabilizer v{version}";

            if (this.Icon == null) this.Icon = SystemIcons.Shield;

            // Main Table Layout
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 6,
                ColumnCount = 1,
                Padding = new Padding(20),
                AutoSize = true
            };
            
            // Row Styles
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Status
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Usage
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Slider
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Checkboxes
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Buttons (Fill remaining)
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Hide Button

            // 1. Status Label
            _statusLabel = new Label
            {
                Text = "Status: STOPPED",
                AutoSize = false,
                Dock = DockStyle.Fill,
                Height = 30,
                Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 10, FontStyle.Bold),
                ForeColor = Color.DarkRed,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoEllipsis = true,
                Margin = new Padding(0, 0, 0, 10)
            };
            mainLayout.Controls.Add(_statusLabel, 0, 0);

            // 2. CPU Usage Label
            _cpuUsageLabel = new Label
            {
                Text = "Current App CPU Usage: 0.0%",
                AutoSize = false,
                Dock = DockStyle.Fill,
                Height = 30,
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoEllipsis = true
            };
            mainLayout.Controls.Add(_cpuUsageLabel, 0, 1);

            // 3. Slider Section (Table for Label + Slider + Value)
            var sliderPanel = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 2,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 20, 0, 0)
            };
            sliderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            sliderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var headerLabel = new Label
            {
                Text = "Target CPU Load",
                AutoSize = true,
                Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 5)
            };
            sliderPanel.Controls.Add(headerLabel, 0, 0);
            sliderPanel.SetColumnSpan(headerLabel, 2);

            _targetSlider = new TrackBar
            {
                Minimum = 1,
                Maximum = 20,
                Value = (int)(_settings.TargetTotalPercent * 2),
                TickStyle = TickStyle.None,
                Dock = DockStyle.Fill
            };
            
            _targetLabel = new Label
            {
                Text = $"{_settings.TargetTotalPercent:F1}%",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(0, 5, 0, 0)
            };

            sliderPanel.Controls.Add(_targetSlider, 0, 1);
            sliderPanel.Controls.Add(_targetLabel, 1, 1);
            
            _targetSlider.ValueChanged += (s, e) =>
            {
                double target = _targetSlider.Value / 2.0;
                _targetLabel.Text = $"{target:F1}%";
                _settings.TargetTotalPercent = target;
                SettingsManager.Save(_settings);
                if (_controller.IsRunning) ApplySettings();
            };

            mainLayout.Controls.Add(sliderPanel, 0, 2);

            // 4. Checkboxes
            var checkPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 15, 0, 15)
            };

            _ecoCheckBox = new CheckBox
            {
                Text = "Eco Mode (Jitter)",
                Checked = _settings.EcoMode,
                AutoSize = true
            };
            _ecoCheckBox.Click += (s, e) => 
            { 
                _settings.EcoMode = _ecoCheckBox.Checked;
                SettingsManager.Save(_settings);
                if (_controller.IsRunning) ApplySettings(); 
            };

            var autostartFlow = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };

            _autostartCheckBox = new CheckBox 
            { 
                Text = "Start with Windows", 
                Checked = _settings.StartWithWindows, 
                AutoSize = true 
            };
            _autostartCheckBox.Click += (s, e) => 
            {
                _settings.StartWithWindows = _autostartCheckBox.Checked;
                SettingsManager.Save(_settings);
                SettingsManager.SetAutostart(_settings.StartWithWindows, _settings.StartMinimized);
                HandleAutostartChanged();
            };
            
            var infoButton = new Button
            {
                Text = "?",
                Width = 25,
                Height = 25,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(5, 0, 0, 0)
            };
            infoButton.FlatAppearance.BorderSize = 1;
            infoButton.Click += (s, e) => ShowAutostartHelp();

            autostartFlow.Controls.Add(_autostartCheckBox);
            autostartFlow.Controls.Add(infoButton);

            _startMinimizedCheckBox = new CheckBox
            {
                Text = "Start Minimized (System Tray)",
                Checked = _settings.StartMinimized,
                AutoSize = true,
                Margin = new Padding(20, 0, 0, 0)
            };
            _startMinimizedCheckBox.Enabled = _settings.StartWithWindows;
            _startMinimizedCheckBox.Click += (s, e) => 
            {
                _settings.StartMinimized = _startMinimizedCheckBox.Checked;
                SettingsManager.Save(_settings);
                SettingsManager.SetAutostart(_settings.StartWithWindows, _settings.StartMinimized);
            };

            checkPanel.Controls.Add(_ecoCheckBox);
            checkPanel.Controls.Add(autostartFlow);
            checkPanel.Controls.Add(_startMinimizedCheckBox);
            mainLayout.Controls.Add(checkPanel, 0, 3);

            // 5. Buttons Section (Start/Stop and About)
            var buttonPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                AutoSize = true
            };
            buttonPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Big Toggle Button
            buttonPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // About Button

            _toggleButton = new Button
            {
                Text = "START",
                Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 14, FontStyle.Bold),
                Height = 60,
                Dock = DockStyle.Top,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.ForestGreen,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            _toggleButton.Click += (s, e) => ToggleStartStop();
            buttonPanel.Controls.Add(_toggleButton, 0, 0);

            _aboutButton = new Button
            {
                Text = "About",
                AutoSize = true,
                Dock = DockStyle.Top,
                FlatStyle = FlatStyle.System,
                Margin = new Padding(0, 10, 0, 0)
            };
            _aboutButton.Click += (s, e) => new AboutForm(this.Icon).ShowDialog();
            buttonPanel.Controls.Add(_aboutButton, 0, 1);

            mainLayout.Controls.Add(buttonPanel, 0, 4);

            // 6. Hide Button
            _hideButton = new Button
            {
                Text = "Hide to Tray",
                Dock = DockStyle.Bottom,
                Height = 30
            };
            _hideButton.Click += (s, e) => this.Close(); // TrayAppContext handles Hide on Close
            mainLayout.Controls.Add(_hideButton, 0, 5);

            this.Controls.Add(mainLayout);
        }

        private void ToggleStartStop()
        {
            if (_controller.IsRunning)
            {
                _controller.Stop();
            }
            else
            {
                ApplySettings();
                _controller.Start(_settings.TargetTotalPercent, _settings.EcoMode);
            }
            UpdateStatus();
        }

        private void ApplySettings()
        {
             double target = _targetSlider.Value / 2.0;
             bool eco = _ecoCheckBox.Checked;
             _controller.UpdateSettings(target, eco);
        }

        private void UpdateStatus()
        {
            if (_controller.IsRunning)
            {
                _statusLabel.Text = $"Status: RUNNING ({_controller.CoreCount} threads)";
                _statusLabel.ForeColor = Color.DarkGreen;
                _toggleButton.Text = "STOP";
                _toggleButton.BackColor = Color.Firebrick;
            }
            else
            {
                _statusLabel.Text = "Status: STOPPED";
                _statusLabel.ForeColor = Color.DarkRed;
                _toggleButton.Text = "START";
                _toggleButton.BackColor = Color.ForestGreen;
            }
            SyncSettingsToUI();
            UpdateCpuUsage();
        }

        private void SyncSettingsToUI()
        {
            // Sync UI controls with current settings (in case they changed via tray menu)
            if (_targetSlider.Value != (int)(_settings.TargetTotalPercent * 2))
            {
                _targetSlider.Value = (int)(_settings.TargetTotalPercent * 2);
                _targetLabel.Text = $"{_settings.TargetTotalPercent:F1}%";
            }
            
            if (_ecoCheckBox.Checked != _settings.EcoMode)
            {
                _ecoCheckBox.Checked = _settings.EcoMode;
            }

            if (_autostartCheckBox.Checked != _settings.StartWithWindows)
            {
                _autostartCheckBox.Checked = _settings.StartWithWindows;
                _startMinimizedCheckBox.Enabled = _settings.StartWithWindows;
            }

            if (_startMinimizedCheckBox.Checked != _settings.StartMinimized)
            {
                _startMinimizedCheckBox.Checked = _settings.StartMinimized;
            }
        }

        private DateTime _lastCheckTime = DateTime.UtcNow;
        private TimeSpan _lastProcessorTime;
        private readonly Process _currentProcess = Process.GetCurrentProcess();

        private void UpdateCpuUsage()
        {
            try 
            {
                var currentTime = DateTime.UtcNow;
                var currentProcessorTime = _currentProcess.TotalProcessorTime;
                
                var timeDiff = (currentTime - _lastCheckTime).TotalMilliseconds;
                var processorDiff = (currentProcessorTime - _lastProcessorTime).TotalMilliseconds;
                
                if (timeDiff > 0)
                {
                    double cpuUsage = (processorDiff / timeDiff) / Environment.ProcessorCount * 100.0;
                    _cpuUsageLabel.Text = $"Current App CPU Usage: {cpuUsage:F1}%";
                }
                
                _lastCheckTime = currentTime;
                _lastProcessorTime = currentProcessorTime;
            }
            catch 
            {
                _cpuUsageLabel.Text = "Current App CPU Usage: -";
            }
        }

        private void HandleAutostartChanged()
        {
            _startMinimizedCheckBox.Enabled = _autostartCheckBox.Checked;
            
            if (_autostartCheckBox.Checked && !SettingsManager.IsRunningFromInstallFolder)
            {
                var result = MessageBox.Show(
                    "To ensure the autostart link doesn't break if you move this file, the app can copy itself to your local AppData folder.\n\n" +
                    "Would you like to 'install' it to AppData now?",
                    "Stable Autostart Recommended",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    MigrateToAppData();
                }
            }
        }

        private void ShowAutostartHelp()
        {
            MessageBox.Show(
                "Autostart Features Explained:\n\n" +
                "1. Start with Windows: Automatically launches the app when you log in.\n\n" +
                "2. Self-Installation: If you enable autostart, the app copies itself to %AppData%\\CPUIdleStabilizer.\\bin\\ so the shortcut remains valid even if you move this original executable.\n\n" +
                "3. Start Minimized: Launches the app directly to the system tray (near the clock) without opening this window.",
                "Autostart Help",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void MigrateToAppData()
        {
            try
            {
                string targetDir = SettingsManager.InstallFolder;
                Directory.CreateDirectory(targetDir);
                
                string exeName = Path.GetFileName(Application.ExecutablePath);
                string targetPath = Path.Combine(targetDir, exeName);

                // Copy ourselves if not there or different
                if (!File.Exists(targetPath) || File.GetLastWriteTime(Application.ExecutablePath) > File.GetLastWriteTime(targetPath))
                {
                    File.Copy(Application.ExecutablePath, targetPath, true);
                }

                // Update registry to point to the NEW stable location
                SettingsManager.SetAutostart(true, _startMinimizedCheckBox.Checked, targetPath);

                MessageBox.Show(
                    $"Succesfully copied to:\n{targetPath}\n\nAutostart has been configured to use this stable location.",
                    "Migration Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy to AppData: {ex.Message}", "Migration Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _autostartCheckBox.Checked = false;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Remove UserClosing check to ensure we save on Windows shutdown/logoff too
            _settings.TargetTotalPercent = _targetSlider.Value / 2.0;
            _settings.EcoMode = _ecoCheckBox.Checked;
            _settings.StartWithWindows = _autostartCheckBox.Checked;
            _settings.StartMinimized = _startMinimizedCheckBox.Checked;
            
            SettingsManager.Save(_settings);
            SettingsManager.SetAutostart(_settings.StartWithWindows, _settings.StartMinimized);
            base.OnFormClosing(e);
        }
    }
}
