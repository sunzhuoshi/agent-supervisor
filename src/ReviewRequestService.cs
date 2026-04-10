using AgentSupervisor.Models;

namespace AgentSupervisor
{
    public class ReviewRequestService
    {
        private readonly List<ReviewRequestEntry> _requests;
        private readonly object _lockObject = new object();
        private readonly List<IReviewRequestObserver> _observers = new List<IReviewRequestObserver>();

        public ReviewRequestService()
        {
            _requests = Load();
        }

        /// <summary>
        /// Subscribe an observer to receive notifications about review request changes.
        /// </summary>
        public void Subscribe(IReviewRequestObserver observer)
        {
            lock (_lockObject)
            {
                if (!_observers.Contains(observer))
                {
                    _observers.Add(observer);
                    Logger.LogInfo($"Observer subscribed: {observer.GetType().Name}");
                }
            }
        }

        /// <summary>
        /// Unsubscribe an observer from receiving notifications.
        /// </summary>
        public void Unsubscribe(IReviewRequestObserver observer)
        {
            lock (_lockObject)
            {
                _observers.Remove(observer);
                Logger.LogInfo($"Observer unsubscribed: {observer.GetType().Name}");
            }
        }

        /// <summary>
        /// Notify all observers that the review requests have changed.
        /// </summary>
        private void NotifyObservers()
        {
            // Create a copy of observers to avoid issues with modifications during iteration
            List<IReviewRequestObserver> observersCopy;
            lock (_lockObject)
            {
                observersCopy = new List<IReviewRequestObserver>(_observers);
            }

            foreach (var observer in observersCopy)
            {
                try
                {
                    observer.OnReviewRequestsChanged();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error notifying observer {observer.GetType().Name}", ex);
                }
            }
        }

        private List<ReviewRequestEntry> Load()
        {
            return JsonFileStore.LoadList<ReviewRequestEntry>(Constants.ReviewRequestDetailsFileName, "review request details");
        }

        private void Save()
        {
            lock (_lockObject)
            {
                JsonFileStore.SaveList(_requests, Constants.ReviewRequestDetailsFileName, "review request details");
            }
        }

        public void AddOrUpdate(ReviewRequestEntry entry)
        {
            bool saveNeeded = false;
            lock (_lockObject)
            {
                var existing = _requests.FirstOrDefault(r => r.Id == entry.Id);
                if (existing != null)
                {
                    // Update existing entry
                    bool hasChanges = existing.Title != entry.Title || 
                                     existing.Author != entry.Author || 
                                     existing.HtmlUrl != entry.HtmlUrl;
                    
                    if (hasChanges)
                    {
                        existing.Title = entry.Title;
                        existing.Author = entry.Author;
                        existing.HtmlUrl = entry.HtmlUrl;
                        saveNeeded = true;
                    }
                    
                    // Check if the entry has been updated (newer updated_at timestamp)
                    if (entry.UpdatedAt > existing.UpdatedAt)
                    {
                        existing.UpdatedAt = entry.UpdatedAt;
                        saveNeeded = true;
                    }
                    
                    // Check if commit count has changed (increase or decrease due to new commits,
                    // force-pushes or rebases are all meaningful changes worth notifying about)
                    if (entry.CommitCount.HasValue && existing.CommitCount != entry.CommitCount)
                    {
                        existing.IsNew = true;
                        existing.CommitCount = entry.CommitCount;
                        saveNeeded = true;
                    }
                }
                else
                {
                    // New entry
                    entry.IsNew = true;
                    entry.AddedAt = DateTime.UtcNow;
                    _requests.Add(entry);
                    saveNeeded = true;
                }
                
                if (saveNeeded)
                {
                    Save();
                }
            }
            
            if (saveNeeded)
            {
                NotifyObservers();
            }
        }

        public void MarkAsRead(string requestId)
        {
            bool notifyNeeded = false;
            lock (_lockObject)
            {
                var request = _requests.FirstOrDefault(r => r.Id == requestId);
                if (request != null && request.IsNew)
                {
                    request.IsNew = false;
                    Save();
                    notifyNeeded = true;
                }
            }
            
            if (notifyNeeded)
            {
                NotifyObservers();
            }
        }

        public void MarkAllAsRead()
        {
            bool notifyNeeded = false;
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
                    notifyNeeded = true;
                }
            }
            
            if (notifyNeeded)
            {
                NotifyObservers();
            }
        }

        public List<ReviewRequestEntry> GetAll()
        {
            lock (_lockObject)
            {
                return _requests.OrderByDescending(r => r.AddedAt).Select(r => r.Clone()).ToList();
            }
        }

        public DateTime? GetUpdatedAt(string requestId)
        {
            lock (_lockObject)
            {
                return _requests.FirstOrDefault(r => r.Id == requestId)?.UpdatedAt;
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

        /// <summary>
        /// Reload the review requests from persistent storage.
        /// Used when restarting monitoring to reset in-memory state.
        /// </summary>
        public void Reload()
        {
            lock (_lockObject)
            {
                _requests.Clear();
                _requests.AddRange(Load());
            }
            NotifyObservers();
        }

        public void RemoveStaleRequests(List<string> currentRequestIds)
        {
            bool notifyNeeded = false;
            lock (_lockObject)
            {
                var initialCount = _requests.Count;
                _requests.RemoveAll(r => !currentRequestIds.Contains(r.Id));
                if (_requests.Count != initialCount)
                {
                    Save();
                    notifyNeeded = true;
                }
            }
            
            if (notifyNeeded)
            {
                NotifyObservers();
            }
        }
    }
}
