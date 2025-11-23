using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reflection;

namespace AgentSupervisor
{
    class Program
    {
        private static Mutex? _mutex;

        [STAThread]
        static void Main(string[] args)
        {
            // Initialize crash dump handler early to catch any startup crashes
            CrashDumpHandler.Initialize();
            
            // Check for single instance
            _mutex = new Mutex(true, Constants.SingleInstanceMutexName, out bool createdNew);
            
            if (!createdNew)
            {
                // Another instance is already running
                Logger.LogWarning("Another instance of Agent Supervisor is already running");
                MessageBox.Show(
                    Constants.MessageAlreadyRunning,
                    Constants.MessageBoxTitleAlreadyRunning,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new AppApplicationContext());
            }
            finally
            {
                // Release the mutex when the application exits
                _mutex?.ReleaseMutex();
                _mutex?.Dispose();
            }
        }

        public static string? GetInformationalVersion()
        {
            return Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        }
    }

    public class AppApplicationContext : ApplicationContext
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
        private UpdateService? _updateService;
        private SettingsForm? _settingsForm;

        public AppApplicationContext()
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
                    MessageBox.Show(Constants.MessageTokenRequired, 
                        Constants.MessageBoxTitleWarning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                OnCheckForUpdatesClick,
                RefreshTaskbarBadge,
                ShowReviewRequestsForm,
#if ENABLE_DEV_FEATURES
                TriggerImmediatePolling,
#endif
                _config,
                OnConfigChanged
                );

            var proxyUrl = _config.UseProxy ? _config.ProxyUrl : null;
            _gitHubService = new GitHubService(_config.PersonalAccessToken, proxyUrl, _reviewRequestService);

            // Initialize update service with GitHubService
            _updateService = new UpdateService(_gitHubService);

            // Verify GitHub connection
            _systemTrayManager.UpdateStatus(Constants.StatusConnecting);
            var username = await _gitHubService.GetCurrentUserAsync();
            
            if (string.IsNullOrEmpty(username))
            {
                Logger.LogError("Failed to connect to GitHub");
                MessageBox.Show(Constants.MessageConnectionFailed, 
                    Constants.MessageBoxTitleConnectionError, MessageBoxButtons.OK, MessageBoxIcon.Error);
                OnSettingsClick();
                return;
            }

            _systemTrayManager.UpdateStatus(Localization.GetString("StatusConnectedAs", username));
            Logger.LogInfo($"Connected to GitHub as {username}");

