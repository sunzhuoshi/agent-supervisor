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
        private CheckBox _useProxyCheckBox = null!;
        private TextBox _proxyUrlTextBox = null!;
        private TextBox _customIconTextBox = null!;
        private Button _browseIconButton = null!;
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
            this.Text = "Agent Supervisor - Settings";
            this.Size = new System.Drawing.Size(500, 470);
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

            // Custom Icon Label
            var iconLabel = new Label
            {
                Text = "Custom Tray Icon (.ico file):",
                Location = new System.Drawing.Point(20, 180),
                Size = new System.Drawing.Size(200, 20)
            };
            this.Controls.Add(iconLabel);

            // Custom Icon TextBox
            _customIconTextBox = new TextBox
            {
                Location = new System.Drawing.Point(20, 205),
                Size = new System.Drawing.Size(350, 25),
                ReadOnly = true
            };
            this.Controls.Add(_customIconTextBox);

            // Browse Icon Button
            _browseIconButton = new Button
            {
                Text = "Browse...",
                Location = new System.Drawing.Point(380, 203),
                Size = new System.Drawing.Size(90, 27)
            };
            _browseIconButton.Click += BrowseIconButton_Click;
            this.Controls.Add(_browseIconButton);

            // Clear Icon Button
            var clearIconButton = new Button
            {
                Text = "Clear",
                Location = new System.Drawing.Point(20, 237),
                Size = new System.Drawing.Size(75, 25)
            };
            clearIconButton.Click += (s, e) => _customIconTextBox.Text = string.Empty;
            this.Controls.Add(clearIconButton);

            // Proxy Group
            var proxyGroup = new GroupBox
            {
                Text = "Proxy Settings",
                Location = new System.Drawing.Point(20, 275),
                Size = new System.Drawing.Size(450, 100)
            };
            this.Controls.Add(proxyGroup);

            // Use Proxy CheckBox
            _useProxyCheckBox = new CheckBox
            {
                Text = "Use Proxy",
                Location = new System.Drawing.Point(10, 25),
                Size = new System.Drawing.Size(150, 20)
            };
            _useProxyCheckBox.CheckedChanged += (s, e) =>
            {
                _proxyUrlTextBox.Enabled = _useProxyCheckBox.Checked;
            };
            proxyGroup.Controls.Add(_useProxyCheckBox);

            // Proxy URL Label
            var proxyLabel = new Label
            {
                Text = "Proxy URL (e.g., http://proxy:8080):",
                Location = new System.Drawing.Point(10, 50),
                Size = new System.Drawing.Size(430, 20)
            };
            proxyGroup.Controls.Add(proxyLabel);

            // Proxy URL TextBox
            _proxyUrlTextBox = new TextBox
            {
                Location = new System.Drawing.Point(10, 70),
                Size = new System.Drawing.Size(430, 25),
                Enabled = false
            };
            proxyGroup.Controls.Add(_proxyUrlTextBox);

            // Save Button
            _saveButton = new Button
            {
                Text = "Save",
                Location = new System.Drawing.Point(290, 395),
                Size = new System.Drawing.Size(85, 30),
                DialogResult = DialogResult.OK
            };
            _saveButton.Click += SaveButton_Click;
            this.Controls.Add(_saveButton);

            // Cancel Button
            _cancelButton = new Button
            {
                Text = "Cancel",
                Location = new System.Drawing.Point(385, 395),
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
            _customIconTextBox.Text = _configuration.CustomIconPath;
            _useProxyCheckBox.Checked = _configuration.UseProxy;
            _proxyUrlTextBox.Text = _configuration.ProxyUrl;
            _proxyUrlTextBox.Enabled = _configuration.UseProxy;
        }

        private void BrowseIconButton_Click(object? sender, EventArgs e)
        {
            using var openFileDialog = new OpenFileDialog
            {
                Title = "Select Custom Icon",
                Filter = "Icon Files (*.ico)|*.ico|All Files (*.*)|*.*",
                FilterIndex = 1,
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                _customIconTextBox.Text = openFileDialog.FileName;
            }
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

            if (_useProxyCheckBox.Checked && string.IsNullOrWhiteSpace(_proxyUrlTextBox.Text))
            {
                MessageBox.Show("Proxy URL is required when proxy is enabled.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.None;
                return;
            }

            // Validate custom icon path if provided
            if (!string.IsNullOrWhiteSpace(_customIconTextBox.Text) && !File.Exists(_customIconTextBox.Text))
            {
                var result = MessageBox.Show(
                    "The custom icon file does not exist. Do you want to continue without a custom icon?",
                    "Icon File Not Found",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                
                if (result == DialogResult.No)
                {
                    this.DialogResult = DialogResult.None;
                    return;
                }
                
                _customIconTextBox.Text = string.Empty;
            }

            _configuration.PersonalAccessToken = _tokenTextBox.Text.Trim();
            _configuration.PollingIntervalSeconds = (int)_intervalNumeric.Value;
            _configuration.MaxHistoryEntries = (int)_maxHistoryNumeric.Value;
            _configuration.CustomIconPath = _customIconTextBox.Text.Trim();
            _configuration.UseProxy = _useProxyCheckBox.Checked;
            _configuration.ProxyUrl = _proxyUrlTextBox.Text.Trim();
            _configuration.Save();

            MessageBox.Show("Settings saved successfully.", "Success", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
