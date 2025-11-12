using System;
using System.Windows.Forms;
using System.Drawing;
using System.Reflection;

namespace AgentSupervisor
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Text = "About Agent Supervisor";
            this.Size = new Size(450, 300);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Application Name Label
            var appNameLabel = new Label
            {
                Text = "Agent Supervisor",
                Location = new Point(20, 20),
                Size = new Size(410, 30),
                Font = new Font(this.Font.FontFamily, 16, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(appNameLabel);

            // Version Label
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var versionLabel = new Label
            {
                Text = $"Version {version?.Major}.{version?.Minor}.{version?.Build}",
                Location = new Point(20, 55),
                Size = new Size(410, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(versionLabel);

            // Description Label
            var descriptionLabel = new Label
            {
                Text = "A Windows system tray application that monitors\nGitHub pull request reviews and sends desktop notifications.",
                Location = new Point(20, 90),
                Size = new Size(410, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(descriptionLabel);

            // Copyright Label
            var copyrightLabel = new Label
            {
                Text = $"© {DateTime.Now.Year} Agent Supervisor Contributors",
                Location = new Point(20, 140),
                Size = new Size(410, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(copyrightLabel);

            // GitHub Link Label
            var githubLinkLabel = new LinkLabel
            {
                Text = "github.com/sunzhuoshi/AgentSupervisor",
                Location = new Point(20, 170),
                Size = new Size(410, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            githubLinkLabel.LinkClicked += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "https://github.com/sunzhuoshi/AgentSupervisor",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening link: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            this.Controls.Add(githubLinkLabel);

            // OK Button
            var okButton = new Button
            {
                Text = "OK",
                Location = new Point(175, 210),
                Size = new Size(100, 30),
                DialogResult = DialogResult.OK
            };
            this.Controls.Add(okButton);

            this.AcceptButton = okButton;
            this.CancelButton = okButton;
        }
    }
}
