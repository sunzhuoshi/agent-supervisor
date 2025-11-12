using System;
using System.Windows.Forms;

namespace GitHubCopilotAgentBot
{
    public partial class SettingsForm : Form
    {
        private readonly Configuration _configuration;
        private TextBox _tokenTextBox = null!;
        private NumericUpDown _intervalNumeric = null!;
        private NumericUpDown _maxHistoryNumeric = null!;
        private Button _saveButton = null!;
        private Button _cancelButton = null!;

        public SettingsForm(Configuration configuration)
        {
            _configuration = configuration;
            InitializeComponents();
            LoadSettings();
        }

        private void InitializeComponents()
        {
            this.Text = "GitHub Copilot Agent Bot - Settings";
            this.Size = new System.Drawing.Size(500, 300);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Token Label
            var tokenLabel = new Label
            {
                Text = "GitHub Personal Access Token:",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(450, 20)
            };
            this.Controls.Add(tokenLabel);

            // Token TextBox
            _tokenTextBox = new TextBox
            {
                Location = new System.Drawing.Point(20, 45),
                Size = new System.Drawing.Size(450, 25),
                UseSystemPasswordChar = true
            };
            this.Controls.Add(_tokenTextBox);

            // Show/Hide Token CheckBox
            var showTokenCheckBox = new CheckBox
            {
                Text = "Show token",
                Location = new System.Drawing.Point(20, 75),
                Size = new System.Drawing.Size(150, 20)
            };
            showTokenCheckBox.CheckedChanged += (s, e) =>
            {
                _tokenTextBox.UseSystemPasswordChar = !showTokenCheckBox.Checked;
            };
            this.Controls.Add(showTokenCheckBox);

            // Polling Interval Label
            var intervalLabel = new Label
            {
                Text = "Polling Interval (seconds):",
                Location = new System.Drawing.Point(20, 110),
                Size = new System.Drawing.Size(200, 20)
            };
            this.Controls.Add(intervalLabel);

            // Polling Interval NumericUpDown
            _intervalNumeric = new NumericUpDown
            {
                Location = new System.Drawing.Point(220, 108),
                Size = new System.Drawing.Size(100, 25),
                Minimum = 10,
                Maximum = 3600,
                Value = 60
            };
            this.Controls.Add(_intervalNumeric);

            // Max History Label
            var historyLabel = new Label
            {
                Text = "Max History Entries:",
                Location = new System.Drawing.Point(20, 145),
                Size = new System.Drawing.Size(200, 20)
            };
            this.Controls.Add(historyLabel);

            // Max History NumericUpDown
            _maxHistoryNumeric = new NumericUpDown
            {
                Location = new System.Drawing.Point(220, 143),
                Size = new System.Drawing.Size(100, 25),
                Minimum = 10,
                Maximum = 1000,
                Value = 100
            };
            this.Controls.Add(_maxHistoryNumeric);

            // Save Button
            _saveButton = new Button
            {
                Text = "Save",
                Location = new System.Drawing.Point(290, 220),
                Size = new System.Drawing.Size(85, 30),
                DialogResult = DialogResult.OK
            };
            _saveButton.Click += SaveButton_Click;
            this.Controls.Add(_saveButton);

            // Cancel Button
            _cancelButton = new Button
            {
                Text = "Cancel",
                Location = new System.Drawing.Point(385, 220),
                Size = new System.Drawing.Size(85, 30),
                DialogResult = DialogResult.Cancel
            };
            this.Controls.Add(_cancelButton);

            this.AcceptButton = _saveButton;
            this.CancelButton = _cancelButton;
        }

        private void LoadSettings()
        {
            _tokenTextBox.Text = _configuration.PersonalAccessToken;
            _intervalNumeric.Value = _configuration.PollingIntervalSeconds;
            _maxHistoryNumeric.Value = _configuration.MaxHistoryEntries;
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_tokenTextBox.Text))
            {
                MessageBox.Show("Personal Access Token is required.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.None;
                return;
            }

            _configuration.PersonalAccessToken = _tokenTextBox.Text.Trim();
            _configuration.PollingIntervalSeconds = (int)_intervalNumeric.Value;
            _configuration.MaxHistoryEntries = (int)_maxHistoryNumeric.Value;
            _configuration.Save();

            MessageBox.Show("Settings saved successfully.", "Success", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
