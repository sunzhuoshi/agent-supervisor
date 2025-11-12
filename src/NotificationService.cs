using System.Runtime.InteropServices;
using System.Diagnostics;
using GitHubCopilotAgentBot.Models;

namespace GitHubCopilotAgentBot
{
    public class NotificationService
    {
        private readonly NotificationHistory _history;

        public NotificationService(NotificationHistory history)
        {
            _history = history;
        }

        public void ShowNotification(PullRequestReview review)
        {
            // Check if already notified
            if (_history.HasBeenNotified(review.Id))
            {
                return;
            }

            // Create notification entry
            var entry = new NotificationEntry
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

            // Display custom notification in console
            DisplayConsoleNotification(entry);

            // Add to history
            _history.Add(entry);
        }

        private void DisplayConsoleNotification(NotificationEntry entry)
        {
            Console.WriteLine("\n" + new string('=', 80));
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("NEW PR REVIEW NOTIFICATION");
            Console.ResetColor();
            Console.WriteLine(new string('=', 80));
            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Repository: {entry.Repository}");
            Console.WriteLine($"PR #{entry.PullRequestNumber}");
            Console.ResetColor();
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Reviewer: {entry.Reviewer}");
            Console.ResetColor();
            
            Console.WriteLine($"State: {entry.State}");
            
            if (!string.IsNullOrEmpty(entry.Body))
            {
                Console.WriteLine($"\nComment:");
                Console.WriteLine(entry.Body.Length > 200 
                    ? entry.Body.Substring(0, 200) + "..." 
                    : entry.Body);
            }
            
            Console.WriteLine($"\nTime: {entry.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"\nPress 'O' to open PR in browser: {entry.HtmlUrl}");
            Console.ResetColor();
            Console.WriteLine(new string('=', 80) + "\n");

            // Play a beep sound to get attention
            try
            {
                Console.Beep(800, 200);
                Thread.Sleep(100);
                Console.Beep(1000, 200);
            }
            catch
            {
                // Beep not supported on all systems
            }
        }

        public void OpenInBrowser(string url)
        {
            try
            {
                // Windows-specific approach to open URL in default browser
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
                Console.WriteLine($"Opened URL in browser: {url}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening browser: {ex.Message}");
            }
        }

        public void DisplayHistory(int count = 10)
        {
            var entries = _history.GetRecent(count);
            
            if (entries.Count == 0)
            {
                Console.WriteLine("No notification history.");
                return;
            }

            Console.WriteLine($"\n=== Recent Notifications (showing {entries.Count}) ===\n");
            
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                Console.WriteLine($"{i + 1}. [{entry.NotifiedAt:MM/dd HH:mm}] {entry.Repository} PR#{entry.PullRequestNumber}");
                Console.WriteLine($"   {entry.State} by {entry.Reviewer}");
                Console.WriteLine($"   {entry.HtmlUrl}");
                Console.WriteLine();
            }
        }
    }
}
