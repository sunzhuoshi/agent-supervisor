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
        private CheckBox _enableNotificationsCheckBox = null!;
        private Button _saveButton = null!;
        private Button _cancelButton = null!;
        private Button _fontSelectButton = null!;
        private Label _fontDisplayLabel = null!;
        private Font? _selectedFont;

        public SettingsForm(Configuration configuration)
        {
            _configuration = configuration;
            InitializeComponents();
            LoadSettings();
        }

        private void InitializeComponents()
        {
            this.Text = "Agent Supervisor - Settings";
            this.Size = new System.Drawing.Size(500, 520);
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

            // Enable Desktop Notifications CheckBox
            _enableNotificationsCheckBox = new CheckBox
            {
                Text = "Enable Desktop Notifications",
                Location = new System.Drawing.Point(20, 178),
                Size = new System.Drawing.Size(250, 20)
            };
            this.Controls.Add(_enableNotificationsCheckBox);

            // Font Selection Group
            var fontGroup = new GroupBox
            {
                Text = "List Font",
                Location = new System.Drawing.Point(20, 210),
                Size = new System.Drawing.Size(450, 90)
            };
            this.Controls.Add(fontGroup);

            // Font Display Label
            _fontDisplayLabel = new Label
            {
                Text = "Segoe UI, 9pt",
                Location = new System.Drawing.Point(10, 25),
                Size = new System.Drawing.Size(330, 30),
                AutoSize = false,
                BorderStyle = BorderStyle.Fixed3D,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };
            fontGroup.Controls.Add(_fontDisplayLabel);

            // Font Select Button
            _fontSelectButton = new Button
            {
                Text = "Select Font...",
                Location = new System.Drawing.Point(350, 25),
                Size = new System.Drawing.Size(90, 30)
            };
            _fontSelectButton.Click += FontSelectButton_Click;
            fontGroup.Controls.Add(_fontSelectButton);

            // Font Info Label
            var fontInfoLabel = new Label
            {
                Text = "This font will be used for the review request list items.",
                Location = new System.Drawing.Point(10, 60),
                Size = new System.Drawing.Size(430, 20),
                ForeColor = System.Drawing.Color.Gray
            };
            fontGroup.Controls.Add(fontInfoLabel);

            // Proxy Group
            var proxyGroup = new GroupBox
            {
                Text = "Proxy Settings",
                Location = new System.Drawing.Point(20, 310),
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
                Location = new System.Drawing.Point(290, 440),
                Size = new System.Drawing.Size(85, 30),
                DialogResult = DialogResult.OK
            };
            _saveButton.Click += SaveButton_Click;
            this.Controls.Add(_saveButton);

            // Cancel Button
            _cancelButton = new Button
            {
                Text = "Cancel",
                Location = new System.Drawing.Point(385, 440),
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
            _enableNotificationsCheckBox.Checked = _configuration.EnableDesktopNotifications;
            _useProxyCheckBox.Checked = _configuration.UseProxy;
            _proxyUrlTextBox.Text = _configuration.ProxyUrl;
            _proxyUrlTextBox.Enabled = _configuration.UseProxy;
            
            // Load font settings
            try
            {
                _selectedFont = new Font(_configuration.FontFamily, _configuration.FontSize);
                _fontDisplayLabel.Text = $"{_selectedFont.FontFamily.Name}, {_selectedFont.Size}pt";
            }
            catch
            {
                _selectedFont = new Font("Segoe UI", 9.0f);
                _fontDisplayLabel.Text = "Segoe UI, 9pt";
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

            _configuration.PersonalAccessToken = _tokenTextBox.Text.Trim();
            _configuration.PollingIntervalSeconds = (int)_intervalNumeric.Value;
            _configuration.MaxHistoryEntries = (int)_maxHistoryNumeric.Value;
            _configuration.EnableDesktopNotifications = _enableNotificationsCheckBox.Checked;
            _configuration.UseProxy = _useProxyCheckBox.Checked;
            _configuration.ProxyUrl = _proxyUrlTextBox.Text.Trim();
            
            // Save font settings
            if (_selectedFont != null)
            {
                _configuration.FontFamily = _selectedFont.FontFamily.Name;
                _configuration.FontSize = _selectedFont.Size;
            }
            
            _configuration.Save();

            MessageBox.Show("Settings saved successfully.", "Success", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void FontSelectButton_Click(object? sender, EventArgs e)
        {
            using var fontDialog = new FontDialog();
            
            if (_selectedFont != null)
            {
                fontDialog.Font = _selectedFont;
            }
            
            fontDialog.ShowEffects = false;
            fontDialog.ShowColor = false;
            fontDialog.MinSize = 7;
            fontDialog.MaxSize = 16;
            
            if (fontDialog.ShowDialog() == DialogResult.OK)
            {
                _selectedFont = fontDialog.Font;
                _fontDisplayLabel.Text = $"{_selectedFont.FontFamily.Name}, {_selectedFont.Size}pt";
            }
        }
    }
}
