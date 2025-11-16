using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using AgentSupervisor.Models;

namespace AgentSupervisor
{
    public class SystemTrayManager : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenuStrip _contextMenu;
        private readonly NotificationHistory _notificationHistory;
        private readonly ReviewRequestService _reviewRequestService;
        private readonly Action _onSettingsClick;
        private readonly Action _onExitClick;
        private readonly Action<string> _onOpenUrlClick;
        private readonly Action _onCheckForUpdatesClick;
        private readonly Action? _onRefreshBadge;
        private readonly Action _showReviewRequestsForm;
#if ENABLE_DEV_FEATURES
        private readonly Action? _onTriggerPolling;
#endif
        private readonly Configuration _config;
        private readonly Action? _onConfigChanged;
        private ToolStripMenuItem? _pausePollingMenuItem;
        private Icon? _customIcon;
        private AboutForm? _aboutForm;
        private EventHandler? _currentBalloonTipClickedHandler;

        public SystemTrayManager(
            NotificationHistory notificationHistory,
            ReviewRequestService reviewRequestService,
            Action onSettingsClick,
            Action onExitClick,
            Action<string> onOpenUrlClick,
            Action onCheckForUpdatesClick,
            Action? onRefreshBadge,
            Action showReviewRequestsForm,
#if ENABLE_DEV_FEATURES
            Action? onTriggerPolling = null,
#endif
            Configuration? config = null,
            Action? onConfigChanged = null
            )
        {
            Logger.LogInfo("Initializing SystemTrayManager");
            
            _notificationHistory = notificationHistory;
            _reviewRequestService = reviewRequestService;
            _onSettingsClick = onSettingsClick;
            _onExitClick = onExitClick;
            _onOpenUrlClick = onOpenUrlClick;
            _onCheckForUpdatesClick = onCheckForUpdatesClick;
            _onRefreshBadge = onRefreshBadge;
            _showReviewRequestsForm = showReviewRequestsForm;
#if ENABLE_DEV_FEATURES
            _onTriggerPolling = onTriggerPolling;
#endif
            _config = config ?? new Configuration();
            _onConfigChanged = onConfigChanged;

            // Create custom icon
            _customIcon = CreateCustomIcon();

            _contextMenu = CreateContextMenu();
            _notifyIcon = CreateNotifyIcon();
            
            Logger.LogInfo("SystemTrayManager initialized successfully");
        }

        private NotifyIcon CreateNotifyIcon()
        {
            Logger.LogInfo("Creating system tray icon");
            
            var icon = new NotifyIcon
            {
                Icon = _customIcon,
                Visible = true,
                Text = Constants.ApplicationName
            };
            icon.ContextMenuStrip = _contextMenu;
            icon.DoubleClick += (s, e) => _showReviewRequestsForm();
            
            // Ensure icon is shown (workaround for Windows 11 issues)
            icon.Visible = false;
            icon.Visible = true;
            
            Logger.LogInfo($"System tray icon created - Visible: {icon.Visible}, Text: {icon.Text}");
            
            return icon;
        }

        private ContextMenuStrip CreateContextMenu()
        {
            var menu = new ContextMenuStrip();

            var recentItem = new ToolStripMenuItem(Constants.MenuItemReviewRequests);
            recentItem.Click += (s, e) => _showReviewRequestsForm();
            menu.Items.Add(recentItem);

            menu.Items.Add(new ToolStripSeparator());

#if ENABLE_DEV_FEATURES
            // DEV-only menu item for data polling
            var pollDataItem = new ToolStripMenuItem(Constants.MenuItemPollAtOnce);
            pollDataItem.Click += (s, e) => PollData();
            menu.Items.Add(pollDataItem);
            
            Logger.LogInfo("DEV features enabled - 'Poll at Once' menu item added");
#endif
            
            // Pause polling menu item available in all builds
            _pausePollingMenuItem = new ToolStripMenuItem(GetPauseMenuText());
            _pausePollingMenuItem.Click += (s, e) => TogglePausePolling();
            menu.Items.Add(_pausePollingMenuItem);
            
            menu.Items.Add(new ToolStripSeparator());

            var settingsItem = new ToolStripMenuItem(Constants.MenuItemSettings);
            settingsItem.Click += (s, e) => _onSettingsClick();
            menu.Items.Add(settingsItem);

            var aboutItem = new ToolStripMenuItem(Constants.MenuItemAbout);
            aboutItem.Click += (s, e) => ShowAbout();
            menu.Items.Add(aboutItem);

            menu.Items.Add(new ToolStripSeparator());

            var checkUpdatesItem = new ToolStripMenuItem(Constants.MenuItemCheckForUpdates);
            checkUpdatesItem.Click += (s, e) => _onCheckForUpdatesClick();
            menu.Items.Add(checkUpdatesItem);

            menu.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem(Constants.MenuItemExit);
            exitItem.Click += (s, e) => _onExitClick();
            menu.Items.Add(exitItem);

            return menu;
        }

        public void ShowNotification(PullRequestReview review)
        {
            var title = $"New PR Review - {review.RepositoryName}";
            var message = $"PR #{review.PullRequestNumber} - {review.State}\n" +
                         $"Reviewer: {review.User?.Login ?? "Unknown"}";

            _notifyIcon.ShowBalloonTip(Constants.NotificationTimeoutMilliseconds, title, message, ToolTipIcon.Info);
            
            // Store the URL for the balloon click event
            var tempUrl = review.HtmlUrl;
            _notifyIcon.BalloonTipClicked += (s, e) =>
            {
                _onOpenUrlClick(tempUrl);
            };
        }

