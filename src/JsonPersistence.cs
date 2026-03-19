using System.Text.Json;

namespace AgentSupervisor
{
    /// <summary>
    /// Provides shared JSON file persistence helpers used by history classes.
    /// Thread-safety must be managed by callers: <see cref="Save{T}"/> writes atomically (via a sibling
    /// <c>.tmp</c> file), so concurrent readers will observe either the previous or the new file — never
    /// a partially-written one. Callers should still coordinate reads and writes under a shared lock to
    /// avoid logical races (e.g., reading stale data immediately after a write).
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
        /// Serializes <paramref name="data"/> to indented JSON and atomically writes it to <paramref name="filePath"/>
        /// (via a sibling <c>.tmp</c> file that is moved over the target on success).
        /// The caller is responsible for acquiring any necessary lock before calling this method.
        /// </summary>
        internal static void Save<T>(string filePath, T data, string context)
        {
            var tempPath = filePath + ".tmp";
            try
            {
                var json = JsonSerializer.Serialize(data, _writeOptions);
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, filePath, overwrite: true);
                tempPath = null; // moved successfully; nothing to clean up
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error saving {context}", ex);
            }
            finally
            {
                if (tempPath != null)
                {
                    try { File.Delete(tempPath); } catch { /* best-effort cleanup */ }
                }
            }
        }
    }
}
