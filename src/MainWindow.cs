using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace AgentSupervisor
{
    public class MainWindow : Form
    {
        public MainWindow()
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
    }
}
