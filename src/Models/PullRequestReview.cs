using System.Text.Json.Serialization;

namespace AgentSupervisor.Models
{
    public class PullRequestReview
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;

        [JsonPropertyName("user")]
        public User? User { get; set; }

        [JsonPropertyName("submitted_at")]
        public DateTime SubmittedAt { get; set; }

        [JsonPropertyName("pull_request_url")]
        public string PullRequestUrl { get; set; } = string.Empty;

        public string RepositoryName { get; set; } = string.Empty;
        public int PullRequestNumber { get; set; }
    }

    public class User
    {
        [JsonPropertyName("login")]
        public string Login { get; set; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;
    }
}
