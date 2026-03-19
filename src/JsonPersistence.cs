using System.Text.Json;

namespace AgentSupervisor
{
    /// <summary>
    /// Provides shared JSON file persistence helpers used by history classes.
    /// Thread-safety for <see cref="Save{T}"/> must be managed by callers; <see cref="Load{T}"/> is stateless and safe to call concurrently.
    /// </summary>
    internal static class JsonPersistence
    {
        private static readonly JsonSerializerOptions _writeOptions = new JsonSerializerOptions { WriteIndented = true };

        /// <summary>
        /// Loads a JSON file and deserializes it to <typeparamref name="T"/>.
        /// Returns a new instance of <typeparamref name="T"/> when the file is absent or on error.
        /// </summary>
        internal static T Load<T>(string filePath, string context) where T : new()
        {
            if (File.Exists(filePath))
            {
                try
                {
                    var json = File.ReadAllText(filePath);
                    return JsonSerializer.Deserialize<T>(json) ?? new T();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error loading {context}", ex);
                }
            }
            return new T();
        }

        /// <summary>
        /// Serializes <paramref name="data"/> to indented JSON and writes it to <paramref name="filePath"/>.
        /// The caller is responsible for acquiring any necessary lock before calling this method.
        /// </summary>
        internal static void Save<T>(string filePath, T data, string context)
        {
            try
            {
                var json = JsonSerializer.Serialize(data, _writeOptions);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error saving {context}", ex);
            }
        }
    }
}
