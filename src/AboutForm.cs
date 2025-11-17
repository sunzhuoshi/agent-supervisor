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
            this.Text = $"{Localization.GetString("AboutFormTitle")} {Constants.ApplicationName}";
            this.Size = new Size(Constants.AboutFormWidth, Constants.AboutFormHeight);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ShowInTaskbar = false;

            // Application Name Label
            var appNameLabel = new Label
            {
                Text = Constants.ApplicationName,
                Location = new Point(20, 20),
                Size = new Size(410, 30),
                Font = new Font(this.Font.FontFamily, 16, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(appNameLabel);

            // Version Label
            var assembly = Assembly.GetExecutingAssembly();
            var infoVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            var version = assembly.GetName().Version;
            var versionText = !string.IsNullOrEmpty(infoVersion) ? infoVersion : version?.ToString() ?? "Unknown";
            
            var versionLabel = new Label
            {
                Text = $"{Localization.GetString("AboutVersion")} {versionText}",
                Location = new Point(20, 55),
                Size = new Size(410, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(versionLabel);

            // Description Label
            var descriptionLabel = new Label
            {
                Text = Localization.GetString("AboutDescription"),
                Location = new Point(20, 90),
                Size = new Size(410, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(descriptionLabel);

            // Copyright Label
            var copyrightLabel = new Label
            {
                Text = $"© {DateTime.Now.Year} {Localization.GetString("AboutCopyright")}",
                Location = new Point(20, 140),
                Size = new Size(410, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(copyrightLabel);

            // GitHub Link Label
            var githubLinkLabel = new LinkLabel
            {
                Text = Constants.GitHubRepoUrl.Replace("https://", ""),
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
                        FileName = Constants.GitHubRepoUrl,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening link: {ex.Message}", Constants.MessageBoxTitleError,
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            this.Controls.Add(githubLinkLabel);

            // OK Button
            var okButton = new Button
            {
                Text = Localization.GetString("AboutButtonOK"),
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
