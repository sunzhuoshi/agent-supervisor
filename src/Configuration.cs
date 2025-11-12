using System.Text.Json;

namespace GitHubCopilotAgentBot
{
    public class Configuration
    {
        public string PersonalAccessToken { get; set; } = string.Empty;
        public int PollingIntervalSeconds { get; set; } = 60;
        public int MaxHistoryEntries { get; set; } = 100;
        public string ProxyUrl { get; set; } = string.Empty;
        public bool UseProxy { get; set; } = false;
        public string CustomIconPath { get; set; } = string.Empty;

        private const string ConfigFileName = "config.json";

        public static Configuration Load()
        {
            if (File.Exists(ConfigFileName))
            {
                try
                {
                    var json = File.ReadAllText(ConfigFileName);
                    var config = JsonSerializer.Deserialize<Configuration>(json);
                    Logger.LogInfo("Configuration loaded successfully");
                    return config ?? new Configuration();
                }
                catch (Exception ex)
                {
                    Logger.LogError("Failed to load configuration", ex);
                    // Return default configuration on error
                }
            }

            return new Configuration();
        }

        public void Save()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(ConfigFileName, json);
                Logger.LogInfo("Configuration saved successfully");
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to save configuration", ex);
                // Silent fail - error will be shown in UI if needed
            }
        }
    }
}
