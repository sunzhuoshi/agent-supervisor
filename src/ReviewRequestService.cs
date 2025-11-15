using System.Text.Json;
using AgentSupervisor.Models;

namespace AgentSupervisor
{
    public class ReviewRequestService
    {
        private const string RequestsFileName = "review_request_details.json";
        private readonly List<ReviewRequestEntry> _requests;
        private readonly object _lockObject = new object();
        private readonly Action _onBadgeUpdateNeeded;

        public ReviewRequestService(Action? onBadgeUpdateNeeded = null)
        {
            _onBadgeUpdateNeeded = onBadgeUpdateNeeded;
            _requests = Load();
            _onBadgeUpdateNeeded.Invoke();
        }

        private List<ReviewRequestEntry> Load()
        {
            if (File.Exists(RequestsFileName))
            {
                try
                {
                    var json = File.ReadAllText(RequestsFileName);
                    return JsonSerializer.Deserialize<List<ReviewRequestEntry>>(json) ?? new List<ReviewRequestEntry>();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error loading review request details: {ex.Message}", ex);
                }
            }
            return new List<ReviewRequestEntry>();
        }

        private void Save()
        {
            lock (_lockObject)
            {
                try
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    var json = JsonSerializer.Serialize(_requests, options);
                    File.WriteAllText(RequestsFileName, json);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error saving review request details: {ex.Message}", ex);
                }
            }
        }

        public void AddOrUpdate(ReviewRequestEntry entry)
        {
            lock (_lockObject)
            {
                var existing = _requests.FirstOrDefault(r => r.Id == entry.Id);
                if (existing != null)
                {
                    // Update existing entry, preserve IsNew status
                    existing.Title = entry.Title;
                    existing.Author = entry.Author;
                    existing.HtmlUrl = entry.HtmlUrl;
                }
                else
                {
                    // New entry
                    entry.IsNew = true;
                    entry.AddedAt = DateTime.UtcNow;
                    _requests.Add(entry);
                    _onBadgeUpdateNeeded?.Invoke();
                }
                Save();
            }
        }

        public void MarkAsRead(string requestId)
        {
            lock (_lockObject)
            {
                var request = _requests.FirstOrDefault(r => r.Id == requestId);
                if (request != null && request.IsNew)
                {
                    request.IsNew = false;
                    Save();
                    _onBadgeUpdateNeeded?.Invoke();
                }
            }
        }

        public void MarkAllAsRead()
        {
            lock (_lockObject)
            {
                bool changed = false;
                foreach (var request in _requests)
                {
                    if (request.IsNew)
                    {
                        request.IsNew = false;
                        changed = true;
                    }
                }
                if (changed)
                {
                    Save();
                    _onBadgeUpdateNeeded?.Invoke();
                }
            }
        }

        public List<ReviewRequestEntry> GetAll()
        {
            lock (_lockObject)
            {
                return new List<ReviewRequestEntry>(_requests.OrderByDescending(r => r.AddedAt));
            }
        }

        public int GetNewCount()
        {
            lock (_lockObject)
            {
                return _requests.Count(r => r.IsNew);
            }
        }

        public int GetTotalCount()
        {
            lock (_lockObject)
            {
                return _requests.Count;
            }
        }

        public void RemoveStaleRequests(List<string> currentRequestIds)
        {
            lock (_lockObject)
            {
                var initialCount = _requests.Count;
                _requests.RemoveAll(r => !currentRequestIds.Contains(r.Id));
                if (_requests.Count != initialCount)
                {
                    Save();
                    _onBadgeUpdateNeeded?.Invoke();
                }
            }
        }
    }
}
