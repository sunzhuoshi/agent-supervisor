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
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading configuration: {ex.Message}");
                    Console.WriteLine("Using default configuration.");
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving configuration: {ex.Message}");
            }
        }

        public static Configuration CreateDefault()
        {
            var config = new Configuration();
            Console.WriteLine("\nFirst-time setup:");
            Console.Write("Enter your GitHub Personal Access Token: ");
            config.PersonalAccessToken = Console.ReadLine()?.Trim() ?? string.Empty;

            Console.Write("Enter polling interval in seconds (default: 60): ");
            var intervalInput = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(intervalInput) && int.TryParse(intervalInput, out int interval))
            {
                config.PollingIntervalSeconds = interval;
            }

            config.Save();
            Console.WriteLine("\nConfiguration saved to config.json");
            return config;
        }
    }
}
