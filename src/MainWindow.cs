using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using AgentSupervisor.Models;

namespace AgentSupervisor
{
    public class MainWindow : Form, IReviewRequestObserver
    {
        private readonly ReviewRequestService _reviewRequestService;
        private readonly Action<string> _onOpenUrlClick;
        private TaskbarBadgeManager? _badgeManager;
        private ListBox _listBox = null!;
        private Button _markAllReadButton = null!;
        private Label _statusLabel = null!;
        private ContextMenuStrip _contextMenu = null!;

        public MainWindow(
            ReviewRequestService reviewRequestService,
            Action<string> onOpenUrlClick)
        {
            _reviewRequestService = reviewRequestService;
            _onOpenUrlClick = onOpenUrlClick;

            InitializeComponent();
            LoadRequests();
            
            // Subscribe to review request changes
            _reviewRequestService.Subscribe(this);
            
            // Load and set the application icon
            try
            {
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.IconResourcePath, Constants.AppIconFileName);
                if (File.Exists(iconPath))
                {
                    Icon = new Icon(iconPath);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to load application icon", ex);
            }
            
            // Prevent closing - minimize to taskbar instead
            FormClosing += OnFormClosing;
            
            // Refresh list when form is activated/restored
            Activated += OnFormActivated;

            // Refresh list when form is resized
            Resize += OnForm_Resize;
            
            // Start minimized
            WindowState = FormWindowState.Minimized;
            
            Logger.LogInfo("MainWindow created with review requests functionality");
        }

        /// <summary>
        /// Set the badge manager after construction (since MainWindow needs to be shown first)
        /// </summary>
        public void SetBadgeManager(TaskbarBadgeManager badgeManager)
        {
            _badgeManager = badgeManager;
        }

        /// <summary>
        /// Observer callback - automatically called when review requests change
        /// </summary>
        public void OnReviewRequestsChanged()
        {
            // Update badge
            if (_badgeManager != null)
            {
                var unreadCount = _reviewRequestService.GetNewCount();
                if (!IsDisposed)
                {
                    BeginInvoke(() => _badgeManager.UpdateBadgeCount(unreadCount));
                }
            }

            // Refresh UI if visible
            if (!IsDisposed)
            {
                BeginInvoke(() => RefreshIfVisible());
            }
        }

        private void InitializeComponent()
        {
            // Form settings
            Text = $"{Constants.ApplicationName} - {Localization.GetString("MainWindowTitle")}";
            Size = new Size(Constants.MainWindowDefaultWidth, Constants.MainWindowDefaultHeight);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(Constants.MainWindowMinWidth, Constants.MainWindowMinHeight);
            ShowInTaskbar = true;
            FormBorderStyle = FormBorderStyle.Sizable;

            // Status label at top
            _statusLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 5, 10, 5),
                Font = new Font(Font.FontFamily, 9, FontStyle.Bold)
            };
            Controls.Add(_statusLabel);

            // Context menu for list items
            _contextMenu = new ContextMenuStrip();
            
            var openMenuItem = new ToolStripMenuItem(Localization.GetString("MainWindowContextMenuOpen"));
            openMenuItem.Click += ContextMenu_Open_Click;
            _contextMenu.Items.Add(openMenuItem);
            
            var markAsReadMenuItem = new ToolStripMenuItem(Localization.GetString("MainWindowContextMenuMarkAsRead"));
            markAsReadMenuItem.Click += ContextMenu_MarkAsRead_Click;
            _contextMenu.Items.Add(markAsReadMenuItem);

