using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text.Json;

namespace AgentSupervisor
{
    public class UpdateService
    {
        private readonly HttpClient _httpClient;
        private readonly string _currentVersion;
        private readonly GitHubService _gitHubService;

        public UpdateService(string currentVersion, GitHubService gitHubService)
        {
            _currentVersion = currentVersion;
            _gitHubService = gitHubService;
            
            // HttpClient is still needed for downloading release assets (non-API)
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue(Constants.ProductName.Replace(" ", ""), currentVersion));
            
            Logger.LogInfo($"UpdateService initialized with current version: {currentVersion}");
        }

        public async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            try
            {
                Logger.LogInfo("Checking for updates from GitHub releases");
                
                // Use GitHubService to fetch the latest release
                var releaseInfo = await _gitHubService.GetLatestReleaseAsync(Constants.GitHubOwner, Constants.GitHubRepo);
                
                if (releaseInfo == null)
                {
                    return null;
                }

                var tagName = releaseInfo["tag_name"] as string ?? "";
                var latestVersion = tagName.TrimStart('v');
                var releaseUrl = releaseInfo["html_url"] as string ?? "";
                var publishedAt = releaseInfo["published_at"] as DateTime? ?? DateTime.UtcNow;

                Logger.LogInfo($"Latest release found: {latestVersion} (current: {_currentVersion})");

                // Check if this is a CI build - we don't offer CI builds as updates
                if (IsCIBuild(latestVersion))
                {
                    Logger.LogInfo($"Skipping CI build version: {latestVersion}");
                    return null;
                }

                // Find the Windows zip asset
                string? downloadUrl = null;
                var assets = releaseInfo["assets"] as List<Dictionary<string, string>>;
                if (assets != null)
                {
                    foreach (var asset in assets)
                    {
                        var assetName = asset["name"];
                        if (assetName.EndsWith(Constants.WindowsZipAssetSuffix, StringComparison.OrdinalIgnoreCase))
                        {
                            downloadUrl = asset["browser_download_url"];
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

        public async Task<bool> DownloadAndInstallUpdateAsync(string downloadUrl, IProgress<int>? progress = null)
        {
            try
            {
                Logger.LogInfo($"Starting download from: {downloadUrl}");
                
                // Create temp directory for download
                var tempDir = Path.Combine(Path.GetTempPath(), Constants.UpdateTempFolderName);
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
                Directory.CreateDirectory(tempDir);

                var zipPath = Path.Combine(tempDir, Constants.UpdateZipFileName);
                
                // Download the update
                using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    
                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    var canReportProgress = totalBytes != -1 && progress != null;
                    
                    using var contentStream = await response.Content.ReadAsStreamAsync();
                    using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, Constants.HttpBufferSize, true);
                    
                    var buffer = new byte[Constants.HttpBufferSize];
                    long totalRead = 0;
                    int bytesRead;
                    
                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalRead += bytesRead;
                        
                        if (canReportProgress)
                        {
                            var progressPercentage = (int)((totalRead * 100) / totalBytes);
                            progress!.Report(progressPercentage);
                        }
                    }
                }

                Logger.LogInfo($"Download completed: {zipPath}");

                // Extract the update
                var extractPath = Path.Combine(tempDir, Constants.UpdateExtractFolderName);
                ZipFile.ExtractToDirectory(zipPath, extractPath);
                Logger.LogInfo($"Update extracted to: {extractPath}");

                // Prepare the updater script
                var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var updateScriptPath = Path.Combine(tempDir, Constants.UpdateScriptFileName);
                
                // Create rollback directory for old version files
                var rollbackDir = Path.Combine(currentDirectory, Constants.RollbackFolderName);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var versionRollbackDir = Path.Combine(rollbackDir, $"version_{_currentVersion}_{timestamp}");
                
                // Backup config.json before update
                var configPath = Path.Combine(currentDirectory, Constants.ConfigFileName);
                var configBackupPath = Path.Combine(tempDir, Constants.ConfigFileName + Constants.ConfigBackupExtension);
                
                if (File.Exists(configPath))
                {
                    File.Copy(configPath, configBackupPath, true);
                    Logger.LogInfo($"Config backed up to: {configBackupPath}");
                }

                // Backup notification_history.json before update
                var historyPath = Path.Combine(currentDirectory, Constants.NotificationHistoryFileName);
                var historyBackupPath = Path.Combine(tempDir, Constants.NotificationHistoryFileName + Constants.ConfigBackupExtension);
                
                if (File.Exists(historyPath))
                {
                    File.Copy(historyPath, historyBackupPath, true);
                    Logger.LogInfo($"Notification history backed up to: {historyBackupPath}");
                }

                // Backup review_requests.json before update
                var reviewRequestsPath = Path.Combine(currentDirectory, Constants.ReviewRequestHistoryFileName);
                var reviewRequestsBackupPath = Path.Combine(tempDir, Constants.ReviewRequestHistoryFileName + Constants.ConfigBackupExtension);
                
                if (File.Exists(reviewRequestsPath))
                {
                    File.Copy(reviewRequestsPath, reviewRequestsBackupPath, true);
                    Logger.LogInfo($"Review requests history backed up to: {reviewRequestsBackupPath}");
                }

                // Create update script that will:
                // 1. Wait for the application to close
                // 2. Backup old version files to rollback directory
                // 3. Copy new files to application directory
                // 4. Restore config.json, notification_history.json, and review_requests.json
                // 5. Restart the application
                var updateScript = $@"@echo off
echo Waiting for Agent Supervisor to close...
timeout /t {Constants.UpdateWaitTimeoutSeconds} /nobreak >nul

echo Creating rollback backup of old version...
if not exist ""{rollbackDir}"" mkdir ""{rollbackDir}""
if not exist ""{versionRollbackDir}"" mkdir ""{versionRollbackDir}""

echo Backing up old version files...
xcopy /E /I /Y /EXCLUDE:""{Path.Combine(tempDir, "exclude.txt")}"" ""{currentDirectory}*"" ""{versionRollbackDir}""

echo Installing update...
xcopy /E /I /Y ""{extractPath}\*"" ""{currentDirectory}""

echo Restoring configuration...
if exist ""{configBackupPath}"" (
    copy /Y ""{configBackupPath}"" ""{configPath}""
)

echo Restoring notification history...
if exist ""{historyBackupPath}"" (
    copy /Y ""{historyBackupPath}"" ""{historyPath}""
)

echo Restoring review requests history...
if exist ""{reviewRequestsBackupPath}"" (
    copy /Y ""{reviewRequestsBackupPath}"" ""{reviewRequestsPath}""
)

echo.
echo ============================================================
echo Rollback Information:
echo Old version backed up to: {versionRollbackDir}
echo To rollback, close the app and copy files from this folder
echo back to the application directory.
echo ============================================================
echo.

echo Cleaning up temporary update files...
timeout /t {Constants.UpdateWaitTimeoutSeconds} /nobreak >nul

echo Starting Agent Supervisor...
start """" ""{Path.Combine(currentDirectory, "AgentSupervisor.exe")}""

echo Update completed successfully!
timeout /t 3 /nobreak >nul

echo Cleaning up temporary files...
rd /s /q ""{tempDir}""
";

                File.WriteAllText(updateScriptPath, updateScript);
                Logger.LogInfo($"Update script created: {updateScriptPath}");

                // Create exclude list for backup to avoid backing up user data and rollback folders
                var excludeListPath = Path.Combine(tempDir, Constants.UpdateExcludeListFileName);
                var excludeList = $@"{Constants.ConfigFileName}
{Constants.NotificationHistoryFileName}
{Constants.ReviewRequestHistoryFileName}
{Constants.RollbackFolderName}\
";
                File.WriteAllText(excludeListPath, excludeList);
                Logger.LogInfo($"Exclude list created: {excludeListPath}");

                // Start the update script
                var startInfo = new ProcessStartInfo
                {
                    FileName = updateScriptPath,
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process.Start(startInfo);
                Logger.LogInfo("Update script started");

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError("Error downloading and installing update", ex);
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
