namespace AgentSupervisor
{
    /// <summary>
    /// Application-wide constants
    /// </summary>
    public static class Constants
    {
        // Application Identity
        public const string ApplicationName = "Agent Supervisor";
        public const string ApplicationVersion = "1.0";
        
        // GitHub Repository
        public const string GitHubOwner = "sunzhuoshi";
        public const string GitHubRepo = "agent-supervisor";
        public const string GitHubRepoUrl = "https://github.com/sunzhuoshi/agent-supervisor";
        
        // GitHub API
        public const string GitHubApiBaseUrl = "https://api.github.com";
        public const string GitHubApiVersion = "2022-11-28";
        public const string GitHubAcceptHeader = "application/vnd.github+json";
        
        // Configuration Defaults
        public const int DefaultPollingIntervalSeconds = 60;
        public const int DefaultMaxHistoryEntries = 100;
        public const string RegistryKeyPath = @"Software\AgentSupervisor";
        
        // File Names
        public const string NotificationHistoryFileName = "notification_history.json";
        public const string ReviewRequestHistoryFileName = "review_requests.json";
        public const string ReviewRequestDetailsFileName = "review_request_details.json";
        public const string ConfigFileName = "config.json";
        public const string LogFileName = "AgentSupervisor.log";
        public const string AppIconFileName = "app_icon.ico";
        public const string IconResourcePath = "res";
        public const string CrashDumpFolder = "CrashDumps";
        
        // Logger Settings
        public const int MaxLogSizeBytes = 10 * 1024 * 1024; // 10 MB
        public const int MaxLogBackupCount = 5;
        public const string LogTimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
        
        // Crash Dump Settings
        public const int MaxCrashDumpCount = 10;
        
        // UI Dimensions - Main Window
        public const int MainWindowDefaultWidth = 600;
        public const int MainWindowDefaultHeight = 500;
        public const int MainWindowMinWidth = 500;
        public const int MainWindowMinHeight = 400;
        
        // UI Dimensions - Settings Form
        public const int SettingsFormWidth = 500;
        public const int SettingsFormHeight = 450;
        public const int SettingsPollingMinSeconds = 10;
        public const int SettingsPollingMaxSeconds = 3600;
        public const int SettingsHistoryMinEntries = 10;
        public const int SettingsHistoryMaxEntries = 1000;
        
        // UI Dimensions - About Form
        public const int AboutFormWidth = 450;
        public const int AboutFormHeight = 300;
        
        // Notification Settings
        public const int NotificationTimeoutMilliseconds = 5000;
        
        // Timeouts
        public const int ShutdownTimeoutSeconds = 5;
        public const int UpdateWaitTimeoutSeconds = 2;
        
        // Update Service
        public const string UpdateTempFolderName = "AgentSupervisor-Update";
        public const string UpdateZipFileName = "update.zip";
        public const string UpdateScriptFileName = "update.bat";
        public const string UpdateExcludeListFileName = "exclude.txt";
        public const string UpdateExtractFolderName = "extracted";
        public const string RollbackFolderName = "rollback";
        public const string WindowsZipAssetSuffix = "-windows.zip";
        
        // Backup File Extensions
        public const string ConfigBackupExtension = ".backup";
        public const string LogBackupExtension = ".bak";
        
        // Message Box Titles
        public const string MessageBoxTitleError = "Error";
        public const string MessageBoxTitleSuccess = "Success";
        public const string MessageBoxTitleWarning = "Configuration Required";
        public const string MessageBoxTitleAlreadyRunning = "Agent Supervisor Already Running";
        public const string MessageBoxTitleConnectionError = "Connection Error";
        public const string MessageBoxTitleValidationError = "Validation Error";
        public const string MessageBoxTitleUpdateAvailable = "Update Available";
        public const string MessageBoxTitlePreReleaseUpdateAvailable = "Pre-Release Update Available";
        public const string MessageBoxTitleNoUpdatesAvailable = "No Updates Available";
        public const string MessageBoxTitleUpdateCheckFailed = "Update Check Failed";
        public const string MessageBoxTitlePollingStatus = "Polling Status";
        public const string MessageBoxTitlePollingError = "Polling Error";
        public const string MessageBoxTitleConfigurationError = "Configuration Error";
        
        // Message Box Content
        public const string MessageAlreadyRunning = "Agent Supervisor is already running.\n\nPlease check the system tray for the application icon.";
        public const string MessageTokenRequired = "Personal Access Token is required to run the application.";
        public const string MessageConnectionFailed = "Failed to connect to GitHub. Please check your Personal Access Token.";
        public const string MessageTokenValidationFailed = "Personal Access Token is required.";
        public const string MessageProxyValidationFailed = "Proxy URL is required when proxy is enabled.";
        public const string MessageSettingsSaved = "Settings saved successfully.";
        public const string MessageNoUpdatesAvailable = "You are running the latest version.";
        public const string MessagePollingPaused = "Data polling paused";
        public const string MessagePollingResumed = "Data polling resumed";
        public const string MessagePollingNotConfigured = "Data polling callback is not configured.";
        
        // Menu Item Text
        public const string MenuItemReviewRequests = "Review Requests by Copilots";
        public const string MenuItemPollAtOnce = "Poll at Once";
        public const string MenuItemPausePolling = "Pause Polling";
        public const string MenuItemResumePolling = "Resume Polling";
        public const string MenuItemSettings = "Settings";
        public const string MenuItemAbout = "About";
        public const string MenuItemCheckForUpdates = "Check for Updates";
        public const string MenuItemExit = "Exit";
        
        // Status Messages
        public const string StatusConnecting = "Connecting to GitHub...";
        public const string StatusMonitoringRestarted = "Monitoring restarted";
        public const string StatusCheckingForUpdates = "Checking for updates...";
        public const string StatusPollingData = "Polling data...";
        
        // Mutex
        public const string SingleInstanceMutexName = "AgentSupervisor_SingleInstance_Mutex";
        
        // Icon Settings
        public const int SystemTrayIconSize = 16;
        public const int TaskbarIconSize = 32;
        public const float IconGradientAngle = 45f;
        public const int IconGradientStartColorArgb = unchecked((int)0xFF8A2BE2); // Purple (138, 43, 226)
        public const int IconGradientEndColorArgb = unchecked((int)0xFF007ACC); // Blue (0, 122, 204)
        
        // GitHub Search
        public const int GitHubSearchMaxResults = 50;
        public const string GitHubApiReposPrefix = "https://api.github.com/repos/";
        
        // HTTP Buffer Size
        public const int HttpBufferSize = 8192;
        
        // Version Format
        public const int SemanticVersionPartCount = 3; // Major.Minor.Patch
        public const int CIBuildVersionPartThreshold = 3; // CI builds have more than 3 parts
    }
}
