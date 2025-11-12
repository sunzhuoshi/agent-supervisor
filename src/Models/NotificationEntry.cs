using System.Text.Json.Serialization;

namespace AgentSupervisor.Models
{
    public class NotificationEntry
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("repository")]
        public string Repository { get; set; } = string.Empty;

        [JsonPropertyName("pull_request_number")]
        public int PullRequestNumber { get; set; }

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("reviewer")]
        public string Reviewer { get; set; } = string.Empty;

        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("notified_at")]
        public DateTime NotifiedAt { get; set; }
    }
}
