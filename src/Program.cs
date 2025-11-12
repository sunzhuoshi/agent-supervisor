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
            _systemTrayManager = new SystemTrayManager(
                _notificationHistory,
                OnSettingsClick,
                OnExitClick,
                OnOpenUrlClick);

            var proxyUrl = _config.UseProxy ? _config.ProxyUrl : null;
            _gitHubService = new GitHubService(_config.PersonalAccessToken, proxyUrl);

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
                            Logger.LogInfo($"Notification shown for review: {entry.Repository} PR#{entry.PullRequestNumber}");
                        }
                    }

                    if (reviews.Count > 0)
                    {
                        _systemTrayManager!.UpdateStatus($"Found {reviews.Count} new review(s)");
                    }
                    else
                    {
                        _systemTrayManager!.UpdateStatus("Monitoring... (No new reviews)");
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
    }
}
