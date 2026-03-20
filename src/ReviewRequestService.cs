using AgentSupervisor.Models;

namespace AgentSupervisor
{
    public class ReviewRequestService
    {
        private readonly List<ReviewRequestEntry> _requests;
        private readonly object _lockObject = new object();
        private readonly List<IReviewRequestObserver> _observers = new List<IReviewRequestObserver>();
        private readonly JsonFileStore<List<ReviewRequestEntry>> _store =
            new JsonFileStore<List<ReviewRequestEntry>>(Constants.ReviewRequestDetailsFileName);

        public ReviewRequestService()
        {
            _requests = _store.Load(() => new List<ReviewRequestEntry>());
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

        private void Save()
        {
            // Always called from within _lockObject; JsonFileStore uses its own lock for file I/O.
            _store.Save(_requests);
        }

        /// <summary>
        /// Runs <paramref name="action"/> inside a lock and calls <see cref="NotifyObservers"/>
        /// if the action returns <c>true</c>.
        /// </summary>
        private void ExecuteAndNotify(Func<bool> action)
        {
            bool notifyNeeded;
            lock (_lockObject)
            {
                notifyNeeded = action();
            }
            if (notifyNeeded)
            {
                NotifyObservers();
            }
        }

        public void AddOrUpdate(ReviewRequestEntry entry)
        {
            ExecuteAndNotify(() =>
            {
                bool notifyNeeded = false;
                bool saveNeeded = false;
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
                        existing.IsNew = true;
                        notifyNeeded = true;
                        saveNeeded = true;
                    }
                }
                else
                {
                    // New entry
                    entry.IsNew = true;
                    entry.AddedAt = DateTime.UtcNow;
                    _requests.Add(entry);
                    notifyNeeded = true;
                    saveNeeded = true;
                }

                if (saveNeeded)
                {
                    Save();
                }
                return notifyNeeded;
            });
        }

        public void MarkAsRead(string requestId)
        {
            ExecuteAndNotify(() =>
            {
                var request = _requests.FirstOrDefault(r => r.Id == requestId);
                if (request != null && request.IsNew)
                {
                    request.IsNew = false;
                    Save();
                    return true;
                }
                return false;
            });
        }

        public void MarkAllAsRead()
        {
            ExecuteAndNotify(() =>
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
                }
                return changed;
            });
        }

        public List<ReviewRequestEntry> GetAll()
        {
            lock (_lockObject)
            {
                return _requests.OrderByDescending(r => r.AddedAt).Select(r => r.Clone()).ToList();
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
            ExecuteAndNotify(() =>
            {
                _requests.Clear();
                _requests.AddRange(_store.Load(() => new List<ReviewRequestEntry>()));
                return true;
            });
        }

        public void RemoveStaleRequests(List<string> currentRequestIds)
        {
            ExecuteAndNotify(() =>
            {
                var initialCount = _requests.Count;
                _requests.RemoveAll(r => !currentRequestIds.Contains(r.Id));
                if (_requests.Count != initialCount)
                {
                    Save();
                    return true;
                }
                return false;
            });
        }
    }
}
