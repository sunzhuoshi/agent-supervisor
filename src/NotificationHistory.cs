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
            return JsonPersistence.Load<List<NotificationEntry>>(Constants.NotificationHistoryFileName, "notification history");
        }

        // Must be called while holding _lockObject.
        private void SaveUnlocked()
        {
            JsonPersistence.Save(Constants.NotificationHistoryFileName, _entries, "notification history");
        }

        public void Save()
        {
            lock (_lockObject)
            {
                SaveUnlocked();
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
                
                SaveUnlocked();
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
