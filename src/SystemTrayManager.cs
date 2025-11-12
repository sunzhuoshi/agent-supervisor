using System;
using System.Drawing;
using System.Windows.Forms;
using AgentSupervisor.Models;

namespace AgentSupervisor
{
    public class SystemTrayManager : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenuStrip _contextMenu;
        private readonly NotificationHistory _notificationHistory;
        private readonly Action _onSettingsClick;
        private readonly Action _onExitClick;
        private readonly Action<string> _onOpenUrlClick;

        public SystemTrayManager(
            NotificationHistory notificationHistory,
            Action onSettingsClick,
            Action onExitClick,
            Action<string> onOpenUrlClick)
        {
            Logger.LogInfo("Initializing SystemTrayManager");
            
            _notificationHistory = notificationHistory;
            _onSettingsClick = onSettingsClick;
            _onExitClick = onExitClick;
            _onOpenUrlClick = onOpenUrlClick;

            _contextMenu = CreateContextMenu();
            _notifyIcon = CreateNotifyIcon();
            
            Logger.LogInfo("SystemTrayManager initialized successfully");
        }

        private NotifyIcon CreateNotifyIcon()
        {
            Logger.LogInfo("Creating system tray icon");
            
            var icon = new NotifyIcon
            {
                Icon = SystemIcons.Information,
                Visible = true,
                Text = "Agent Supervisor"
            };
            icon.ContextMenuStrip = _contextMenu;
            icon.DoubleClick += (s, e) => ShowRecentNotifications();
            
            // Ensure icon is shown (workaround for Windows 11 issues)
            icon.Visible = false;
            icon.Visible = true;
            
            Logger.LogInfo($"System tray icon created - Visible: {icon.Visible}, Text: {icon.Text}");
            
            return icon;
        }

        private ContextMenuStrip CreateContextMenu()
        {
            var menu = new ContextMenuStrip();

            var recentItem = new ToolStripMenuItem("Recent Notifications");
            recentItem.Click += (s, e) => ShowRecentNotifications();
            menu.Items.Add(recentItem);

            menu.Items.Add(new ToolStripSeparator());

            var settingsItem = new ToolStripMenuItem("Settings");
            settingsItem.Click += (s, e) => _onSettingsClick();
            menu.Items.Add(settingsItem);

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

        private void ShowRecentNotifications()
        {
            var recent = _notificationHistory.GetRecent(10);
            
            if (recent.Count == 0)
            {
                MessageBox.Show("No recent notifications.", "Recent Notifications", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var message = "Recent Notifications:\n\n";
            foreach (var entry in recent)
            {
                message += $"• {entry.Repository} PR#{entry.PullRequestNumber}\n";
                message += $"  {entry.State} by {entry.Reviewer}\n";
                message += $"  {entry.NotifiedAt:MM/dd HH:mm}\n\n";
            }

            var result = MessageBox.Show(message, "Recent Notifications", 
                MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            
            if (result == DialogResult.OK && recent.Count > 0)
            {
                _onOpenUrlClick(recent[0].HtmlUrl);
            }
        }

        public void UpdateStatus(string status)
        {
            _notifyIcon.Text = $"Agent Supervisor\n{status}";
        }

        public void Dispose()
        {
            _notifyIcon?.Dispose();
            _contextMenu?.Dispose();
        }
    }
}
