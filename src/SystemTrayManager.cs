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
        private readonly Action? _onRefreshBadge;
        private readonly Action _showReviewRequestsForm;
        private Icon? _customIcon;
        private AboutForm? _aboutForm;

        public SystemTrayManager(
            NotificationHistory notificationHistory,
            ReviewRequestService reviewRequestService,
            Action onSettingsClick,
            Action onExitClick,
            Action<string> onOpenUrlClick,
            Action? onRefreshBadge,
            Action showReviewRequestsForm)
        {
            Logger.LogInfo("Initializing SystemTrayManager");
            
            _notificationHistory = notificationHistory;
            _reviewRequestService = reviewRequestService;
            _onSettingsClick = onSettingsClick;
            _onExitClick = onExitClick;
            _onOpenUrlClick = onOpenUrlClick;
            _onRefreshBadge = onRefreshBadge;
            _showReviewRequestsForm = showReviewRequestsForm;

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
                Text = "Agent Supervisor"
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

            var recentItem = new ToolStripMenuItem("Review Requests by Copilots");
            recentItem.Click += (s, e) => _showReviewRequestsForm();
            menu.Items.Add(recentItem);

            menu.Items.Add(new ToolStripSeparator());

            var settingsItem = new ToolStripMenuItem("Settings");
            settingsItem.Click += (s, e) => _onSettingsClick();
            menu.Items.Add(settingsItem);

            var aboutItem = new ToolStripMenuItem("About");
            aboutItem.Click += (s, e) => ShowAbout();
            menu.Items.Add(aboutItem);

            menu.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => _onExitClick();
            menu.Items.Add(exitItem);

            return menu;
        }

        public void ShowNotification(PullRequestReview review)
        {
            var title = $"New PR Review - {review.RepositoryName}";
            var message = $"PR #{review.PullRequestNumber} - {review.State}\n" +
                         $"Reviewer: {review.User?.Login ?? "Unknown"}";

            _notifyIcon.ShowBalloonTip(5000, title, message, ToolTipIcon.Info);
            
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
            _notifyIcon.Text = $"Agent Supervisor\n{status}";
        }

        private Icon CreateCustomIcon()
        {
            try
            {
                // Load app_icon.ico for the system tray
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "res", "app_icon.ico");
                if (File.Exists(iconPath))
                {
                    var icon = new Icon(iconPath, 16, 16);
                    Logger.LogInfo("Custom icon loaded from app_icon.ico");
                    return icon;
                }
                else
                {
                    Logger.LogWarning($"app_icon.ico not found at {iconPath}, using fallback");
                    // Fallback: Create a simple custom icon with GitHub Copilot colors
                    using var bitmap = new Bitmap(16, 16);
                    using var graphics = Graphics.FromImage(bitmap);
                    
                    // Fill with a gradient from purple to blue (GitHub Copilot colors)
                    using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                        new Rectangle(0, 0, 16, 16),
                        Color.FromArgb(138, 43, 226), // Purple
                        Color.FromArgb(0, 122, 204),   // Blue
                        45f);
                    
                    graphics.FillEllipse(brush, 0, 0, 16, 16);
                    
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

        public void Dispose()
        {
            _aboutForm?.Dispose();
            _notifyIcon?.Dispose();
            _contextMenu?.Dispose();
            _customIcon?.Dispose();
        }
    }
}
