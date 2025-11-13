using System;
using System.Windows.Forms;

namespace AgentSupervisor
{
    public partial class SettingsForm : Form
    {
        private readonly Configuration _configuration;
        private TextBox _tokenTextBox = null!;
        private NumericUpDown _intervalNumeric = null!;
        private NumericUpDown _maxHistoryNumeric = null!;
        private CheckBox _useProxyCheckBox = null!;
        private TextBox _proxyUrlTextBox = null!;
        private Button _saveButton = null!;
        private Button _cancelButton = null!;
        private Button _exportButton = null!;
        private Button _importButton = null!;

        public SettingsForm(Configuration configuration)
        {
            _configuration = configuration;
            InitializeComponents();
            LoadSettings();
        }

        private void InitializeComponents()
        {
            this.Text = "Agent Supervisor - Settings";
            this.Size = new System.Drawing.Size(500, 400);
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

            // Proxy Group
            var proxyGroup = new GroupBox
            {
                Text = "Proxy Settings",
                Location = new System.Drawing.Point(20, 180),
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

            // Export Button
            _exportButton = new Button
            {
                Text = "Export...",
                Location = new System.Drawing.Point(20, 320),
                Size = new System.Drawing.Size(85, 30)
            };
            _exportButton.Click += ExportButton_Click;
            this.Controls.Add(_exportButton);

            // Import Button
            _importButton = new Button
            {
                Text = "Import...",
                Location = new System.Drawing.Point(115, 320),
                Size = new System.Drawing.Size(85, 30)
            };
            _importButton.Click += ImportButton_Click;
            this.Controls.Add(_importButton);

            // Save Button
            _saveButton = new Button
            {
                Text = "Save",
                Location = new System.Drawing.Point(290, 320),
                Size = new System.Drawing.Size(85, 30),
                DialogResult = DialogResult.OK
            };
            _saveButton.Click += SaveButton_Click;
            this.Controls.Add(_saveButton);

            // Cancel Button
            _cancelButton = new Button
            {
                Text = "Cancel",
                Location = new System.Drawing.Point(385, 320),
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
            _useProxyCheckBox.Checked = _configuration.UseProxy;
            _proxyUrlTextBox.Text = _configuration.ProxyUrl;
            _proxyUrlTextBox.Enabled = _configuration.UseProxy;
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

            _configuration.PersonalAccessToken = _tokenTextBox.Text.Trim();
            _configuration.PollingIntervalSeconds = (int)_intervalNumeric.Value;
            _configuration.MaxHistoryEntries = (int)_maxHistoryNumeric.Value;
            _configuration.UseProxy = _useProxyCheckBox.Checked;
            _configuration.ProxyUrl = _proxyUrlTextBox.Text.Trim();
            _configuration.Save();

            MessageBox.Show("Settings saved successfully.", "Success", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ExportButton_Click(object? sender, EventArgs e)
        {
            using var saveDialog = new SaveFileDialog
            {
                Title = "Export Configuration",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json",
                FileName = "agent-supervisor-config.json"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var result = MessageBox.Show(
                        "Do you want to include the GitHub Personal Access Token in the export?\n\n" +
                        "Warning: The token will be stored in plain text in the exported file.",
                        "Include Token?",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Cancel)
                    {
                        return;
                    }

                    bool includeToken = result == DialogResult.Yes;
                    _configuration.ExportTo(saveDialog.FileName, includeToken);

                    MessageBox.Show(
                        $"Configuration exported successfully to:\n{saveDialog.FileName}",
                        "Export Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Failed to export configuration:\n{ex.Message}",
                        "Export Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private void ImportButton_Click(object? sender, EventArgs e)
        {
            using var openDialog = new OpenFileDialog
            {
                Title = "Import Configuration",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "json"
            };

            if (openDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var importedConfig = Configuration.ImportFrom(openDialog.FileName);

                    // Update UI with imported values
                    if (!string.IsNullOrWhiteSpace(importedConfig.PersonalAccessToken))
                    {
                        _tokenTextBox.Text = importedConfig.PersonalAccessToken;
                    }
                    _intervalNumeric.Value = importedConfig.PollingIntervalSeconds;
                    _maxHistoryNumeric.Value = importedConfig.MaxHistoryEntries;
                    _useProxyCheckBox.Checked = importedConfig.UseProxy;
                    _proxyUrlTextBox.Text = importedConfig.ProxyUrl;

                    MessageBox.Show(
                        "Configuration imported successfully.\n\nPlease review the settings and click Save to apply them.",
                        "Import Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Failed to import configuration:\n{ex.Message}",
                        "Import Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }
    }
}
