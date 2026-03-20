using System.Collections.Concurrent;
using System.Text.Json;

namespace AgentSupervisor
{
    internal static class JsonFileStore
    {
        private static readonly ConcurrentDictionary<string, object> FileLocks = new();
        private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

        private static object GetFileLock(string filePath)
        {
            var fullPath = Path.GetFullPath(filePath);
            return FileLocks.GetOrAdd(fullPath, _ => new object());
        }

        public static T Load<T>(string filePath, T defaultValue)
        {
            var fileLock = GetFileLock(filePath);
            lock (fileLock)
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
            }
            return defaultValue;
        }

        /// <summary>
        /// Serializes <paramref name="data"/> to JSON and writes it to <paramref name="filePath"/>.
        /// Access is synchronized with <see cref="Load{T}(string, T)"/> on a per-file basis.
        /// </summary>
        public static void Save<T>(string filePath, T data)
        {
            var fileLock = GetFileLock(filePath);
            lock (fileLock)
            {
                try
                {
                    var json = JsonSerializer.Serialize(data, WriteOptions);
                    File.WriteAllText(filePath, json);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error saving {filePath}", ex);
                }
            }
        }
    }
}
