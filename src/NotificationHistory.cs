using System.Text.Json;
using AgentSupervisor.Models;

namespace AgentSupervisor
{
    public class NotificationHistory
    {
        private readonly List<NotificationEntry> _entries;
        private readonly int _maxEntries;
        private readonly object _lockObject = new object();

        public NotificationHistory(int maxEntries = Constants.DefaultMaxHistoryEntries)
        {
            _maxEntries = maxEntries;
            _entries = Load();
        }

        private List<NotificationEntry> Load()
        {
            if (File.Exists(Constants.NotificationHistoryFileName))
            {
                try
                {
                    var json = File.ReadAllText(Constants.NotificationHistoryFileName);
                    return JsonSerializer.Deserialize<List<NotificationEntry>>(json) ?? new List<NotificationEntry>();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error loading notification history", ex);
                }
            }
            return new List<NotificationEntry>();
        }

        public void Save()
        {
            lock (_lockObject)
            {
                try
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    var json = JsonSerializer.Serialize(_entries, options);
                    File.WriteAllText(Constants.NotificationHistoryFileName, json);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error saving notification history", ex);
                }
            }
        }

        public bool HasBeenNotified(long reviewId)
        {
            lock (_lockObject)
            {
                return _entries.Any(e => e.Id == reviewId);
            }
        }

        public void Add(NotificationEntry entry)
        {
            lock (_lockObject)
            {
                _entries.Add(entry);
                
                // Keep only the most recent entries
                if (_entries.Count > _maxEntries)
                {
                    _entries.RemoveRange(0, _entries.Count - _maxEntries);
                }
                
                Save();
            }
        }

        public List<NotificationEntry> GetAll()
        {
            lock (_lockObject)
            {
                return new List<NotificationEntry>(_entries);
            }
        }

        public List<NotificationEntry> GetRecent(int count)
        {
            lock (_lockObject)
            {
                return _entries
                    .OrderByDescending(e => e.NotifiedAt)
                    .Take(count)
                    .ToList();
            }
        }
    }
}
