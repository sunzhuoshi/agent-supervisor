using Microsoft.Win32;

namespace AgentSupervisor
{
    public class Configuration
    {
        public string PersonalAccessToken { get; set; } = string.Empty;
        public int PollingIntervalSeconds { get; set; } = 60;
        public int MaxHistoryEntries { get; set; } = 100;
        public string ProxyUrl { get; set; } = string.Empty;
        public bool UseProxy { get; set; } = false;
        public bool EnableDesktopNotifications { get; set; } = true;
        public bool PausePolling { get; set; } = false;

        private const string RegistryKeyPath = @"Software\AgentSupervisor";

        public static Configuration Load()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
                if (key != null)
                {
                    var config = new Configuration
                    {
                        PersonalAccessToken = key.GetValue("PersonalAccessToken") as string ?? string.Empty,
                        PollingIntervalSeconds = (int)(key.GetValue("PollingIntervalSeconds") ?? 60),
                        MaxHistoryEntries = (int)(key.GetValue("MaxHistoryEntries") ?? 100),
                        ProxyUrl = key.GetValue("ProxyUrl") as string ?? string.Empty,
                        UseProxy = ((int)(key.GetValue("UseProxy") ?? 0)) != 0,
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
                using var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath);
                if (key != null)
                {
                    key.SetValue("PersonalAccessToken", PersonalAccessToken);
                    key.SetValue("PollingIntervalSeconds", PollingIntervalSeconds, RegistryValueKind.DWord);
                    key.SetValue("MaxHistoryEntries", MaxHistoryEntries, RegistryValueKind.DWord);
                    key.SetValue("ProxyUrl", ProxyUrl);
                    key.SetValue("UseProxy", UseProxy ? 1 : 0, RegistryValueKind.DWord);
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
    }
}
