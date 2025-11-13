using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace AgentSupervisor
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new BotApplicationContext());
        }
    }

    public class BotApplicationContext : ApplicationContext
    {
        private GitHubService? _gitHubService;
        private NotificationHistory? _notificationHistory;
        private SystemTrayManager? _systemTrayManager;
        private Configuration? _config;
        private CancellationTokenSource? _cts;
        private Task? _monitoringTask;
        private MainWindow? _mainWindow;
        private TaskbarBadgeManager? _badgeManager;
        private UpdateService? _updateService;

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
            
            // Create main window for taskbar presence
            _mainWindow = new MainWindow();
            // Required to be shown in task bar
            _mainWindow.Show();
            _badgeManager = new TaskbarBadgeManager(_mainWindow);
            
            _systemTrayManager = new SystemTrayManager(
                _notificationHistory,
                OnSettingsClick,
                OnExitClick,
                OnOpenUrlClick,
                OnCheckForUpdatesClick);

            var proxyUrl = _config.UseProxy ? _config.ProxyUrl : null;
            _gitHubService = new GitHubService(_config.PersonalAccessToken, proxyUrl);

            // Initialize update service
            var currentVersion = typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
            _updateService = new UpdateService(currentVersion);

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
                    var reviews = await _gitHubService!.GetPendingReviewsAsync();
                    var newReviewCount = 0;
                    
                    foreach (var review in reviews)
                    {
                        // Check if already notified
                        if (!_notificationHistory!.HasBeenNotified(review.Id))
                        {
                            _systemTrayManager!.ShowNotification(review);
                            
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
                            Logger.LogInfo($"Notification shown for review: {entry.Repository} PR#{entry.PullRequestNumber}");
                        }
                    }

                    // Update taskbar badge with total pending review count
                    var totalPendingCount = await _gitHubService!.GetPendingReviewCountAsync();
                    if (_mainWindow != null && !_mainWindow.IsDisposed)
                    {
                        _mainWindow.Invoke(() => _badgeManager!.UpdateBadgeCount(totalPendingCount));
                    }

                    if (newReviewCount > 0)
                    {
                        _systemTrayManager!.UpdateStatus($"{totalPendingCount} pending review(s) - {newReviewCount} new");
                    }
                    else
                    {
                        _systemTrayManager!.UpdateStatus($"{totalPendingCount} pending review(s)");
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
            _gitHubService = new GitHubService(_config!.PersonalAccessToken, proxyUrl);
            _notificationHistory = new NotificationHistory(_config.MaxHistoryEntries);

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
                    _systemTrayManager?.UpdateStatus("Checking for updates...");
                }

                var updateInfo = await _updateService!.CheckForUpdatesAsync();
                
                // Update last check time
                _config!.LastUpdateCheck = DateTime.UtcNow;
                _config.Save();

                if (updateInfo != null)
                {
                    Logger.LogInfo($"Update available: {updateInfo.Version}");
                    
                    // Show notification
                    _systemTrayManager?.ShowUpdateNotification(updateInfo);
                    
                    // Show dialog with update option
                    var result = MessageBox.Show(
                        $"A new version ({updateInfo.Version}) is available!\n\n" +
                        $"Published: {updateInfo.PublishedAt:MMMM dd, yyyy}\n\n" +
                        $"Would you like to download and install the update now?\n\n" +
                        $"The application will restart after the update is installed.",
                        "Update Available",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (result == DialogResult.Yes)
                    {
                        await DownloadAndInstallUpdateAsync(updateInfo);
                    }
                }
                else
                {
                    Logger.LogInfo("No updates available");
                    
                    if (manualCheck)
                    {
                        MessageBox.Show(
                            "You are running the latest version.",
                            "No Updates Available",
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
                        "Update Check Failed",
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
                    _systemTrayManager?.UpdateStatus($"Connected as {username}");
                }
            }
        }

        private async Task DownloadAndInstallUpdateAsync(UpdateInfo updateInfo)
        {
            try
            {
                Logger.LogInfo($"Downloading update: {updateInfo.Version}");
                
                // Show progress dialog
                using var progressForm = new Form
                {
                    Text = "Downloading Update",
                    Width = 400,
                    Height = 150,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterScreen,
                    MaximizeBox = false,
                    MinimizeBox = false
                };

                var progressBar = new ProgressBar
                {
                    Location = new System.Drawing.Point(20, 40),
                    Width = 340,
                    Height = 30,
                    Minimum = 0,
                    Maximum = 100
                };

                var statusLabel = new Label
                {
                    Location = new System.Drawing.Point(20, 15),
                    Width = 340,
                    Text = "Downloading update..."
                };

                progressForm.Controls.Add(statusLabel);
                progressForm.Controls.Add(progressBar);

                var progress = new Progress<int>(value =>
                {
                    if (!progressForm.IsDisposed)
                    {
                        progressBar.Value = value;
                        statusLabel.Text = $"Downloading update... {value}%";
                    }
                });

                // Show the form and start download
                progressForm.Show();
                
                var success = await _updateService!.DownloadAndInstallUpdateAsync(updateInfo.DownloadUrl, progress);

                progressForm.Close();

                if (success)
                {
                    Logger.LogInfo("Update download successful, preparing to exit");
                    MessageBox.Show(
                        "Update downloaded successfully!\n\n" +
                        "The application will now close and the update will be installed automatically.\n" +
                        "Agent Supervisor will restart after the update is complete.",
                        "Update Ready",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    // Exit the application to allow the update script to run
                    OnExitClick();
                }
                else
                {
                    Logger.LogError("Update download failed");
                    MessageBox.Show(
                        "Failed to download the update. Please try again later or download manually from GitHub.",
                        "Update Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error downloading update", ex);
                MessageBox.Show(
                    $"Error downloading update: {ex.Message}",
                    "Update Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
