using System;
using System.Threading;
using System.Threading.Tasks;
using GitHubCopilotAgentBot;

class Program
{
    private static GitHubService? _gitHubService;
    private static NotificationService? _notificationService;
    private static NotificationHistory? _notificationHistory;
    private static Configuration? _config;
    private static CancellationTokenSource? _cts;
    private static string? _lastPendingUrl;

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== GitHub Copilot Agent Bot ===");
        Console.WriteLine("Monitor PR reviews and get desktop notifications\n");

        // Load or create configuration
        _config = Configuration.Load();
        if (string.IsNullOrEmpty(_config.PersonalAccessToken))
        {
            _config = Configuration.CreateDefault();
        }

        if (string.IsNullOrEmpty(_config.PersonalAccessToken))
        {
            Console.WriteLine("Error: Personal Access Token is required.");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return;
        }

        // Initialize services
        _gitHubService = new GitHubService(_config.PersonalAccessToken);
        _notificationHistory = new NotificationHistory(_config.MaxHistoryEntries);
        _notificationService = new NotificationService(_notificationHistory);

        // Verify GitHub connection
        Console.WriteLine("Connecting to GitHub...");
        var username = await _gitHubService.GetCurrentUserAsync();
        if (string.IsNullOrEmpty(username))
        {
            Console.WriteLine("Error: Failed to connect to GitHub. Please check your Personal Access Token.");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine($"Connected as: {username}");
        Console.WriteLine($"Polling interval: {_config.PollingIntervalSeconds} seconds\n");

        // Start monitoring
        _cts = new CancellationTokenSource();
        var monitoringTask = Task.Run(() => MonitorReviews(_cts.Token));

        // Handle user input
        Console.WriteLine("Commands:");
        Console.WriteLine("  H - Show notification history");
        Console.WriteLine("  O - Open last pending PR in browser");
        Console.WriteLine("  R - Refresh now");
        Console.WriteLine("  Q - Quit");
        Console.WriteLine();

        while (true)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                
                switch (char.ToUpper(key.KeyChar))
                {
                    case 'H':
                        _notificationService.DisplayHistory(20);
                        break;
                    
                    case 'O':
                        if (!string.IsNullOrEmpty(_lastPendingUrl))
                        {
                            _notificationService.OpenInBrowser(_lastPendingUrl);
                        }
                        else
                        {
                            Console.WriteLine("No pending PR to open.");
                        }
                        break;
                    
                    case 'R':
                        Console.WriteLine("Refreshing now...");
                        break;
                    
                    case 'Q':
                        Console.WriteLine("\nShutting down...");
                        _cts.Cancel();
                        await monitoringTask;
                        return;
                }
            }

            await Task.Delay(100);
        }
    }

    static async Task MonitorReviews(CancellationToken cancellationToken)
    {
        Console.WriteLine($"Monitoring started. Checking every {_config!.PollingIntervalSeconds} seconds...\n");
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var reviews = await _gitHubService!.GetPendingReviewsAsync();
                
                foreach (var review in reviews)
                {
                    _notificationService!.ShowNotification(review);
                    _lastPendingUrl = review.HtmlUrl;
                }

                if (reviews.Count > 0)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Found {reviews.Count} new review(s)");
                }
                else
                {
                    Console.Write($"\r[{DateTime.Now:HH:mm:ss}] Monitoring... (No new reviews)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] Error during monitoring: {ex.Message}");
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

        Console.WriteLine("\nMonitoring stopped.");
    }
}
