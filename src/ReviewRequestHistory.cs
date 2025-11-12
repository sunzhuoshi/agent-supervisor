using System.Text.Json;

namespace AgentSupervisor
{
    public class ReviewRequestHistory
    {
        private const string HistoryFileName = "review_requests.json";
        private readonly HashSet<string> _seenRequestIds;
        private readonly object _lockObject = new object();

        public ReviewRequestHistory()
        {
            _seenRequestIds = Load();
        }

        private HashSet<string> Load()
        {
            if (File.Exists(HistoryFileName))
            {
                try
                {
                    var json = File.ReadAllText(HistoryFileName);
                    var list = JsonSerializer.Deserialize<List<string>>(json);
                    return list != null ? new HashSet<string>(list) : new HashSet<string>();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error loading review request history: {ex.Message}", ex);
                }
            }
            return new HashSet<string>();
        }

        private void Save()
        {
            lock (_lockObject)
            {
                try
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    var list = _seenRequestIds.ToList();
                    var json = JsonSerializer.Serialize(list, options);
                    File.WriteAllText(HistoryFileName, json);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error saving review request history: {ex.Message}", ex);
                }
            }
        }

        public bool HasBeenSeen(string requestId)
        {
            lock (_lockObject)
            {
                return _seenRequestIds.Contains(requestId);
            }
        }

        public void MarkAsSeen(string requestId)
        {
            lock (_lockObject)
            {
                if (_seenRequestIds.Add(requestId))
                {
                    Save();
                }
            }
        }

        public void MarkMultipleAsSeen(IEnumerable<string> requestIds)
        {
            lock (_lockObject)
            {
                bool changed = false;
                foreach (var id in requestIds)
                {
                    if (_seenRequestIds.Add(id))
                    {
                        changed = true;
                    }
                }
                
                if (changed)
                {
                    Save();
                }
            }
        }
    }
}
