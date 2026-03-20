using System.Text.Json;

namespace AgentSupervisor
{
    /// <summary>
    /// Generic thread-safe JSON file persistence helper.
    /// Provides Load and Save operations with shared try/catch and locking logic.
    /// </summary>
    public class JsonFileStore<T>
    {
        private readonly string _filePath;
        private readonly object _lockObject = new object();
        private static readonly JsonSerializerOptions SerializerOptions =
            new JsonSerializerOptions { WriteIndented = true };

        public JsonFileStore(string filePath)
        {
            _filePath = filePath;
        }

        /// <summary>
        /// Loads data from the JSON file. Returns the result of <paramref name="defaultFactory"/>
        /// if the file does not exist or cannot be read.
        /// </summary>
        public T Load(Func<T> defaultFactory)
        {
            lock (_lockObject)
            {
                if (File.Exists(_filePath))
                {
                    try
                    {
                        var json = File.ReadAllText(_filePath);
                        return JsonSerializer.Deserialize<T>(json, SerializerOptions) ?? defaultFactory();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Error loading {_filePath}", ex);
                    }
                }
                return defaultFactory();
            }
        }

        /// <summary>
        /// Saves <paramref name="data"/> to the JSON file in a thread-safe manner.
        /// </summary>
        public void Save(T data)
        {
            lock (_lockObject)
            {
                try
                {
                    var json = JsonSerializer.Serialize(data, SerializerOptions);
                    File.WriteAllText(_filePath, json);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error saving {_filePath}", ex);
                }
            }
        }
    }
}
