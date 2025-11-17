using Microsoft.Win32;

namespace AgentSupervisor
{
    public class Configuration
    {
        public string PersonalAccessToken { get; set; } = string.Empty;
        public int PollingIntervalSeconds { get; set; } = Constants.DefaultPollingIntervalSeconds;
        public int MaxHistoryEntries { get; set; } = Constants.DefaultMaxHistoryEntries;
        public string ProxyUrl { get; set; } = string.Empty;
        public bool UseProxy { get; set; } = false;
        public DateTime? LastUpdateCheck { get; set; } = null;
        public bool CheckForUpdatesOnStartup { get; set; } = true;
        public string SkippedVersion { get; set; } = string.Empty;
        public bool EnableDesktopNotifications { get; set; } = true;
        public bool PausePolling { get; set; } = false;

        public static Configuration Load()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(Constants.RegistryKeyPath);
                if (key != null)
                {
                    var config = new Configuration
                    {
                        PersonalAccessToken = key.GetValue("PersonalAccessToken") as string ?? string.Empty,
                        PollingIntervalSeconds = (int)(key.GetValue("PollingIntervalSeconds") ?? Constants.DefaultPollingIntervalSeconds),
                        MaxHistoryEntries = (int)(key.GetValue("MaxHistoryEntries") ?? Constants.DefaultMaxHistoryEntries),
                        ProxyUrl = key.GetValue("ProxyUrl") as string ?? string.Empty,
                        UseProxy = ((int)(key.GetValue("UseProxy") ?? 0)) != 0,
                        SkippedVersion = key.GetValue("SkippedVersion") as string ?? string.Empty,
                        EnableDesktopNotifications = ((int)(key.GetValue("EnableDesktopNotifications") ?? 1)) != 0,
                        PausePolling = ((int)(key.GetValue("PausePolling") ?? 0)) != 0
                    };
                    Logger.LogInfo("Configuration loaded successfully from Registry");
                    return config;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to load configuration from Registry", ex);
            }

            return new Configuration();
        }

        public void Save()
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(Constants.RegistryKeyPath);
                if (key != null)
                {
                    key.SetValue("PersonalAccessToken", PersonalAccessToken);
                    key.SetValue("PollingIntervalSeconds", PollingIntervalSeconds, RegistryValueKind.DWord);
                    key.SetValue("MaxHistoryEntries", MaxHistoryEntries, RegistryValueKind.DWord);
                    key.SetValue("ProxyUrl", ProxyUrl);
                    key.SetValue("UseProxy", UseProxy ? 1 : 0, RegistryValueKind.DWord);
                    key.SetValue("SkippedVersion", SkippedVersion);
                    key.SetValue("EnableDesktopNotifications", EnableDesktopNotifications ? 1 : 0, RegistryValueKind.DWord);
                    key.SetValue("PausePolling", PausePolling ? 1 : 0, RegistryValueKind.DWord);
                    Logger.LogInfo("Configuration saved successfully to Registry");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to save configuration to Registry", ex);
                // Silent fail - error will be shown in UI if needed
            }
        }

        /// <summary>
        /// Loads the language preference from Registry
        /// </summary>
        public static string LoadLanguage()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(Constants.RegistryKeyPath);
                if (key != null)
                {
                    return key.GetValue("Language") as string ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to load language from Registry", ex);
            }
            return string.Empty;
        }

        /// <summary>
        /// Saves the language preference to Registry
        /// </summary>
        public static void SaveLanguage(string language)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(Constants.RegistryKeyPath);
                if (key != null)
                {
                    key.SetValue("Language", language);
                    Logger.LogInfo($"Language preference saved: {language}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to save language to Registry", ex);
            }
        }
    }
}
