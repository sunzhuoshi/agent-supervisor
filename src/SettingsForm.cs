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
        private ComboBox _languageComboBox = null!;
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
            this.Text = $"{Constants.ApplicationName} - {Localization.GetString("SettingsFormTitle")}";
            this.Size = new System.Drawing.Size(Constants.SettingsFormWidth, Constants.SettingsFormHeight + 50);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Token Label
            var tokenLabel = new Label
            {
                Text = Localization.GetString("SettingsLabelToken"),
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
                Text = Localization.GetString("SettingsLabelShowToken"),
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
                Text = Localization.GetString("SettingsLabelPollingInterval"),
                Location = new System.Drawing.Point(20, 110),
                Size = new System.Drawing.Size(200, 20)
            };
            this.Controls.Add(intervalLabel);

            // Polling Interval NumericUpDown
            _intervalNumeric = new NumericUpDown
            {
                Location = new System.Drawing.Point(220, 108),
                Size = new System.Drawing.Size(100, 25),
                Minimum = Constants.SettingsPollingMinSeconds,
                Maximum = Constants.SettingsPollingMaxSeconds,
                Value = Constants.DefaultPollingIntervalSeconds
            };
            this.Controls.Add(_intervalNumeric);

            // Max History Label
            var historyLabel = new Label
            {
                Text = Localization.GetString("SettingsLabelMaxHistory"),
                Location = new System.Drawing.Point(20, 145),
                Size = new System.Drawing.Size(200, 20)
            };
            this.Controls.Add(historyLabel);

            // Max History NumericUpDown
            _maxHistoryNumeric = new NumericUpDown
            {
                Location = new System.Drawing.Point(220, 143),
                Size = new System.Drawing.Size(100, 25),
                Minimum = Constants.SettingsHistoryMinEntries,
                Maximum = Constants.SettingsHistoryMaxEntries,
                Value = Constants.DefaultMaxHistoryEntries
            };
            this.Controls.Add(_maxHistoryNumeric);

            // Enable Desktop Notifications CheckBox
            _enableNotificationsCheckBox = new CheckBox
            {
                Text = Localization.GetString("SettingsLabelEnableNotifications"),
                Location = new System.Drawing.Point(20, 178),
                Size = new System.Drawing.Size(250, 20)
            };
            this.Controls.Add(_enableNotificationsCheckBox);

            // Language Label
            var languageLabel = new Label
            {
                Text = Localization.GetString("SettingsLabelLanguage"),
                Location = new System.Drawing.Point(20, 210),
                Size = new System.Drawing.Size(200, 20)
            };
            this.Controls.Add(languageLabel);

            // Language ComboBox
            _languageComboBox = new ComboBox
            {
                Location = new System.Drawing.Point(220, 208),
                Size = new System.Drawing.Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            foreach (var (code, displayName) in Localization.AvailableLanguages)
            {
                _languageComboBox.Items.Add(new LanguageItem { Code = code, DisplayName = displayName });
            }
            _languageComboBox.DisplayMember = "DisplayName";
            this.Controls.Add(_languageComboBox);

            // Proxy Group
            var proxyGroup = new GroupBox
            {
                Text = Localization.GetString("SettingsLabelProxyGroup"),
                Location = new System.Drawing.Point(20, 245),
                Size = new System.Drawing.Size(450, 100)
            };
            this.Controls.Add(proxyGroup);

            // Use Proxy CheckBox
            _useProxyCheckBox = new CheckBox
            {
                Text = Localization.GetString("SettingsLabelUseProxy"),
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
                Text = Localization.GetString("SettingsLabelProxyUrl"),
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
                Text = Localization.GetString("SettingsButtonSave"),
                Location = new System.Drawing.Point(290, 405),
                Size = new System.Drawing.Size(85, 30),
                DialogResult = DialogResult.OK
            };
            _saveButton.Click += SaveButton_Click;
            this.Controls.Add(_saveButton);

            // Cancel Button
            _cancelButton = new Button
            {
                Text = Localization.GetString("SettingsButtonCancel"),
                Location = new System.Drawing.Point(385, 405),
                Size = new System.Drawing.Size(85, 30),
                DialogResult = DialogResult.Cancel
            };
            this.Controls.Add(_cancelButton);

            this.AcceptButton = _saveButton;
            this.CancelButton = _cancelButton;
        }

        private class LanguageItem
        {
            public string Code { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
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
            
            // Load language selection
            var currentLanguage = Localization.CurrentCultureName;
            for (int i = 0; i < _languageComboBox.Items.Count; i++)
            {
                if (_languageComboBox.Items[i] is LanguageItem item && item.Code == currentLanguage)
                {
                    _languageComboBox.SelectedIndex = i;
                    break;
                }
            }
            if (_languageComboBox.SelectedIndex == -1 && _languageComboBox.Items.Count > 0)
            {
                _languageComboBox.SelectedIndex = 0;
            }
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_tokenTextBox.Text))
            {
                MessageBox.Show(Constants.MessageTokenValidationFailed, Constants.MessageBoxTitleValidationError, 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.DialogResult = DialogResult.None;
                return;
            }

            if (_useProxyCheckBox.Checked && string.IsNullOrWhiteSpace(_proxyUrlTextBox.Text))
            {
                MessageBox.Show(Constants.MessageProxyValidationFailed, Constants.MessageBoxTitleValidationError, 
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
            _configuration.Save();
            
            // Save language setting and apply if changed
            if (_languageComboBox.SelectedItem is LanguageItem selectedLanguage)
            {
                var currentLanguage = Localization.CurrentCultureName;
                if (selectedLanguage.Code != currentLanguage)
                {
                    Localization.SetCulture(selectedLanguage.Code);
                    MessageBox.Show(
                        Localization.GetString("MessageSettingsSaved") + "\n\n" + 
                        Localization.GetString("MessageLanguageChangeRequiresRestart"),
                        Constants.MessageBoxTitleSuccess, 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(Constants.MessageSettingsSaved, Constants.MessageBoxTitleSuccess, 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show(Constants.MessageSettingsSaved, Constants.MessageBoxTitleSuccess, 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
