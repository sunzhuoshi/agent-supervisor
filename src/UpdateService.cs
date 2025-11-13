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
        private const string GitHubOwner = "sunzhuoshi";
        private const string GitHubRepo = "agent-supervisor";

        public UpdateService(string currentVersion)
        {
            _currentVersion = currentVersion;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("AgentSupervisor", currentVersion));
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
            
            Logger.LogInfo($"UpdateService initialized with current version: {currentVersion}");
        }

        public async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            try
            {
                Logger.LogInfo("Checking for updates from GitHub releases");
                var url = $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";
                
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

                // Find the Windows zip asset
                string? downloadUrl = null;
                if (root.TryGetProperty("assets", out var assets))
                {
                    foreach (var asset in assets.EnumerateArray())
                    {
                        var assetName = asset.GetProperty("name").GetString() ?? "";
                        if (assetName.EndsWith("-windows.zip", StringComparison.OrdinalIgnoreCase))
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
                    return new UpdateInfo
                    {
                        Version = latestVersion,
                        DownloadUrl = downloadUrl,
                        ReleaseUrl = releaseUrl,
                        PublishedAt = publishedAt
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
                var latest = Version.Parse(latestVersion);
                var current = Version.Parse(currentVersion);
                return latest > current;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error comparing versions: {latestVersion} vs {currentVersion}", ex);
                return false;
            }
        }

        public async Task<bool> DownloadAndInstallUpdateAsync(string downloadUrl, IProgress<int>? progress = null)
        {
            try
            {
                Logger.LogInfo($"Starting download from: {downloadUrl}");
                
                // Create temp directory for download
                var tempDir = Path.Combine(Path.GetTempPath(), "AgentSupervisor-Update");
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
                Directory.CreateDirectory(tempDir);

                var zipPath = Path.Combine(tempDir, "update.zip");
                
                // Download the update
                using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    
                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    var canReportProgress = totalBytes != -1 && progress != null;
                    
                    using var contentStream = await response.Content.ReadAsStreamAsync();
                    using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
                    
                    var buffer = new byte[8192];
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
                var extractPath = Path.Combine(tempDir, "extracted");
                ZipFile.ExtractToDirectory(zipPath, extractPath);
                Logger.LogInfo($"Update extracted to: {extractPath}");

                // Prepare the updater script
                var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var updateScriptPath = Path.Combine(tempDir, "update.bat");
                
                // Create rollback directory for old version files
                var rollbackDir = Path.Combine(currentDirectory, "rollback");
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var versionRollbackDir = Path.Combine(rollbackDir, $"version_{_currentVersion}_{timestamp}");
                
                // Backup config.json before update
                var configPath = Path.Combine(currentDirectory, "config.json");
                var configBackupPath = Path.Combine(tempDir, "config.json.backup");
                
                if (File.Exists(configPath))
                {
                    File.Copy(configPath, configBackupPath, true);
                    Logger.LogInfo($"Config backed up to: {configBackupPath}");
                }

                // Backup notification_history.json before update
                var historyPath = Path.Combine(currentDirectory, "notification_history.json");
                var historyBackupPath = Path.Combine(tempDir, "notification_history.json.backup");
                
                if (File.Exists(historyPath))
                {
                    File.Copy(historyPath, historyBackupPath, true);
                    Logger.LogInfo($"Notification history backed up to: {historyBackupPath}");
                }

                // Backup review_requests.json before update
                var reviewRequestsPath = Path.Combine(currentDirectory, "review_requests.json");
                var reviewRequestsBackupPath = Path.Combine(tempDir, "review_requests.json.backup");
                
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
timeout /t 2 /nobreak >nul

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
timeout /t 2 /nobreak >nul

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
                var excludeListPath = Path.Combine(tempDir, "exclude.txt");
                var excludeList = @"config.json
notification_history.json
review_requests.json
rollback\
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
    }
}
