using System.Text.Json.Serialization;

namespace AgentSupervisor.Models
{
    public class ReviewRequestEntry
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty; // Format: "repo#prNumber"

        [JsonPropertyName("repository")]
        public string Repository { get; set; } = string.Empty;

        [JsonPropertyName("pull_request_number")]
        public int PullRequestNumber { get; set; }

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("author")]
        public string Author { get; set; } = string.Empty;

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("is_new")]
        public bool IsNew { get; set; }

        [JsonPropertyName("added_at")]
        public DateTime AddedAt { get; set; }
    }
}