        private void ShowAbout()
        {
            // If form exists and is not disposed, bring it to front
            if (_aboutForm != null && !_aboutForm.IsDisposed)
            {
                _aboutForm.Activate();
                _aboutForm.BringToFront();
                return;
            }

            // Create new form instance
            _aboutForm = new AboutForm();
            _aboutForm.FormClosed += (s, e) => _aboutForm = null;
            _aboutForm.ShowDialog();
        }

        public void UpdateStatus(string status)
        {
            _notifyIcon.Text = $"{Constants.ApplicationName}\n{status}";
        }

        private Icon CreateCustomIcon()
        {
            try
            {
                // Load app_icon.ico for the system tray
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.IconResourcePath, Constants.AppIconFileName);
                if (File.Exists(iconPath))
                {
                    var icon = new Icon(iconPath, Constants.SystemTrayIconSize, Constants.SystemTrayIconSize);
                    Logger.LogInfo("Custom icon loaded from app_icon.ico");
                    return icon;
                }
                else
                {
                    Logger.LogWarning($"app_icon.ico not found at {iconPath}, using fallback");
                    // Fallback: Create a simple custom icon with GitHub Copilot colors
                    using var bitmap = new Bitmap(Constants.SystemTrayIconSize, Constants.SystemTrayIconSize);
                    using var graphics = Graphics.FromImage(bitmap);
                    
                    // Fill with a gradient from purple to blue (GitHub Copilot colors)
                    using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                        new Rectangle(0, 0, Constants.SystemTrayIconSize, Constants.SystemTrayIconSize),
                        Color.FromArgb(Constants.IconGradientStartColorArgb),
                        Color.FromArgb(Constants.IconGradientEndColorArgb),
                        Constants.IconGradientAngle);
                    
                    graphics.FillEllipse(brush, 0, 0, Constants.SystemTrayIconSize, Constants.SystemTrayIconSize);
                    
                    // Draw a simple "A" in white for "Agent"
                    using var font = new Font("Arial", 9, FontStyle.Bold);
                    using var textBrush = new SolidBrush(Color.White);
                    graphics.DrawString("A", font, textBrush, -1, 1);
                    
                    var hIcon = bitmap.GetHicon();
                    var icon = Icon.FromHandle(hIcon);
                    
                    Logger.LogInfo("Fallback icon created");
                    return icon;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to create custom icon, using default", ex);
                return SystemIcons.Information;
            }
        }

        public void ShowUpdateNotification(UpdateInfo updateInfo)
        {
            var title = Constants.MessageBoxTitleUpdateAvailable;
            var message = $"Version {updateInfo.Version} is now available!\n" +
                         $"Published: {updateInfo.PublishedAt:MMM dd, yyyy}";

            _notifyIcon.ShowBalloonTip(Constants.NotificationTimeoutMilliseconds, title, message, ToolTipIcon.Info);

            // Remove previous handler if it exists
            if (_currentBalloonTipClickedHandler != null)
            {
                _notifyIcon.BalloonTipClicked -= _currentBalloonTipClickedHandler;
            }

            // Store the release URL for the balloon click event
            var tempUrl = updateInfo.ReleaseUrl;
            _currentBalloonTipClickedHandler = (s, e) =>
            {
                _onOpenUrlClick(tempUrl);
            };
            _notifyIcon.BalloonTipClicked += _currentBalloonTipClickedHandler;
        }

#if ENABLE_DEV_FEATURES
        /// <summary>
        /// Triggers immediate polling of review requests from GitHub
        /// This method is only available when ENABLE_DEV_FEATURES is defined during compilation
        /// </summary>
        private void PollData()
        {
            try
            {
                Logger.LogInfo("Triggering immediate data polling (DEV build)");

                if (_onTriggerPolling != null)
                {
                    _onTriggerPolling();
                }
                else
                {
                    Logger.LogWarning("Polling callback not configured");
                    MessageBox.Show(
                        Constants.MessagePollingNotConfigured,
                        Constants.MessageBoxTitleConfigurationError,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error triggering data polling", ex);
                MessageBox.Show(
                    $"Error triggering data polling:\n\n{ex.Message}",
                    Constants.MessageBoxTitlePollingError,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
#endif

        /// <summary>
        /// Toggles the pause polling state
        /// </summary>
        private void TogglePausePolling()
        {
            try
            {
                _config.PausePolling = !_config.PausePolling;
                _config.Save();
                
                // Update menu item text
                if (_pausePollingMenuItem != null)
                {
                    _pausePollingMenuItem.Text = GetPauseMenuText();
                }
                
                // Notify the application of the configuration change
                _onConfigChanged?.Invoke();
                
                var statusMessage = _config.PausePolling ? Constants.MessagePollingPaused : Constants.MessagePollingResumed;
                Logger.LogInfo($"Pause polling toggled: {statusMessage}");
                
                MessageBox.Show(
                    statusMessage,
                    Constants.MessageBoxTitlePollingStatus,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Logger.LogError("Error toggling pause polling", ex);
                MessageBox.Show(
                    $"Error toggling pause polling:\n\n{ex.Message}",
                    Constants.MessageBoxTitleError,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Gets the menu text for the pause polling menu item based on current state
        /// </summary>
        private string GetPauseMenuText()
        {
            return _config.PausePolling ? Constants.MenuItemResumePolling : Constants.MenuItemPausePolling;
        }

        public void Dispose()
        {
            _aboutForm?.Dispose();
            _notifyIcon?.Dispose();
            _contextMenu?.Dispose();
            _customIcon?.Dispose();
        }
    }
}
