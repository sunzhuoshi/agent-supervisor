using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace AgentSupervisor
{
    public class MainWindow : Form
    {
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_CONTEXTMENU = 0xF093;
        
        private readonly ContextMenuStrip? _contextMenu;

        public MainWindow(
            Action? onRecentNotificationsClick = null,
            Action? onSettingsClick = null,
            Action? onAboutClick = null,
            Action? onExitClick = null)
        {
            // Create a minimized, invisible window that appears in the taskbar
            Text = "Agent Supervisor";
            ShowInTaskbar = true;
            FormBorderStyle = FormBorderStyle.Fixed3D;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(-10000, -10000); // Position off-screen
            Size = new Size(0, 0);
            
            // Load and set the application icon
            try
            {
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "res", "app_icon.ico");
                if (File.Exists(iconPath))
                {
                    Icon = new Icon(iconPath);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to load application icon", ex);
            }
            
            // Create context menu for taskbar icon
            if (onRecentNotificationsClick != null && onSettingsClick != null && 
                onAboutClick != null && onExitClick != null)
            {
                _contextMenu = CreateContextMenu(
                    onRecentNotificationsClick,
                    onSettingsClick,
                    onAboutClick,
                    onExitClick);
                Logger.LogInfo("MainWindow context menu created");
            }
            
            // Prevent the window from being shown
            WindowState = FormWindowState.Minimized;
            
            // Prevent closing - hide instead
            FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    Hide();
                }
            };
            
            Logger.LogInfo("MainWindow created for taskbar presence");
        }

        protected override void WndProc(ref Message m)
        {
            // Handle right-click on taskbar icon
            if (m.Msg == WM_SYSCOMMAND && ((int)m.WParam & 0xFFF0) == SC_CONTEXTMENU)
            {
                if (_contextMenu != null)
                {
                    // Show context menu at cursor position
                    _contextMenu.Show(Cursor.Position);
                    return;
                }
            }
            
            base.WndProc(ref m);
        }

        private ContextMenuStrip CreateContextMenu(
            Action onRecentNotificationsClick,
            Action onSettingsClick,
            Action onAboutClick,
            Action onExitClick)
        {
            var menu = new ContextMenuStrip();

            var recentItem = new ToolStripMenuItem("Recent Notifications");
            recentItem.Click += (s, e) => onRecentNotificationsClick();
            menu.Items.Add(recentItem);

            menu.Items.Add(new ToolStripSeparator());

            var settingsItem = new ToolStripMenuItem("Settings");
            settingsItem.Click += (s, e) => onSettingsClick();
            menu.Items.Add(settingsItem);

            var aboutItem = new ToolStripMenuItem("About");
            aboutItem.Click += (s, e) => onAboutClick();
            menu.Items.Add(aboutItem);

            menu.Items.Add(new ToolStripSeparator());

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => onExitClick();
            menu.Items.Add(exitItem);

            return menu;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _contextMenu?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
