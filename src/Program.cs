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
        private SettingsForm? _settingsForm;

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

            // Initialize services
            _notificationHistory = new NotificationHistory(_config.MaxHistoryEntries);
            
            // Initialize ReviewRequestService (no callbacks needed - uses observer pattern)
            _reviewRequestService = new ReviewRequestService();
            
            // Create main window with review requests functionality
            _mainWindow = new MainWindow(
                _reviewRequestService,
                OnOpenUrlClick);
            // Required to be shown in task bar
            _mainWindow.Show();
            _badgeManager = new TaskbarBadgeManager(_mainWindow, _reviewRequestService);
            
            // Set badge manager in MainWindow for cross-updates
            _mainWindow.SetBadgeManager(_badgeManager);
            
            _systemTrayManager = new SystemTrayManager(
                _notificationHistory,
                _reviewRequestService,
                OnSettingsClick,
                OnExitClick,
                OnOpenUrlClick,
                ShowReviewRequestsForm
#if ENABLE_CI_FEATURES
                , TriggerImmediatePolling
                , _config
                , OnConfigChanged
#endif
                );

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
#if ENABLE_CI_FEATURES
                    // Check if polling is paused (CI builds only)
                    if (_config.PausePolling)
                    {
                        var currentUnreadCount = _reviewRequestService!.GetNewCount();
                        var currentTotalCount = _reviewRequestService!.GetTotalCount();
                        _systemTrayManager!.UpdateStatus($"Paused - {currentTotalCount} pending review(s) - {currentUnreadCount} unread");
                        Logger.LogInfo("Data polling is paused, skipping this cycle");
                        
                        // Skip to the delay without polling data
                        await Task.Delay(TimeSpan.FromSeconds(_config.PollingIntervalSeconds), cancellationToken);
                        continue;
                    }
#endif
                    var reviews = await _gitHubService!.GetPendingReviewsAsync();
                    var newReviewCount = 0;
                    
                    foreach (var review in reviews)
                    {
                        // Check if already notified
                        if (!_notificationHistory!.HasBeenNotified(review.Id))
                        {
                            // Show desktop notification only if enabled in settings
                            if (_config.EnableDesktopNotifications)
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
                            
                            if (_config.EnableDesktopNotifications)
                            {
                                Logger.LogInfo($"Desktop notification shown for review: {entry.Repository} PR#{entry.PullRequestNumber}");
                            }
                            else
                            {
                                Logger.LogInfo($"New review detected (notification disabled): {entry.Repository} PR#{entry.PullRequestNumber}");
                            }
                        }
                    }

                    // Update status with current counts
                    var unreadCount = _reviewRequestService!.GetNewCount();
                    var totalPendingCount = _reviewRequestService!.GetTotalCount();
                    _systemTrayManager!.UpdateStatus($"{totalPendingCount} pending review(s) - {unreadCount} unread");
                }
                catch (Exception ex)
                {
                    Logger.LogError("Error during monitoring", ex);
                    _systemTrayManager?.UpdateStatus($"Error: {ex.Message}");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_config.PollingIntervalSeconds), cancellationToken);
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
            // If form exists and is not disposed, bring it to front
            if (_settingsForm != null && !_settingsForm.IsDisposed)
            {
                _settingsForm.Activate();
                _settingsForm.BringToFront();
                return;
            }

            // Create new form instance
            _settingsForm = new SettingsForm(_config!);
            _settingsForm.FormClosed += (s, e) => _settingsForm = null;
            if (_settingsForm.ShowDialog() == DialogResult.OK)
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
            _reviewRequestService = new ReviewRequestService();
            _gitHubService = new GitHubService(_config!.PersonalAccessToken, proxyUrl, _reviewRequestService);

            // Re-subscribe views to the new service instance
            if (_mainWindow != null && !_mainWindow.IsDisposed)
            {
                _reviewRequestService.Subscribe(_mainWindow);
            }
            if (_badgeManager != null)
            {
                _reviewRequestService.Subscribe(_badgeManager);
            }

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
            _settingsForm?.Dispose();
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

#if ENABLE_CI_FEATURES
        private async void TriggerImmediatePolling()
        {
            try
            {
                Logger.LogInfo("Immediate polling triggered from menu");
                
                if (_gitHubService == null)
                {
                    Logger.LogError("GitHub service not initialized");
                    return;
                }

                _systemTrayManager?.UpdateStatus("Polling data...");
                
                // Trigger the review polling immediately
                var reviews = await _gitHubService.GetPendingReviewsAsync();
                
                // Update status
                var unreadCount = _reviewRequestService!.GetNewCount();
                var totalPendingCount = _reviewRequestService!.GetTotalCount();
                _systemTrayManager?.UpdateStatus($"{totalPendingCount} pending review(s) - {unreadCount} unread");
                
                Logger.LogInfo($"Immediate polling completed: {totalPendingCount} total, {unreadCount} unread");
            }
            catch (Exception ex)
            {
                Logger.LogError("Error during immediate polling", ex);
                _systemTrayManager?.UpdateStatus($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when configuration changes (e.g., when pause is toggled)
        /// </summary>
        private void OnConfigChanged()
        {
            try
            {
                // Reload configuration from Registry to ensure we have the latest values
                _config = Configuration.Load();
                
                Logger.LogInfo($"Configuration changed - PausePolling: {_config.PausePolling}");
                
                // Update status immediately to reflect the new state
                var unreadCount = _reviewRequestService?.GetNewCount() ?? 0;
                var totalPendingCount = _reviewRequestService?.GetTotalCount() ?? 0;
                
                if (_config.PausePolling)
                {
                    _systemTrayManager?.UpdateStatus($"Paused - {totalPendingCount} pending review(s) - {unreadCount} unread");
                }
                else
                {
                    _systemTrayManager?.UpdateStatus($"{totalPendingCount} pending review(s) - {unreadCount} unread");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error handling configuration change", ex);
            }
        }
#endif

        private void ShowReviewRequestsForm()
        {
            // Show and restore the main window
            if (_mainWindow != null && !_mainWindow.IsDisposed)
            {
                _mainWindow.RefreshAndShow();
            }
        }
    }
}
