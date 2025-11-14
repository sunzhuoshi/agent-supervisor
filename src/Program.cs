using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace AgentSupervisor
{
    class Program
    {
        private static Mutex? _mutex;
        private const string MutexName = "AgentSupervisor_SingleInstance_Mutex";

        [STAThread]
        static void Main(string[] args)
        {
            // Check for single instance
            _mutex = new Mutex(true, MutexName, out bool createdNew);
            
            if (!createdNew)
            {
                // Another instance is already running
                Logger.LogWarning("Another instance of Agent Supervisor is already running");
                MessageBox.Show(
                    "Agent Supervisor is already running.\n\nPlease check the system tray for the application icon.",
                    "Agent Supervisor Already Running",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new BotApplicationContext());
            }
            finally
            {
                // Release the mutex when the application exits
                _mutex?.ReleaseMutex();
                _mutex?.Dispose();
            }
        }
    }

    public class BotApplicationContext : ApplicationContext
    {
        private GitHubService? _gitHubService;
        private NotificationHistory? _notificationHistory;
        private ReviewRequestService? _reviewRequestService;
        private SystemTrayManager? _systemTrayManager;
        private Configuration? _config;
        private CancellationTokenSource? _cts;
        private Task? _monitoringTask;
        private MainWindow? _mainWindow;
        private TaskbarBadgeManager? _badgeManager;

        public BotApplicationContext()
        {
            InitializeApplication();
        }

        private async void InitializeApplication()
        {
            Logger.LogInfo("Application starting");
            
            // Load or create configuration
            _config = Configuration.Load();
            
            if (string.IsNullOrEmpty(_config.PersonalAccessToken))
            {
                // Show settings dialog on first run
                var settingsForm = new SettingsForm(_config);
                if (settingsForm.ShowDialog() != DialogResult.OK || 
                    string.IsNullOrEmpty(_config.PersonalAccessToken))
                {
                    Logger.LogWarning("Application exiting - no token configured");
                    MessageBox.Show("Personal Access Token is required to run the application.", 
                        "Configuration Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Application.Exit();
                    return;
                }
            }

            // Initialize services (ReviewRequestService without badge callback first)
            _notificationHistory = new NotificationHistory(_config.MaxHistoryEntries);
            
            // Create main window for taskbar presence
            _mainWindow = new MainWindow();
            // Required to be shown in task bar
            _mainWindow.Show();
            _badgeManager = new TaskbarBadgeManager(_mainWindow);
            
            // Initialize ReviewRequestService with badge update callback
            _reviewRequestService = new ReviewRequestService(() => 
            {
                var unreadCount = _reviewRequestService?.GetNewCount() ?? 0;
                if (_mainWindow != null && !_mainWindow.IsDisposed)
                {
                    _mainWindow.Invoke(() => _badgeManager?.UpdateBadgeCount(unreadCount));
                }
            });
            
            _systemTrayManager = new SystemTrayManager(
                _notificationHistory,
                _reviewRequestService,
                OnSettingsClick,
                OnExitClick,
                OnOpenUrlClick,
                RefreshTaskbarBadge);

            var proxyUrl = _config.UseProxy ? _config.ProxyUrl : null;
            _gitHubService = new GitHubService(_config.PersonalAccessToken, proxyUrl, _reviewRequestService);

            // Verify GitHub connection
            _systemTrayManager.UpdateStatus("Connecting to GitHub...");
            var username = await _gitHubService.GetCurrentUserAsync();
            
            if (string.IsNullOrEmpty(username))
            {
                Logger.LogError("Failed to connect to GitHub");
                MessageBox.Show("Failed to connect to GitHub. Please check your Personal Access Token.", 
                    "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                OnSettingsClick();
                return;
            }

            _systemTrayManager.UpdateStatus($"Connected as {username}");
            Logger.LogInfo($"Connected to GitHub as {username}");

            // Start monitoring
            _cts = new CancellationTokenSource();
            _monitoringTask = Task.Run(() => MonitorReviews(_cts.Token));
        }

        private async Task MonitorReviews(CancellationToken cancellationToken)
        {
            Logger.LogInfo($"Monitoring started with interval: {_config!.PollingIntervalSeconds} seconds");
            
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var reviews = await _gitHubService!.GetPendingReviewsAsync();
                    var newReviewCount = 0;
                    
                    foreach (var review in reviews)
                    {
                        // Check if already notified
                        if (!_notificationHistory!.HasBeenNotified(review.Id))
                        {
                            // Show desktop notification only if enabled in settings
                            if (_config!.EnableDesktopNotifications)
                            {
                                _systemTrayManager!.ShowNotification(review);
                            }
                            
                            var entry = new Models.NotificationEntry
                            {
                                Id = review.Id,
                                Repository = review.RepositoryName,
                                PullRequestNumber = review.PullRequestNumber,
                                HtmlUrl = review.HtmlUrl,
                                Reviewer = review.User?.Login ?? "Unknown",
                                State = review.State,
                                Body = review.Body,
                                Timestamp = review.SubmittedAt,
                                NotifiedAt = DateTime.UtcNow
                            };
                            
                            _notificationHistory.Add(entry);
                            newReviewCount++;
                            
                            if (_config!.EnableDesktopNotifications)
                            {
                                Logger.LogInfo($"Desktop notification shown for review: {entry.Repository} PR#{entry.PullRequestNumber}");
                            }
                            else
                            {
                                Logger.LogInfo($"New review detected (notification disabled): {entry.Repository} PR#{entry.PullRequestNumber}");
                            }
                        }
                    }

                    // Update taskbar badge with count of unread review requests
                    var unreadCount = _reviewRequestService!.GetNewCount();
                    if (_mainWindow != null && !_mainWindow.IsDisposed)
                    {
                        _mainWindow.Invoke(() => _badgeManager!.UpdateBadgeCount(unreadCount));
                    }

                    var totalPendingCount = _reviewRequestService!.GetTotalCount();
                    if (newReviewCount > 0)
                    {
                        _systemTrayManager!.UpdateStatus($"{totalPendingCount} pending review(s) - {unreadCount} unread");
                    }
                    else
                    {
                        _systemTrayManager!.UpdateStatus($"{totalPendingCount} pending review(s) - {unreadCount} unread");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("Error during monitoring", ex);
                    _systemTrayManager?.UpdateStatus($"Error: {ex.Message}");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_config!.PollingIntervalSeconds), cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    Logger.LogInfo("Monitoring cancelled");
                    break;
                }
            }
            
            Logger.LogInfo("Monitoring stopped");
        }

        private void OnSettingsClick()
        {
            var settingsForm = new SettingsForm(_config!);
            if (settingsForm.ShowDialog() == DialogResult.OK)
            {
                // Restart monitoring with new settings
                RestartMonitoring();
            }
        }

        private void RestartMonitoring()
        {
            Logger.LogInfo("Restarting monitoring with new configuration");
            _cts?.Cancel();
            _monitoringTask?.Wait(TimeSpan.FromSeconds(5));

            var proxyUrl = _config!.UseProxy ? _config.ProxyUrl : null;
            _notificationHistory = new NotificationHistory(_config.MaxHistoryEntries);
            _reviewRequestService = new ReviewRequestService(() => 
            {
                var unreadCount = _reviewRequestService?.GetNewCount() ?? 0;
                if (_mainWindow != null && !_mainWindow.IsDisposed)
                {
                    _mainWindow.Invoke(() => _badgeManager?.UpdateBadgeCount(unreadCount));
                }
            });
            _gitHubService = new GitHubService(_config!.PersonalAccessToken, proxyUrl, _reviewRequestService);

            _cts = new CancellationTokenSource();
            _monitoringTask = Task.Run(() => MonitorReviews(_cts.Token));

            _systemTrayManager?.UpdateStatus("Monitoring restarted");
        }

        private void OnExitClick()
        {
            Logger.LogInfo("Application exiting");
            _cts?.Cancel();
            _monitoringTask?.Wait(TimeSpan.FromSeconds(5));
            _systemTrayManager?.Dispose();
            _badgeManager?.Dispose();
            _mainWindow?.Dispose();
            Application.Exit();
        }

        private void OnOpenUrlClick(string url)
        {
            try
            {
                Logger.LogInfo($"Opening URL: {url}");
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error opening URL: {url}", ex);
                MessageBox.Show($"Error opening browser: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshTaskbarBadge()
        {
            try
            {
                // Get the current unread review count from ReviewRequestService
                var unreadCount = _reviewRequestService!.GetNewCount();
                
                // Update the taskbar badge on the UI thread
                if (_mainWindow != null && !_mainWindow.IsDisposed)
                {
                    _mainWindow.Invoke(() => _badgeManager!.UpdateBadgeCount(unreadCount));
                }
                
                Logger.LogInfo($"Taskbar badge refreshed: {unreadCount} unread review(s)");
            }
            catch (Exception ex)
            {
                Logger.LogError("Error refreshing taskbar badge", ex);
            }
        }
    }
}
