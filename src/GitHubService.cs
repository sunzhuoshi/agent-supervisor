using System.Net.Http.Headers;
using System.Text.Json;
using GitHubCopilotAgentBot.Models;

namespace GitHubCopilotAgentBot
{
    public class GitHubService
    {
        private readonly HttpClient _httpClient;
        private readonly string _username;
        private readonly HashSet<long> _seenReviewIds = new HashSet<long>();

        public GitHubService(string personalAccessToken)
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("GitHubCopilotAgentBot", "1.0"));
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", personalAccessToken);
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2019-11-05");

            _username = string.Empty;
        }

        public async Task<string> GetCurrentUserAsync()
        {
            if (!string.IsNullOrEmpty(_username))
            {
                return _username;
            }

            try
            {
                var response = await _httpClient.GetAsync("https://api.github.com/user");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var login = doc.RootElement.GetProperty("login").GetString();
                return login ?? string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting current user: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task<List<PullRequestReview>> GetPendingReviewsAsync()
        {
            var reviews = new List<PullRequestReview>();

            try
            {
                var username = await GetCurrentUserAsync();
                if (string.IsNullOrEmpty(username))
                {
                    Console.WriteLine("Unable to get current username.");
                    return reviews;
                }

                // Get all pull requests where the user is requested as a reviewer
                var searchUrl = $"https://api.github.com/search/issues?q=type:pr+review-requested:{username}+state:open&sort=updated&per_page=50";
                var response = await _httpClient.GetAsync(searchUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error fetching PRs: {response.StatusCode}");
                    return reviews;
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                
                if (!doc.RootElement.TryGetProperty("items", out var items))
                {
                    return reviews;
                }

                foreach (var item in items.EnumerateArray())
                {
                    try
                    {
                        var pullRequestUrl = item.GetProperty("pull_request").GetProperty("url").GetString();
                        if (string.IsNullOrEmpty(pullRequestUrl))
                        {
                            continue;
                        }

                        // Get PR details to fetch reviews
                        var prResponse = await _httpClient.GetAsync(pullRequestUrl);
                        if (!prResponse.IsSuccessStatusCode)
                        {
                            continue;
                        }

                        var prJson = await prResponse.Content.ReadAsStringAsync();
                        using var prDoc = JsonDocument.Parse(prJson);
                        
                        var repoFullName = prDoc.RootElement.GetProperty("base")
                            .GetProperty("repo").GetProperty("full_name").GetString() ?? "";
                        var prNumber = prDoc.RootElement.GetProperty("number").GetInt32();
                        
                        // Fetch reviews for this PR
                        var reviewsUrl = $"{pullRequestUrl}/reviews";
                        var reviewsResponse = await _httpClient.GetAsync(reviewsUrl);
                        
                        if (reviewsResponse.IsSuccessStatusCode)
                        {
                            var reviewsJson = await reviewsResponse.Content.ReadAsStringAsync();
                            using var reviewsDoc = JsonDocument.Parse(reviewsJson);
                            
                            foreach (var reviewElement in reviewsDoc.RootElement.EnumerateArray())
                            {
                                var review = ParseReview(reviewElement, repoFullName, prNumber);
                                if (review != null && !_seenReviewIds.Contains(review.Id))
                                {
                                    reviews.Add(review);
                                    _seenReviewIds.Add(review.Id);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing PR item: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching pending reviews: {ex.Message}");
            }

            return reviews;
        }

        private PullRequestReview? ParseReview(JsonElement element, string repoName, int prNumber)
        {
            try
            {
                var review = new PullRequestReview
                {
                    Id = element.GetProperty("id").GetInt64(),
                    HtmlUrl = element.GetProperty("html_url").GetString() ?? "",
                    State = element.GetProperty("state").GetString() ?? "",
                    Body = element.TryGetProperty("body", out var body) ? body.GetString() ?? "" : "",
                    SubmittedAt = element.TryGetProperty("submitted_at", out var submitted) 
                        ? DateTime.Parse(submitted.GetString() ?? DateTime.UtcNow.ToString()) 
                        : DateTime.UtcNow,
                    RepositoryName = repoName,
                    PullRequestNumber = prNumber
                };

                if (element.TryGetProperty("user", out var userElement))
                {
                    review.User = new User
                    {
                        Login = userElement.GetProperty("login").GetString() ?? "",
                        HtmlUrl = userElement.TryGetProperty("html_url", out var htmlUrl) 
                            ? htmlUrl.GetString() ?? "" 
                            : ""
                    };
                }

                return review;
            }
            catch
            {
                return null;
            }
        }
    }
}
