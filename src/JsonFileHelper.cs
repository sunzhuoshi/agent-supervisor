using System.Text.Json;

namespace AgentSupervisor
{
    /// <summary>
    /// Provides generic helpers for loading and saving JSON-serialized data to files.
    /// </summary>
    internal static class JsonFileHelper
    {
        /// <summary>
        /// Loads and deserializes a JSON file, returning <paramref name="defaultValue"/> on missing file or error.
        /// </summary>
        public static T Load<T>(string fileName, string errorContext, T defaultValue)
        {
            if (File.Exists(fileName))
            {
                try
                {
                    var json = File.ReadAllText(fileName);
                    return JsonSerializer.Deserialize<T>(json) ?? defaultValue;
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error loading {errorContext} from {fileName}", ex);
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Serializes <paramref name="data"/> as indented JSON and writes it to <paramref name="fileName"/>.
        /// </summary>
        public static void Save<T>(string fileName, T data, string errorContext)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(data, options);
                File.WriteAllText(fileName, json);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error saving {errorContext} to {fileName}", ex);
            }
        }
    }
}
