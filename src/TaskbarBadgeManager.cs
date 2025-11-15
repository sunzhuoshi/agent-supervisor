using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace AgentSupervisor
{
    public class TaskbarBadgeManager : IDisposable
    {
        private readonly Form _mainWindow;
        private Icon? _currentBadgeIcon;
        private int _currentCount;

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

        public TaskbarBadgeManager(Form mainWindow)
        {
            _mainWindow = mainWindow;
            _currentCount = 0;
            
            // Set a unique AppUserModelID for proper taskbar integration
            try
            {
                SetCurrentProcessExplicitAppUserModelID("AgentSupervisor.App");
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to set AppUserModelID", ex);
            }
            
            Logger.LogInfo("TaskbarBadgeManager initialized");
        }

        public void UpdateBadgeCount(int count)
        {
            if (_currentCount == count)
            {
                return; // No change needed
            }

            _currentCount = count;
            
            try
            {
                // Clean up previous icon
                var oldIcon = _currentBadgeIcon;
                
                if (count > 0)
                {
                    // Create new badge icon with count
                    _currentBadgeIcon = CreateBadgeIcon(count);
                    _mainWindow.Icon = _currentBadgeIcon;
                    Logger.LogInfo($"Badge count updated to {count}");
                }
                else
                {
                    // No pending reviews - use default icon
                    _currentBadgeIcon = CreateDefaultIcon();
                    _mainWindow.Icon = _currentBadgeIcon;
                    Logger.LogInfo("Badge cleared (no pending reviews)");
                }
                
                // Dispose old icon after setting new one
                if (oldIcon != null && oldIcon != _currentBadgeIcon)
                {
                    oldIcon.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to update badge count to {count}", ex);
            }
        }

        private Icon CreateBadgeIcon(int count)
        {
            // Create a 32x32 bitmap for the icon
            using var bitmap = new Bitmap(32, 32);
            using var graphics = Graphics.FromImage(bitmap);
            
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            
            // Load and draw the base icon
            DrawBaseIcon(graphics);
            
            // Draw badge overlay in top-right corner
            if (count > 0)
            {
                var badgeSize = 22;
                var badgeX = 32 - badgeSize;
                var badgeY = 0;
                
                // Draw red circle for badge
                using var badgeBrush = new SolidBrush(Color.FromArgb(255, 50, 50)); // Red
                graphics.FillEllipse(badgeBrush, badgeX, badgeY, badgeSize, badgeSize);
                
                // Draw white border
                using var borderPen = new Pen(Color.White, 2);
                graphics.DrawEllipse(borderPen, badgeX, badgeY, badgeSize, badgeSize);
                
                // Draw count number
                var countText = count > 99 ? "99+" : count.ToString();
                using var badgeFont = new Font("Arial", count > 9 ? 9 : 11, FontStyle.Bold);
                using var badgeTextBrush = new SolidBrush(Color.White);
                var countSize = graphics.MeasureString(countText, badgeFont);
                var countX = badgeX + (badgeSize - countSize.Width) / 2;
                var countY = badgeY + (badgeSize - countSize.Height) / 2;
                graphics.DrawString(countText, badgeFont, badgeTextBrush, countX, countY);
            }
            
            var hIcon = bitmap.GetHicon();
            var icon = Icon.FromHandle(hIcon);
            
            return icon;
        }

        private Icon CreateDefaultIcon()
        {
            // Create the same base icon without badge
            using var bitmap = new Bitmap(32, 32);
            using var graphics = Graphics.FromImage(bitmap);
            
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            
            // Load and draw the base icon
            DrawBaseIcon(graphics);
            
            var hIcon = bitmap.GetHicon();
            var icon = Icon.FromHandle(hIcon);
            
            return icon;
        }

        private void DrawBaseIcon(Graphics graphics)
        {
            // Load and draw the app_icon.ico as base
            try
            {
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "res", "app_icon.ico");
                if (File.Exists(iconPath))
                {
                    using var baseIcon = new Icon(iconPath, 32, 32);
                    using var baseIconBitmap = baseIcon.ToBitmap();
                    graphics.DrawImage(baseIconBitmap, 0, 0, 32, 32);
                }
                else
                {
                    Logger.LogWarning($"app_icon.ico not found at {iconPath}, using fallback");
                    DrawFallbackIcon(graphics);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to load app_icon.ico", ex);
                DrawFallbackIcon(graphics);
            }
        }

        private void DrawFallbackIcon(Graphics graphics)
        {
            // Fallback: Draw base circle with GitHub Copilot-inspired gradient
            using var brush = new LinearGradientBrush(
                new Rectangle(0, 0, 32, 32),
                Color.FromArgb(138, 43, 226), // Purple
                Color.FromArgb(0, 122, 204),   // Blue
                45f);
            graphics.FillEllipse(brush, 2, 2, 28, 28);
        }

        public void Dispose()
        {
            _currentBadgeIcon?.Dispose();
        }
    }
}
