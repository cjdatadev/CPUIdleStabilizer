using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

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
            this.Size = new Size(400, 320);
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
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.1";
            var date = System.IO.File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location).ToShortDateString();

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
                Anchor = AnchorStyles.None,
                Width = 80
            };

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
            
            mainLayout.Controls.Add(closeButton);

            this.Controls.Add(mainLayout);
            this.AcceptButton = closeButton;
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
