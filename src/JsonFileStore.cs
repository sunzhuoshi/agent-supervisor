using System.Text.Json;

namespace AgentSupervisor
{
    internal static class JsonFileStore
    {
        public static T Load<T>(string filePath, T defaultValue)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    var json = File.ReadAllText(filePath);
                    return JsonSerializer.Deserialize<T>(json) ?? defaultValue;
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error loading {filePath}", ex);
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Serializes <paramref name="data"/> to JSON and writes it to <paramref name="filePath"/>.
        /// Callers are responsible for synchronization when concurrent access is possible.
        /// </summary>
        public static void Save<T>(string filePath, T data)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(data, options);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error saving {filePath}", ex);
            }
        }
    }
}
