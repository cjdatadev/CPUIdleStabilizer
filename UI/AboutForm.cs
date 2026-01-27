using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.IO;
using CPUIdleStabilizer.Infra;

namespace CPUIdleStabilizer.UI
{
    public class AboutForm : Form
    {
        public AboutForm(Icon appIcon)
        {
            InitializeComponent(appIcon);
        }

        private void InitializeComponent(Icon appIcon)
        {
            this.Text = "About CPUIdleStabilizer";
            this.Size = new Size(400, 360);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = appIcon;
            this.BackColor = Color.White;

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                RowCount = 6,
                ColumnCount = 1
            };

            // Version info
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.2";
            var date = "2026-01-27"; // Hardcoded for single-file compatibility

            var titleLabel = new Label
            {
                Text = "CPUIdleStabilizer",
                Font = new Font(this.Font.FontFamily, 14, FontStyle.Bold),
                AutoSize = true,
                Anchor = AnchorStyles.None,
                Margin = new Padding(0, 0, 0, 5)
            };

            var versionLabel = new Label
            {
                Text = $"v{version} (Released: {date})",
                AutoSize = true,
                Anchor = AnchorStyles.None,
                Margin = new Padding(0, 0, 0, 15)
            };

            var descriptionLabel = new Label
            {
                Text = "Maintains a low steady CPU load to prevent idle instability.\nDeveloped by cjdatadev. MIT License.",
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = true,
                Anchor = AnchorStyles.None,
                Margin = new Padding(0, 0, 0, 20)
            };

            var helpLabel = new Label
            {
                Text = "If this has helped - be sure to give me a star on GitHub!",
                Font = new Font(this.Font, FontStyle.Italic),
                ForeColor = Color.DarkSlateBlue,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = true,
                Anchor = AnchorStyles.None,
                Margin = new Padding(0, 0, 0, 10)
            };

            var githubLink = new LinkLabel
            {
                Text = "GitHub Repository",
                AutoSize = true,
                Anchor = AnchorStyles.None,
                Margin = new Padding(0, 0, 0, 5)
            };
            githubLink.Click += (s, e) => OpenUrl("https://github.com/cjdatadev/CPUIdleStabilizer/");

            var redditLink = new LinkLabel
            {
                Text = "Reddit Thread (AMDHelp)",
                AutoSize = true,
                Anchor = AnchorStyles.None,
                Margin = new Padding(0, 0, 0, 20)
            };
            redditLink.Click += (s, e) => OpenUrl("https://www.reddit.com/r/AMDHelp/comments/1qmag98/ryzen_5800x_b550_idle_crash_issue_created_a/");

            var closeButton = new Button
            {
                Text = "Close",
                DialogResult = DialogResult.OK,
                Width = 90,
                Height = 30,
                FlatStyle = FlatStyle.System
            };

            var removeButton = new Button
            {
                Text = "Remove Application",
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.Firebrick,
                Cursor = Cursors.Hand,
                Height = 30
            };
            removeButton.FlatAppearance.BorderSize = 0;
            removeButton.Click += (s, e) => HandleRemoveApplication();

            var buttonPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Height = 40,
                Margin = new Padding(0, 15, 0, 0)
            };
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            
            buttonPanel.Controls.Add(removeButton, 0, 0);
            buttonPanel.Controls.Add(closeButton, 1, 0);
            
            removeButton.Anchor = AnchorStyles.Left;
            closeButton.Anchor = AnchorStyles.Right;

            mainLayout.Controls.Add(titleLabel);
            mainLayout.Controls.Add(versionLabel);
            mainLayout.Controls.Add(descriptionLabel);
            mainLayout.Controls.Add(helpLabel);
            
            var linkPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                Anchor = AnchorStyles.None
            };
            linkPanel.Controls.Add(githubLink);
            linkPanel.Controls.Add(redditLink);
            mainLayout.Controls.Add(linkPanel);

            mainLayout.Controls.Add(buttonPanel);

            this.Controls.Add(mainLayout);
            this.AcceptButton = closeButton;
        }

        private void HandleRemoveApplication()
        {
            var confirm1 = MessageBox.Show(
                "Are you sure you want to remove all settings, logs, and autostart configuration?",
                "Remove Application",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm1 != DialogResult.Yes) return;

            var confirm2 = MessageBox.Show(
                "This will stop the service and clean up all files, including those in AppData.\n\n" +
                "If this app was 'installed' to AppData, it will delete itself completely after closing.\n\n" +
                "Continue?",
                "Final Confirmation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm2 == DialogResult.Yes)
            {
                SettingsManager.Uninstall();
                Application.Exit();
            }
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open link: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