            // ListBox for review requests
            _listBox = new ListBox
            {
                Dock = DockStyle.Fill,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 60,
                Font = new Font(Font.FontFamily, 9),
                SelectionMode = SelectionMode.One,
                ContextMenuStrip = _contextMenu
            };
            _listBox.DrawItem += ListBox_DrawItem;
            _listBox.DoubleClick += ListBox_DoubleClick;
            var listBoxPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, _statusLabel.Height, 0, 0)
            };
            listBoxPanel.Controls.Add(_listBox);    
            Controls.Add(listBoxPanel);

            // Button panel at bottom
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                Padding = new Padding(10)
            };

            _markAllReadButton = new Button
            {
                Text = Localization.GetString("MainWindowButtonMarkAllRead"),
                Dock = DockStyle.Left,
                Width = 150,
                Height = 30
            };
            _markAllReadButton.Click += MarkAllReadButton_Click;
            buttonPanel.Controls.Add(_markAllReadButton);

            Controls.Add(buttonPanel);
        }

        private void OnFormActivated(object? sender, EventArgs e)
        {
            // Refresh the list when the form is activated (e.g., restored from minimized)
            if (WindowState != FormWindowState.Minimized)
            {
                LoadRequests();
            }
        }

        private void OnForm_Resize(object? sender, EventArgs e)
        {
            // Refresh the listbox to properly redraw NEW badges at correct positions
            _listBox.Refresh();
        }

        private void LoadRequests()
        {
            var oldTopIndex = _listBox.Items.Count > 0 ? _listBox.TopIndex : 0;

            _listBox.Items.Clear();
            var requests = _reviewRequestService.GetAll();
            
            foreach (var request in requests)
            {
                _listBox.Items.Add(request);
            }

            _listBox.TopIndex = oldTopIndex < _listBox.Items.Count ? oldTopIndex : 0;
            UpdateStatus();
        }

        private void RefreshChangedItems(string? specificItemId = null)
        {
            // Get current requests from the service
            var currentRequests = _reviewRequestService.GetAll();
            var currentRequestsDict = currentRequests.ToDictionary(r => r.Id);
            
            // Track which items need to be refreshed
            var itemsToRefresh = new List<int>();
            
            // Check each item in the list box
            for (int i = 0; i < _listBox.Items.Count; i++)
            {
                if (_listBox.Items[i] is ReviewRequestEntry listItem)
                {
                    // If the item exists in the current requests
                    if (currentRequestsDict.TryGetValue(listItem.Id, out var currentItem))
                    {
                        bool shouldRefresh = false;
                        
                        // If a specific item ID was provided, check if this is that item
                        if (specificItemId != null && listItem.Id == specificItemId)
                        {
                            shouldRefresh = true;
                        }
                        // Otherwise, check if IsNew status changed (for service updates)
                        else if (specificItemId == null && listItem.IsNew != currentItem.IsNew)
                        {
                            shouldRefresh = true;
                        }
                        
                        if (shouldRefresh)
                        {
                            // Update the item in place with current data
                            _listBox.Items[i] = currentItem;
                            itemsToRefresh.Add(i);
                        }
                    }
                }
            }
            
            // If there are items to refresh, invalidate only those items
            if (itemsToRefresh.Count > 0)
            {
                foreach (var index in itemsToRefresh)
                {
                    // Invalidate the specific item region
                    _listBox.Invalidate(_listBox.GetItemRectangle(index));
                }
                // Force immediate redraw of invalidated regions
                _listBox.Update();
            }
            
            // Always update status when called
            UpdateStatus();
        }

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            // Only cancel and minimize for user-initiated closes
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                WindowState = FormWindowState.Minimized;
            }
        }

        public void RefreshAndShow()
        {
            LoadRequests();
            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }
            Show();
            BringToFront();
            Activate();
        }

        public void RefreshIfVisible()
        {
            // Only refresh the list if the window is visible and not minimized
            if (WindowState != FormWindowState.Minimized && Visible)
            {
                // Check if we can do a partial refresh (items haven't changed, only IsNew status)
                var currentRequests = _reviewRequestService.GetAll();
                var currentIds = currentRequests.Select(r => r.Id).ToList();
                var listBoxIds = _listBox.Items.Cast<ReviewRequestEntry>().Select(r => r.Id).ToList();
                
                // If the IDs match, we can do a selective refresh
                if (currentIds.SequenceEqual(listBoxIds))
                {
                    RefreshChangedItems();
                }
                else
                {
                    // Items were added or removed, do a full refresh
                    LoadRequests();
                }
            }
        }

        private void UpdateStatus()
        {
            var newCount = _reviewRequestService.GetNewCount();
            var totalCount = _listBox.Items.Count;

            if (totalCount == 0)
            {
                _statusLabel.Text = Localization.GetString("MainWindowStatusNoRequests");
                _markAllReadButton.Enabled = false;
            }
            else
            {
                _statusLabel.Text = Localization.GetString("MainWindowStatusFormat", totalCount, newCount);
                _markAllReadButton.Enabled = newCount > 0;
            }
        }

        private void ListBox_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _listBox.Items.Count || _listBox.Items[e.Index] is not ReviewRequestEntry request)
                return;

            // Background
            var backgroundColor = e.State.HasFlag(DrawItemState.Selected)
                ? SystemColors.Highlight
                : (request.IsNew ? Color.FromArgb(255, 250, 220) : SystemColors.Window);
            
            using var backgroundBrush = new SolidBrush(backgroundColor);
            e.Graphics.FillRectangle(backgroundBrush, e.Bounds);

            // Text color
            var textColor = e.State.HasFlag(DrawItemState.Selected)
                ? SystemColors.HighlightText
                : SystemColors.WindowText;

            var x = e.Bounds.X + 10;
            var y = e.Bounds.Y + 5;

            // Draw "NEW" badge if applicable
            if (request.IsNew)
            {
                using var newBadgeBrush = new SolidBrush(Color.FromArgb(220, 53, 69));
                using var newBadgeFont = new Font(e.Font?.FontFamily ?? SystemFonts.DefaultFont.FontFamily, 7, FontStyle.Bold);
                using var newBadgeTextBrush = new SolidBrush(Color.White);
                
                var badgeText = Localization.GetString("MainWindowBadgeNew");
                var badgeTextSize = e.Graphics.MeasureString(badgeText, newBadgeFont);
                var badgeWidth = Math.Max(50, (int)badgeTextSize.Width + 10);
                var badgeRect = new Rectangle(e.Bounds.Right - badgeWidth - 10, y, badgeWidth, 18);
                e.Graphics.FillRectangle(newBadgeBrush, badgeRect);
                
                var badgeTextX = badgeRect.X + (badgeRect.Width - badgeTextSize.Width) / 2;
                var badgeTextY = badgeRect.Y + (badgeRect.Height - badgeTextSize.Height) / 2;
                e.Graphics.DrawString(badgeText, newBadgeFont, newBadgeTextBrush, badgeTextX, badgeTextY);
            }

            // Draw title
            using var titleFont = new Font(e.Font?.FontFamily ?? SystemFonts.DefaultFont.FontFamily, 9, FontStyle.Bold);
            using var textBrush = new SolidBrush(textColor);
            var titleMaxWidth = e.Bounds.Width - 80;
            var title = request.Title.Length > 60 ? request.Title.Substring(0, 60) + "..." : request.Title;
            e.Graphics.DrawString(title, titleFont, textBrush, new RectangleF(x, y, titleMaxWidth, 30));

            // Draw repository and PR number
            y += 18;
            var repoText = $"{request.Repository} PR#{request.PullRequestNumber}";
            e.Graphics.DrawString(repoText, e.Font ?? SystemFonts.DefaultFont, textBrush, x, y);

            // Draw author and date
            y += 18;
            using var detailFont = new Font(e.Font?.FontFamily ?? SystemFonts.DefaultFont.FontFamily, 8);
            using var detailBrush = new SolidBrush(e.State.HasFlag(DrawItemState.Selected) ? textColor : Color.Gray);
            var detailText = Localization.GetString("DetailAuthorDateFormat", request.Author, request.CreatedAt.ToString("MMM dd, yyyy HH:mm"));
            e.Graphics.DrawString(detailText, detailFont, detailBrush, x, y);

            // Draw focus rectangle
            e.DrawFocusRectangle();
        }

        private void ListBox_DoubleClick(object? sender, EventArgs e)
        {
            if (_listBox.SelectedItem is ReviewRequestEntry request)
            {
                MarkAsReadAndOpen(request);
            }
        }

        private void MarkAllReadButton_Click(object? sender, EventArgs e)
        {
            _reviewRequestService.MarkAllAsRead();
            // UI will be updated automatically via observer pattern
        }

        private void ContextMenu_Open_Click(object? sender, EventArgs e)
        {
            if (_listBox.SelectedItem is ReviewRequestEntry request)
            {
                MarkAsReadAndOpen(request);
            }
        }

        private void MarkAsReadAndOpen(ReviewRequestEntry request)
        {
            // Mark as read - UI will be updated automatically via observer
            _reviewRequestService.MarkAsRead(request.Id);
            
            // Open URL
            _onOpenUrlClick(request.HtmlUrl);
        }

        private void ContextMenu_MarkAsRead_Click(object? sender, EventArgs e)
        {
            if (_listBox.SelectedItem is ReviewRequestEntry request)
            {
                // Mark as read - UI will be updated automatically via observer
                _reviewRequestService.MarkAsRead(request.Id);
            }
        }
    }
}
