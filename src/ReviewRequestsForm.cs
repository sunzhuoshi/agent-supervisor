using System;
using System.Drawing;
using System.Windows.Forms;
using AgentSupervisor.Models;

namespace AgentSupervisor
{
    public class ReviewRequestsForm : Form
    {
        private readonly ReviewRequestService _reviewRequestService;
        private readonly Action<string> _onOpenUrlClick;
        private readonly Action _onMarkAllAsRead;
        private readonly Action? _onRefreshBadge;
        private ListBox _listBox = null!;
        private Button _markAllReadButton = null!;
        private Label _statusLabel = null!;
        private ContextMenuStrip _contextMenu = null!;
        private DateTime _lastMinimizedOrHiddenTime = DateTime.MinValue;

        public DateTime LastMinimizedOrHiddenTime => _lastMinimizedOrHiddenTime;

        public ReviewRequestsForm(
            ReviewRequestService reviewRequestService,
            Action<string> onOpenUrlClick,
            Action onMarkAllAsRead,
            Action onRefreshBadge)
        {
            _reviewRequestService = reviewRequestService;
            _onOpenUrlClick = onOpenUrlClick;
            _onMarkAllAsRead = onMarkAllAsRead;
            _onRefreshBadge = onRefreshBadge;

            InitializeComponent();
            LoadRequests();
            
            // Override FormClosing to hide instead of close
            FormClosing += OnFormClosing;
            
            // Track when form is minimized
            Resize += OnResize;
        }

        private void InitializeComponent()
        {
            // Form settings
            Text = "Review Requests by Copilots";
            Size = new Size(600, 500);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(500, 400);
            ShowInTaskbar = false;

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
            
            var openMenuItem = new ToolStripMenuItem("Open");
            openMenuItem.Click += ContextMenu_Open_Click;
            _contextMenu.Items.Add(openMenuItem);
            
            var markAsReadMenuItem = new ToolStripMenuItem("Mark as read");
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
                Text = "Mark All as Read",
                Dock = DockStyle.Left,
                Width = 150,
                Height = 30
            };
            _markAllReadButton.Click += MarkAllReadButton_Click;
            buttonPanel.Controls.Add(_markAllReadButton);

            var closeButton = new Button
            {
                Text = "Close",
                Dock = DockStyle.Right,
                Width = 100,
                Height = 30,
                DialogResult = DialogResult.OK
            };
            closeButton.Click += CloseButton_Click;
            buttonPanel.Controls.Add(closeButton);

            Controls.Add(buttonPanel);

            AcceptButton = closeButton;
        }

        private void CloseButton_Click(object? sender, EventArgs e)
        {
            Close();
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

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            // Only cancel and hide for user-initiated closes
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                _lastMinimizedOrHiddenTime = DateTime.Now;
                Hide();
            }
        }

        private void OnResize(object? sender, EventArgs e)
        {
            // Track when the form is minimized
            if (WindowState == FormWindowState.Minimized)
            {
                _lastMinimizedOrHiddenTime = DateTime.Now;
            }
        }

        public void RefreshAndShow()
        {
            LoadRequests();
            Show();
            BringToFront();
            Activate();
        }

        private void UpdateStatus()
        {
            var newCount = _reviewRequestService.GetNewCount();
            var totalCount = _listBox.Items.Count;

             _onRefreshBadge.Invoke();
            if (totalCount == 0)
            {
                _statusLabel.Text = "No review requests";
                _markAllReadButton.Enabled = false;
            }
            else
            {
                _statusLabel.Text = $"Total: {totalCount} review request(s) | New: {newCount}";
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
                
                var badgeRect = new Rectangle(e.Bounds.Right - 60, y, 50, 18);
                e.Graphics.FillRectangle(newBadgeBrush, badgeRect);
                
                var badgeText = "NEW";
                var badgeTextSize = e.Graphics.MeasureString(badgeText, newBadgeFont);
                var badgeTextX = badgeRect.X + (badgeRect.Width - badgeTextSize.Width) / 2;
                var badgeTextY = badgeRect.Y + (badgeRect.Height - badgeTextSize.Height) / 2;
                e.Graphics.DrawString(badgeText, newBadgeFont, newBadgeTextBrush, badgeTextX, badgeTextY);
            }

            // Draw repository and PR number
            using var titleFont = new Font(e.Font?.FontFamily ?? SystemFonts.DefaultFont.FontFamily, 9, FontStyle.Bold);
            using var textBrush = new SolidBrush(textColor);
            var titleText = $"{request.Repository} PR#{request.PullRequestNumber}";
            e.Graphics.DrawString(titleText, titleFont, textBrush, x, y);

            // Draw title
            y += 18;
            var titleMaxWidth = e.Bounds.Width - 80;
            var title = request.Title.Length > 60 ? request.Title.Substring(0, 60) + "..." : request.Title;
            e.Graphics.DrawString(title, e.Font ?? SystemFonts.DefaultFont, textBrush, new RectangleF(x, y, titleMaxWidth, 30));

            // Draw author and date
            y += 18;
            using var detailFont = new Font(e.Font?.FontFamily ?? SystemFonts.DefaultFont.FontFamily, 8);
            using var detailBrush = new SolidBrush(e.State.HasFlag(DrawItemState.Selected) ? textColor : Color.Gray);
            var detailText = $"by {request.Author} • {request.CreatedAt:MMM dd, yyyy HH:mm}";
            e.Graphics.DrawString(detailText, detailFont, detailBrush, x, y);

            // Draw focus rectangle
            e.DrawFocusRectangle();
        }

        private void ListBox_DoubleClick(object? sender, EventArgs e)
        {
            if (_listBox.SelectedItem is ReviewRequestEntry request)
            {
                // Mark as read
                _reviewRequestService.MarkAsRead(request.Id);
                
                // Open URL
                _onOpenUrlClick(request.HtmlUrl);
                
                // Refresh the display
                LoadRequests();
                
                // Refresh taskbar badge
                _onRefreshBadge?.Invoke();
            }
        }

        private void MarkAllReadButton_Click(object? sender, EventArgs e)
        {
            _onMarkAllAsRead();
            LoadRequests();
            
            // Refresh taskbar badge
            _onRefreshBadge?.Invoke();
        }

        private void ContextMenu_Open_Click(object? sender, EventArgs e)
        {
            if (_listBox.SelectedItem is ReviewRequestEntry request)
            {
                // Mark as read
                _reviewRequestService.MarkAsRead(request.Id);
                
                // Open URL
                _onOpenUrlClick(request.HtmlUrl);
                
                // Refresh the display
                LoadRequests();
                
                // Refresh taskbar badge
                _onRefreshBadge?.Invoke();
            }
        }

        private void ContextMenu_MarkAsRead_Click(object? sender, EventArgs e)
        {
            if (_listBox.SelectedItem is ReviewRequestEntry request)
            {
                // Save scroll position
                var topIndex = _listBox.TopIndex;
                var selectedIndex = _listBox.SelectedIndex;
                
                // Mark as read
                _reviewRequestService.MarkAsRead(request.Id);
                
                // Refresh the display
                LoadRequests();
                
                // Restore scroll position
                if (topIndex < _listBox.Items.Count)
                {
                    _listBox.TopIndex = topIndex;
                }
                
                // Restore selection if the item still exists
                if (selectedIndex < _listBox.Items.Count)
                {
                    _listBox.SelectedIndex = selectedIndex;
                }
                
                // Refresh taskbar badge
                _onRefreshBadge?.Invoke();
            }
        }
    }
}
