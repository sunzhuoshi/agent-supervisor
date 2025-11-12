using System.Net.Http.Headers;
using System.Text.Json;
using AgentSupervisor.Models;

namespace AgentSupervisor
{
    public class GitHubService
    {
        private readonly HttpClient _httpClient;
        private readonly string _username;
        private readonly ReviewRequestHistory _reviewRequestHistory;

        public GitHubService(string personalAccessToken, string? proxyUrl = null)
        {
            var handler = new HttpClientHandler();
            
            // Configure proxy if provided
            if (!string.IsNullOrEmpty(proxyUrl))
            {
                try
                {
                    handler.Proxy = new System.Net.WebProxy(proxyUrl);
                    handler.UseProxy = true;
                    Logger.LogInfo($"Using proxy: {proxyUrl}");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to configure proxy: {proxyUrl}", ex);
                }
            }

            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("AgentSupervisor", "1.0"));
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", personalAccessToken);
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

            _username = string.Empty;
            _reviewRequestHistory = new ReviewRequestHistory();
            Logger.LogInfo("GitHubService initialized");
        }

        public async Task<string> GetCurrentUserAsync()
        {
            if (!string.IsNullOrEmpty(_username))
            {
                return _username;
            }

            try
            {
                var url = "https://api.github.com/user";
                Logger.LogInfo($"HTTP GET {url}");
                
                var startTime = DateTime.UtcNow;
                var response = await _httpClient.GetAsync(url);
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                
                Logger.LogInfo($"HTTP Response: {(int)response.StatusCode} {response.StatusCode} | {elapsed:F0}ms | {url}");
                
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var login = doc.RootElement.GetProperty("login").GetString();
                Logger.LogInfo($"Current user: {login}");
                return login ?? string.Empty;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error getting current user", ex);
                return string.Empty;
            }
        }

        public async Task<List<PullRequestReview>> GetPendingReviewsAsync()
        {
            var reviews = new List<PullRequestReview>();
            var currentRequestIds = new List<string>();

            try
            {
                Logger.LogInfo("Fetching pending review requests");
                var username = await GetCurrentUserAsync();
                if (string.IsNullOrEmpty(username))
                {
                    Logger.LogWarning("Unable to get current username");
                    return reviews;
                }

                // Get all pull requests where the user is requested as a reviewer
                var searchUrl = $"https://api.github.com/search/issues?q=type:pr+review-requested:{username}+state:open&sort=updated&per_page=50";
                Logger.LogInfo($"HTTP GET {searchUrl}");
                
                var startTime = DateTime.UtcNow;
                var response = await _httpClient.GetAsync(searchUrl);
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                
                Logger.LogInfo($"HTTP Response: {(int)response.StatusCode} {response.StatusCode} | {elapsed:F0}ms | {searchUrl}");
                
                if (!response.IsSuccessStatusCode)
                {
                    Logger.LogError($"Error fetching PRs: {response.StatusCode}");
                    return reviews;
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                
                if (!doc.RootElement.TryGetProperty("items", out var items))
                {
                    Logger.LogInfo("No items found in search results");
                    return reviews;
                }

                Logger.LogInfo($"Found {items.GetArrayLength()} review requests to check");

                foreach (var item in items.EnumerateArray())
                {
                    try
                    {
                        var pullRequestUrl = item.GetProperty("pull_request").GetProperty("url").GetString();
                        if (string.IsNullOrEmpty(pullRequestUrl))
                        {
                            continue;
                        }

                        // Get PR details
                        Logger.LogInfo($"HTTP GET {pullRequestUrl}");
                        startTime = DateTime.UtcNow;
                        var prResponse = await _httpClient.GetAsync(pullRequestUrl);
                        elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                        Logger.LogInfo($"HTTP Response: {(int)prResponse.StatusCode} {prResponse.StatusCode} | {elapsed:F0}ms | {pullRequestUrl}");
                        
                        if (!prResponse.IsSuccessStatusCode)
                        {
                            continue;
                        }

                        var prJson = await prResponse.Content.ReadAsStringAsync();
                        using var prDoc = JsonDocument.Parse(prJson);
                        
                        var repoFullName = prDoc.RootElement.GetProperty("base")
                            .GetProperty("repo").GetProperty("full_name").GetString() ?? "";
                        var prNumber = prDoc.RootElement.GetProperty("number").GetInt32();
                        var prId = prDoc.RootElement.GetProperty("id").GetInt64();
                        var htmlUrl = prDoc.RootElement.GetProperty("html_url").GetString() ?? "";
                        var title = prDoc.RootElement.GetProperty("title").GetString() ?? "";
                        var createdAt = prDoc.RootElement.TryGetProperty("created_at", out var created)
                            ? DateTime.Parse(created.GetString() ?? DateTime.UtcNow.ToString())
                            : DateTime.UtcNow;
                        
                        // Create unique identifier for this review request
                        var requestId = $"{repoFullName}#{prNumber}";
                        currentRequestIds.Add(requestId);
                        
                        // Check if this is a new review request
                        if (!_reviewRequestHistory.HasBeenSeen(requestId))
                        {
                            // Create a PullRequestReview object to represent the review request
                            var review = new PullRequestReview
                            {
                                Id = prId,
                                HtmlUrl = htmlUrl,
                                State = "PENDING",
                                Body = title,
                                SubmittedAt = createdAt,
                                RepositoryName = repoFullName,
                                PullRequestNumber = prNumber
                            };
                            
                            // Get PR author info if available
                            if (prDoc.RootElement.TryGetProperty("user", out var userElement))
                            {
                                review.User = new User
                                {
                                    Login = userElement.GetProperty("login").GetString() ?? "",
                                    HtmlUrl = userElement.TryGetProperty("html_url", out var userHtmlUrl)
                                        ? userHtmlUrl.GetString() ?? ""
                                        : ""
                                };
                            }
                            
                            reviews.Add(review);
                            Logger.LogInfo($"New review request found: {repoFullName} PR#{prNumber}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Error processing PR item", ex);
                    }
                }

                // Save all current request IDs to persistent storage
                _reviewRequestHistory.MarkMultipleAsSeen(currentRequestIds);
                
                Logger.LogInfo($"Fetched {reviews.Count} new review requests");
            }
            catch (Exception ex)
            {
                Logger.LogError("Error fetching pending review requests", ex);
            }

            return reviews;
        }
    }
}

