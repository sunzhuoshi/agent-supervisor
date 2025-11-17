namespace AgentSupervisor
{
    /// <summary>
    /// Application-wide constants
    /// </summary>
    public static class Constants
    {
        // The product name used for HTTP User-Agent and other non-localized references.
        // This value is not localized.
        public const string ProductName = "AgentSupervisor";

        // Application Identity
        public static string ApplicationName => Localization.GetString("ApplicationName");
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
        
        // Update Service
        public const string WindowsZipAssetSuffix = "-windows.zip";
        
        // Backup File Extensions
        public const string LogBackupExtension = ".bak";
        
        // Message Box Titles
        public static string MessageBoxTitleError => Localization.GetString("MessageBoxTitleError");
        public static string MessageBoxTitleSuccess => Localization.GetString("MessageBoxTitleSuccess");
        public static string MessageBoxTitleWarning => Localization.GetString("MessageBoxTitleWarning");
        public static string MessageBoxTitleAlreadyRunning => Localization.GetString("MessageBoxTitleAlreadyRunning");
        public static string MessageBoxTitleConnectionError => Localization.GetString("MessageBoxTitleConnectionError");
        public static string MessageBoxTitleValidationError => Localization.GetString("MessageBoxTitleValidationError");
        public static string MessageBoxTitleUpdateAvailable => Localization.GetString("MessageBoxTitleUpdateAvailable");
        public static string MessageBoxTitlePreReleaseUpdateAvailable => Localization.GetString("MessageBoxTitlePreReleaseUpdateAvailable");
        public static string MessageBoxTitleNoUpdatesAvailable => Localization.GetString("MessageBoxTitleNoUpdatesAvailable");
        public static string MessageBoxTitleUpdateCheckFailed => Localization.GetString("MessageBoxTitleUpdateCheckFailed");
        public static string MessageBoxTitlePollingStatus => Localization.GetString("MessageBoxTitlePollingStatus");
        public static string MessageBoxTitlePollingError => Localization.GetString("MessageBoxTitlePollingError");
        public static string MessageBoxTitleConfigurationError => Localization.GetString("MessageBoxTitleConfigurationError");
        
        // Message Box Content
        public static string MessageAlreadyRunning => Localization.GetString("MessageAlreadyRunning");
        public static string MessageTokenRequired => Localization.GetString("MessageTokenRequired");
        public static string MessageConnectionFailed => Localization.GetString("MessageConnectionFailed");
        public static string MessageTokenValidationFailed => Localization.GetString("MessageTokenValidationFailed");
        public static string MessageProxyValidationFailed => Localization.GetString("MessageProxyValidationFailed");
        public static string MessageSettingsSaved => Localization.GetString("MessageSettingsSaved");
        public static string MessageNoUpdatesAvailable => Localization.GetString("MessageNoUpdatesAvailable");
        public static string MessagePollingPaused => Localization.GetString("MessagePollingPaused");
        public static string MessagePollingResumed => Localization.GetString("MessagePollingResumed");
        public static string MessagePollingNotConfigured => Localization.GetString("MessagePollingNotConfigured");
        
        // Menu Item Text
        public static string MenuItemReviewRequests => Localization.GetString("MenuItemReviewRequests");
        public static string MenuItemPollAtOnce => Localization.GetString("MenuItemPollAtOnce");
        public static string MenuItemPausePolling => Localization.GetString("MenuItemPausePolling");
        public static string MenuItemResumePolling => Localization.GetString("MenuItemResumePolling");
        public static string MenuItemSettings => Localization.GetString("MenuItemSettings");
        public static string MenuItemAbout => Localization.GetString("MenuItemAbout");
        public static string MenuItemCheckForUpdates => Localization.GetString("MenuItemCheckForUpdates");
        public static string MenuItemExit => Localization.GetString("MenuItemExit");
        
        // Status Messages
        public static string StatusConnecting => Localization.GetString("StatusConnecting");
        public static string StatusMonitoringRestarted => Localization.GetString("StatusMonitoringRestarted");
        public static string StatusCheckingForUpdates => Localization.GetString("StatusCheckingForUpdates");
        public static string StatusPollingData => Localization.GetString("StatusPollingData");
        
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
