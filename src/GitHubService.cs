using System.Net.Http.Headers;
using System.Text.Json;
using AgentSupervisor.Models;

namespace AgentSupervisor
{
    public class GitHubService
    {
        private readonly HttpClient _httpClient;
        private string _username;
        private readonly ReviewRequestHistory _reviewRequestHistory;
        private readonly ReviewRequestService? _reviewRequestService;

        public GitHubService(string personalAccessToken, string? proxyUrl = null, ReviewRequestService? reviewRequestService = null)
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
                new ProductInfoHeaderValue(Constants.ProductName.Replace(" ", ""), Constants.ApplicationVersion));
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", personalAccessToken);
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue(Constants.GitHubAcceptHeader));
            _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", Constants.GitHubApiVersion);

            _username = string.Empty;
            _reviewRequestHistory = new ReviewRequestHistory();
            _reviewRequestService = reviewRequestService;
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
                var url = $"{Constants.GitHubApiBaseUrl}/user";
                Logger.LogInfo($"HTTP GET {url}");
                
                var startTime = DateTime.UtcNow;
                var response = await _httpClient.GetAsync(url);
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                
                Logger.LogInfo($"HTTP Response: {(int)response.StatusCode} {response.StatusCode} | {elapsed:F0}ms | {url}");
                
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var login = doc.RootElement.GetProperty("login").GetString();
                _username = login ?? string.Empty;
                Logger.LogInfo($"Current user: {_username}");
                return _username;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error getting current user", ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Attempts to parse a DateTime from a JSON property.
        /// </summary>
        /// <param name="item">The JSON element containing the property</param>
        /// <param name="propertyName">The name of the property to parse</param>
        /// <param name="result">The parsed DateTime, or DateTime.UtcNow if parsing fails</param>
        /// <returns>True if parsing was successful, false otherwise</returns>
        private static bool TryParseJsonDateTime(JsonElement item, string propertyName, out DateTime result)
        {
            result = DateTime.UtcNow;
            
            if (item.TryGetProperty(propertyName, out var property) && 
                property.ValueKind == JsonValueKind.String)
            {
                var dateStr = property.GetString();
                if (!string.IsNullOrEmpty(dateStr) && DateTime.TryParse(dateStr, out var parsed))
                {
                    result = parsed;
                    return true;
                }
            }
            
            return false;
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
                var searchUrl = $"{Constants.GitHubApiBaseUrl}/search/issues?q=type:pr+review-requested:{username}+state:open&sort=updated&per_page={Constants.GitHubSearchMaxResults}";
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
                        // Extract all necessary data directly from the search result
                        // This avoids making an additional API call for each PR
                        
                        // Get repository full name from repository_url
                        // Format: "https://api.github.com/repos/owner/repo"
                        var repositoryUrl = item.GetProperty("repository_url").GetString();
                        if (string.IsNullOrEmpty(repositoryUrl))
                        {
                            continue;
                        }
                        
                        // Parse repository full name from the URL
                        var repoFullName = repositoryUrl.Replace(Constants.GitHubApiReposPrefix, "");
                        
                        var prNumber = item.GetProperty("number").GetInt32();
                        var prId = item.GetProperty("id").GetInt64();
                        var htmlUrl = item.GetProperty("html_url").GetString() ?? "";
                        var title = item.GetProperty("title").GetString() ?? "";
                        
                        TryParseJsonDateTime(item, "created_at", out var createdAt);
                        TryParseJsonDateTime(item, "updated_at", out var updatedAt);
                        
                        // Get PR author info if available
                        var authorLogin = "Unknown";
                        var authorHtmlUrl = "";
                        if (item.TryGetProperty("user", out var userElement))
                        {
                            authorLogin = userElement.GetProperty("login").GetString() ?? "Unknown";
                            authorHtmlUrl = userElement.TryGetProperty("html_url", out var userUrl)
                                ? userUrl.GetString() ?? ""
                                : "";
                        }
                        
                        // Create unique identifier for this review request
                        var requestId = $"{repoFullName}#{prNumber}";
                        currentRequestIds.Add(requestId);
                        
                        // Add to ReviewRequestService if available
                        if (_reviewRequestService != null)
                        {
                            var entry = new ReviewRequestEntry
                            {
                                Id = requestId,
                                Repository = repoFullName,
                                PullRequestNumber = prNumber,
                                HtmlUrl = htmlUrl,
                                Title = title,
                                Author = authorLogin,
                                CreatedAt = createdAt,
                                UpdatedAt = updatedAt
                            };
                            _reviewRequestService.AddOrUpdate(entry);
                        }
                        
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
                            
                            // Set PR author info
                            review.User = new User
                            {
                                Login = authorLogin,
                                HtmlUrl = authorHtmlUrl
                            };
                            
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
                
                // Remove stale requests from ReviewRequestService
                if (_reviewRequestService != null)
                {
                    _reviewRequestService.RemoveStaleRequests(currentRequestIds);
                }
                
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