            // Check for updates on startup if enabled
            if (_config.CheckForUpdatesOnStartup)
            {
                _ = Task.Run(async () => await CheckForUpdatesAsync(false));
            }

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
                    // Check if polling is paused (available in all builds)
                    if (_config.PausePolling)
                    {
                        var currentUnreadCount = _reviewRequestService!.GetNewCount();
                        var currentTotalCount = _reviewRequestService!.GetTotalCount();
                        _systemTrayManager!.UpdateStatus(Localization.GetString("StatusPausedFormat", currentTotalCount, currentUnreadCount));
                        Logger.LogInfo("Data polling is paused, skipping this cycle");
                        
                        // Skip to the delay without polling data
                        await Task.Delay(TimeSpan.FromSeconds(_config.PollingIntervalSeconds), cancellationToken);
                        continue;
                    }

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
                    _systemTrayManager!.UpdateStatus(Localization.GetString("StatusPendingReviews", totalPendingCount, unreadCount));
                }
                catch (Exception ex)
                {
                    Logger.LogError("Error during monitoring", ex);
                    _systemTrayManager?.UpdateStatus(Localization.GetString("StatusError", ex.Message));
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
            _monitoringTask?.Wait(TimeSpan.FromSeconds(Constants.ShutdownTimeoutSeconds));

            var proxyUrl = _config!.UseProxy ? _config.ProxyUrl : null;
            _notificationHistory = new NotificationHistory(_config.MaxHistoryEntries);
            _reviewRequestService = new ReviewRequestService();
            _gitHubService = new GitHubService(_config!.PersonalAccessToken, proxyUrl, _reviewRequestService);

            // Re-initialize update service with the new GitHubService
            _updateService = new UpdateService(_gitHubService);

            // Re-subscribe views to the new service instance
            if (_mainWindow != null && !_mainWindow.IsDisposed)
            {
                _reviewRequestService.Subscribe(_mainWindow);
            }
            if (_badgeManager != null)
            {
                _reviewRequestService.Subscribe(_badgeManager);
            }
            if (_systemTrayManager != null)
            {
                _reviewRequestService.Subscribe(_systemTrayManager);
            }

            _cts = new CancellationTokenSource();
            _monitoringTask = Task.Run(() => MonitorReviews(_cts.Token));

            _systemTrayManager?.UpdateStatus(Constants.StatusMonitoringRestarted);
        }

        private void OnExitClick()
        {
            Logger.LogInfo("Application exiting");
            _cts?.Cancel();
            _monitoringTask?.Wait(TimeSpan.FromSeconds(Constants.ShutdownTimeoutSeconds));
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
                MessageBox.Show($"Error opening browser: {ex.Message}", Constants.MessageBoxTitleError, 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void OnCheckForUpdatesClick()
        {
            await CheckForUpdatesAsync(true);
        }

        private async Task CheckForUpdatesAsync(bool manualCheck)
        {
            try
            {
                Logger.LogInfo("Checking for updates");
                
                if (manualCheck)
                {
                    _systemTrayManager?.UpdateStatus(Constants.StatusCheckingForUpdates);
                }

                var updateInfo = await _updateService!.CheckForUpdatesAsync();
                
                // Update last check time
                _config!.LastUpdateCheck = DateTime.UtcNow;
                _config.Save();

                if (updateInfo != null)
                {
                    Logger.LogInfo($"Update available: {updateInfo.Version}");
                    
                    // Check if this version was previously skipped
                    if (!manualCheck && updateInfo.Version == _config!.SkippedVersion)
                    {
                        Logger.LogInfo($"Skipping notification for version {updateInfo.Version} (user previously declined)");
                        return;
                    }
                    
                    // Show notification
                    _systemTrayManager?.ShowUpdateNotification(updateInfo);
                    
                    // Build the update message
                    var formattedDate = updateInfo.PublishedAt.ToString("MMMM dd, yyyy", Localization.CurrentCulture);
                    var message = Localization.GetString("UpdateMessageNewVersionAvailable", updateInfo.Version) + "\n\n" +
                                 Localization.GetString("UpdateMessagePublishedDate", formattedDate) + "\n\n";
                    
                    // Add pre-release warning if applicable
                    if (updateInfo.IsPreRelease)
                    {
                        message += Localization.GetString("UpdateMessagePreReleaseWarning") + "\n" +
                                  Localization.GetString("UpdateMessagePreReleaseDetails") + "\n\n";
                    }
                    
                    message += Localization.GetString("UpdateMessageDownloadPrompt");
                    
                    // Show dialog with update option
                    var result = MessageBox.Show(
                        message,
                        updateInfo.IsPreRelease ? Constants.MessageBoxTitlePreReleaseUpdateAvailable : Constants.MessageBoxTitleUpdateAvailable,
                        MessageBoxButtons.YesNo,
                        updateInfo.IsPreRelease ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

                    if (result == DialogResult.Yes)
                    {
                        // Open the release page in the default browser
                        OnOpenUrlClick(updateInfo.ReleaseUrl);
                    }
                    else
                    {
                        // User clicked No - remember this version to skip future notifications
                        _config!.SkippedVersion = updateInfo.Version;
                        _config.Save();
                        Logger.LogInfo($"User skipped version {updateInfo.Version}");
                    }
                }
                else
                {
                    Logger.LogInfo("No updates available");
                    
                    if (manualCheck)
                    {
                        MessageBox.Show(
                            Constants.MessageNoUpdatesAvailable,
                            Constants.MessageBoxTitleNoUpdatesAvailable,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error checking for updates", ex);
                
                if (manualCheck)
                {
                    MessageBox.Show(
                        $"Error checking for updates: {ex.Message}",
                        Constants.MessageBoxTitleUpdateCheckFailed,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            finally
            {
                if (manualCheck)
                {
                    // Restore previous status
                    var username = await _gitHubService!.GetCurrentUserAsync();
                    _systemTrayManager?.UpdateStatus(Localization.GetString("StatusConnectedAs", username));
                }
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

        private void UpdateBadgeAndRefreshIfVisible()
        {
            var unreadCount = _reviewRequestService?.GetNewCount() ?? 0;
            if (_mainWindow != null && !_mainWindow.IsDisposed)
            {
                _mainWindow.BeginInvoke(() => 
                {
                    _badgeManager?.UpdateBadgeCount(unreadCount);
                    _mainWindow.RefreshIfVisible();
                });
            }
        }

#if ENABLE_DEV_FEATURES
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
                _systemTrayManager?.UpdateStatus(Localization.GetString("StatusPendingReviews", totalPendingCount, unreadCount));
                
                Logger.LogInfo($"Immediate polling completed: {totalPendingCount} total, {unreadCount} unread");
            }
            catch (Exception ex)
            {
                Logger.LogError("Error during immediate polling", ex);
                _systemTrayManager?.UpdateStatus(Localization.GetString("StatusError", ex.Message));
            }
        }
#endif

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
                    _systemTrayManager?.UpdateStatus(Localization.GetString("StatusPausedFormat", totalPendingCount, unreadCount));
                }
                else
                {
                    _systemTrayManager?.UpdateStatus(Localization.GetString("StatusPendingReviews", totalPendingCount, unreadCount));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error during polling", ex);
            }
        }

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
