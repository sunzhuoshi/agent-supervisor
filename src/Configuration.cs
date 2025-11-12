using System.Text.Json;

namespace GitHubCopilotAgentBot
{
    public class Configuration
    {
        public string PersonalAccessToken { get; set; } = string.Empty;
        public int PollingIntervalSeconds { get; set; } = 60;
        public int MaxHistoryEntries { get; set; } = 100;

        private const string ConfigFileName = "config.json";

        public static Configuration Load()
        {
            if (File.Exists(ConfigFileName))
            {
                try
                {
                    var json = File.ReadAllText(ConfigFileName);
                    var config = JsonSerializer.Deserialize<Configuration>(json);
                    return config ?? new Configuration();
                }
                catch (Exception)
                {
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
            }
            catch (Exception)
            {
                // Silent fail - error will be shown in UI if needed
            }
        }
    }
}
