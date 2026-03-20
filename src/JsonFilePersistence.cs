using System.Text.Json;

namespace AgentSupervisor
{
    /// <summary>
    /// Generic utility for JSON file-based persistence with thread-safe Load and Save operations.
    /// </summary>
    public class JsonFilePersistence<T>
    {
        private readonly string _filePath;
        private readonly object _lockObject = new object();

        public JsonFilePersistence(string filePath)
        {
            _filePath = filePath;
        }

        public T Load(T defaultValue)
        {
            lock (_lockObject)
            {
                if (File.Exists(_filePath))
                {
                    try
                    {
                        var json = File.ReadAllText(_filePath);
                        return JsonSerializer.Deserialize<T>(json) ?? defaultValue;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Error loading {_filePath}", ex);
                    }
                }
                return defaultValue;
            }
        }

        public void Save(T data)
        {
            lock (_lockObject)
            {
                try
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    var json = JsonSerializer.Serialize(data, options);
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
