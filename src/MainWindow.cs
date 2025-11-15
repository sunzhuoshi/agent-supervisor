using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace AgentSupervisor
{
    public class MainWindow : Form
    {
        private readonly ReviewRequestService? _reviewRequestService;
        private readonly Action<string>? _onOpenUrlClick;
        private readonly Action? _onRefreshBadge;

        public MainWindow(
            ReviewRequestService? reviewRequestService = null,
            Action<string>? onOpenUrlClick = null,
            Action? onRefreshBadge = null)
        {
            _reviewRequestService = reviewRequestService;
            _onOpenUrlClick = onOpenUrlClick;
            _onRefreshBadge = onRefreshBadge;

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
            
            // Prevent the window from being shown
            WindowState = FormWindowState.Minimized;
            
            // Add activated handler to open review requests form
            Activated += MainWindow_Activated;
            
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

        private void MainWindow_Activated(object? sender, EventArgs e)
        {
            if (_reviewRequestService != null && _onOpenUrlClick != null)
            {
                ShowReviewRequests();
            }
        }

        private void ShowReviewRequests()
        {
            var form = new ReviewRequestsForm(
                _reviewRequestService!,
                _onOpenUrlClick!,
                () => _reviewRequestService!.MarkAllAsRead(),
                _onRefreshBadge);
            form.ShowDialog();
        }
    }
}
