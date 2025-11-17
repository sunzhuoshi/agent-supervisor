using System.Net.Http.Headers;
using System.Text.Json;

namespace AgentSupervisor
{
    public class UpdateService
    {
        private readonly HttpClient _httpClient;
        private readonly string _currentVersion;

        public UpdateService(string currentVersion)
        {
            _currentVersion = currentVersion;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue(Constants.ProductName.Replace(" ", ""), currentVersion));
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue(Constants.GitHubAcceptHeader));
            _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", Constants.GitHubApiVersion);
            
            Logger.LogInfo($"UpdateService initialized with current version: {currentVersion}");
        }

        public async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            try
            {
                Logger.LogInfo("Checking for updates from GitHub releases");
                var url = $"{Constants.GitHubApiBaseUrl}/repos/{Constants.GitHubOwner}/{Constants.GitHubRepo}/releases/latest";
                
                var startTime = DateTime.UtcNow;
                var response = await _httpClient.GetAsync(url);
                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                
                Logger.LogInfo($"HTTP Response: {(int)response.StatusCode} {response.StatusCode} | {elapsed:F0}ms | {url}");
                
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        Logger.LogInfo("No releases found");
                        return null;
                    }
                    Logger.LogError($"Error checking for updates: {response.StatusCode}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var tagName = root.GetProperty("tag_name").GetString() ?? "";
                var latestVersion = tagName.TrimStart('v');
                var releaseUrl = root.GetProperty("html_url").GetString() ?? "";
                var publishedAt = root.TryGetProperty("published_at", out var published)
                    ? DateTime.Parse(published.GetString() ?? DateTime.UtcNow.ToString())
                    : DateTime.UtcNow;

                Logger.LogInfo($"Latest release found: {latestVersion} (current: {_currentVersion})");

                // Check if this is a CI build - we don't offer CI builds as updates
                if (IsCIBuild(latestVersion))
                {
                    Logger.LogInfo($"Skipping CI build version: {latestVersion}");
                    return null;
                }

                // Find the Windows zip asset
                string? downloadUrl = null;
                if (root.TryGetProperty("assets", out var assets))
                {
                    foreach (var asset in assets.EnumerateArray())
                    {
                        var assetName = asset.GetProperty("name").GetString() ?? "";
                        if (assetName.EndsWith(Constants.WindowsZipAssetSuffix, StringComparison.OrdinalIgnoreCase))
                        {
                            downloadUrl = asset.GetProperty("browser_download_url").GetString();
                            Logger.LogInfo($"Found Windows asset: {assetName}");
                            break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(downloadUrl))
                {
                    Logger.LogWarning("No Windows asset found in release");
                    return null;
                }

                // Compare versions
                if (IsNewerVersion(latestVersion, _currentVersion))
                {
                    Logger.LogInfo($"New version available: {latestVersion}");
                    var isPreRelease = IsPreRelease(latestVersion);
                    if (isPreRelease)
                    {
                        Logger.LogInfo($"Version {latestVersion} is a pre-release");
                    }
                    
                    return new UpdateInfo
                    {
                        Version = latestVersion,
                        DownloadUrl = downloadUrl,
                        ReleaseUrl = releaseUrl,
                        PublishedAt = publishedAt,
                        IsPreRelease = isPreRelease
                    };
                }

                Logger.LogInfo("Current version is up to date");
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error checking for updates", ex);
                return null;
            }
        }

        private bool IsNewerVersion(string latestVersion, string currentVersion)
        {
            try
            {
                var latest = SemanticVersion.Parse(latestVersion);
                var current = SemanticVersion.Parse(currentVersion);
                return latest.CompareTo(current) > 0;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error comparing versions: {latestVersion} vs {currentVersion}", ex);
                return false;
            }
        }

        private bool IsPreRelease(string version)
        {
            try
            {
                var semVer = SemanticVersion.Parse(version);
                return semVer.IsPreRelease;
            }
            catch
            {
                return false;
            }
        }

        private bool IsCIBuild(string version)
        {
            try
            {
                // CI builds have format like 1.0.0.123 (4 parts)
                // Remove 'v' prefix if present
                version = version.TrimStart('v', 'V');
                
                // Split by '+' and '-' to get the base version
                var versionWithoutMetadata = version.Split('+')[0].Split('-')[0];
                
                // Check if it has 4 or more parts (CI build format)
                var parts = versionWithoutMetadata.Split('.');
                if (parts.Length > Constants.CIBuildVersionPartThreshold)
                {
                    Logger.LogInfo($"Version {version} is a CI build (has {parts.Length} parts)");
                    return true;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }
    }

    public class UpdateInfo
    {
        public string Version { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string ReleaseUrl { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
        public bool IsPreRelease { get; set; }
    }

    /// <summary>
    /// Represents a semantic version according to semver 2.0 specification.
    /// Supports format: MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]
    /// </summary>
    public class SemanticVersion : IComparable<SemanticVersion>
    {
        public int Major { get; set; }
        public int Minor { get; set; }
        public int Patch { get; set; }
        public string PreRelease { get; set; } = string.Empty;
        public string Build { get; set; } = string.Empty;

        public bool IsPreRelease => !string.IsNullOrEmpty(PreRelease);

        public static SemanticVersion Parse(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                throw new ArgumentException("Version string cannot be null or empty", nameof(version));

            var semVer = new SemanticVersion();
            
            // Remove 'v' prefix if present
            version = version.TrimStart('v', 'V');

            // Split by '+' to separate build metadata
            var parts = version.Split('+');
            if (parts.Length > 1)
            {
                semVer.Build = parts[1];
            }

            // Split by '-' to separate pre-release
            var versionParts = parts[0].Split('-');
            var mainVersion = versionParts[0];
            
            if (versionParts.Length > 1)
            {
                semVer.PreRelease = string.Join("-", versionParts.Skip(1));
            }

            // Parse major.minor.patch
            // Note: CI builds (e.g., 1.0.0.123) are filtered out before comparison
            var numbers = mainVersion.Split('.');
            if (numbers.Length < Constants.SemanticVersionPartCount)
                throw new FormatException($"Version must have at least {Constants.SemanticVersionPartCount} parts (major.minor.patch): {version}");

            semVer.Major = int.Parse(numbers[0]);
            semVer.Minor = int.Parse(numbers[1]);
            semVer.Patch = int.Parse(numbers[2]);
            // Ignore any additional parts (e.g., build number in 1.0.0.123)

            return semVer;
        }

        public int CompareTo(SemanticVersion? other)
        {
            if (other == null) return 1;

            // Compare major, minor, patch
            if (Major != other.Major) return Major.CompareTo(other.Major);
            if (Minor != other.Minor) return Minor.CompareTo(other.Minor);
            if (Patch != other.Patch) return Patch.CompareTo(other.Patch);

            // When major, minor, and patch are equal, a pre-release version has LOWER precedence
            // Example: 1.0.0-alpha < 1.0.0
            if (string.IsNullOrEmpty(PreRelease) && !string.IsNullOrEmpty(other.PreRelease))
                return 1; // This version is greater (no pre-release)
            
            if (!string.IsNullOrEmpty(PreRelease) && string.IsNullOrEmpty(other.PreRelease))
                return -1; // This version is less (has pre-release)

            // Both have pre-release, compare pre-release identifiers
            if (!string.IsNullOrEmpty(PreRelease) && !string.IsNullOrEmpty(other.PreRelease))
            {
                return ComparePreRelease(PreRelease, other.PreRelease);
            }

            // Build metadata is ignored in version precedence
            return 0;
        }

        private static int ComparePreRelease(string preRelease1, string preRelease2)
        {
            var parts1 = preRelease1.Split('.');
            var parts2 = preRelease2.Split('.');

            var minLength = Math.Min(parts1.Length, parts2.Length);

            for (int i = 0; i < minLength; i++)
            {
                var part1 = parts1[i];
                var part2 = parts2[i];

                // Try to parse as integers
                var isNum1 = int.TryParse(part1, out var num1);
                var isNum2 = int.TryParse(part2, out var num2);

                if (isNum1 && isNum2)
                {
                    // Both are numbers, compare numerically
                    if (num1 != num2) return num1.CompareTo(num2);
                }
                else if (isNum1)
                {
                    // Numbers have lower precedence than strings
                    return -1;
                }
                else if (isNum2)
                {
                    // Numbers have lower precedence than strings
                    return 1;
                }
                else
                {
                    // Both are strings, compare lexically
                    var comparison = string.CompareOrdinal(part1, part2);
                    if (comparison != 0) return comparison;
                }
            }

            // If all compared parts are equal, the one with more parts is greater
            return parts1.Length.CompareTo(parts2.Length);
        }

        public override string ToString()
        {
            var version = $"{Major}.{Minor}.{Patch}";
            if (!string.IsNullOrEmpty(PreRelease))
                version += $"-{PreRelease}";
            if (!string.IsNullOrEmpty(Build))
                version += $"+{Build}";
            return version;
        }
    }
}
