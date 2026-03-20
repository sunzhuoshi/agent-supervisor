using System.Text.Json;

namespace AgentSupervisor
{
    /// <summary>
    /// Provides generic JSON file persistence helpers shared across history and service classes.
    /// </summary>
    internal static class JsonFileStore
    {
        private static readonly JsonSerializerOptions WriteOptions = new JsonSerializerOptions { WriteIndented = true };

        /// <summary>
        /// Loads a list of items from a JSON file.
        /// Returns an empty list if the file does not exist or an error occurs.
        /// </summary>
        internal static List<T> LoadList<T>(string filePath, string errorContext)
        {
            if (!File.Exists(filePath))
                return new List<T>();

            try
            {
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error loading {errorContext}", ex);
                return new List<T>();
            }
        }

        /// <summary>
        /// Saves a collection of items to a JSON file.
        /// The caller is responsible for any required locking before calling this method.
        /// </summary>
        internal static void SaveList<T>(IEnumerable<T> data, string filePath, string errorContext)
        {
            try
            {
                var json = JsonSerializer.Serialize(data, WriteOptions);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error saving {errorContext}", ex);
            }
        }
    }
}
