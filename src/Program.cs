using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace GitHubCopilotAgentBot
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
            // Load or create configuration
            _config = Configuration.Load();
            
            if (string.IsNullOrEmpty(_config.PersonalAccessToken))
            {
                // Show settings dialog on first run
                var settingsForm = new SettingsForm(_config);
                if (settingsForm.ShowDialog() != DialogResult.OK || 
                    string.IsNullOrEmpty(_config.PersonalAccessToken))
                {
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

            _gitHubService = new GitHubService(_config.PersonalAccessToken);

            // Verify GitHub connection
            _systemTrayManager.UpdateStatus("Connecting to GitHub...");
            var username = await _gitHubService.GetCurrentUserAsync();
            
            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("Failed to connect to GitHub. Please check your Personal Access Token.", 
                    "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                OnSettingsClick();
                return;
            }

            _systemTrayManager.UpdateStatus($"Connected as {username}");

            // Start monitoring
            _cts = new CancellationTokenSource();
            _monitoringTask = Task.Run(() => MonitorReviews(_cts.Token));
        }

        private async Task MonitorReviews(CancellationToken cancellationToken)
        {
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
                    _systemTrayManager?.UpdateStatus($"Error: {ex.Message}");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_config!.PollingIntervalSeconds), cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
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
            _cts?.Cancel();
            _monitoringTask?.Wait(TimeSpan.FromSeconds(5));

            _gitHubService = new GitHubService(_config!.PersonalAccessToken);
            _notificationHistory = new NotificationHistory(_config.MaxHistoryEntries);

            _cts = new CancellationTokenSource();
            _monitoringTask = Task.Run(() => MonitorReviews(_cts.Token));

            _systemTrayManager?.UpdateStatus("Monitoring restarted");
        }

        private void OnExitClick()
        {
            _cts?.Cancel();
            _monitoringTask?.Wait(TimeSpan.FromSeconds(5));
            _systemTrayManager?.Dispose();
            Application.Exit();
        }

        private void OnOpenUrlClick(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening browser: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
