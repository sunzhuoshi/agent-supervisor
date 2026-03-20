namespace AgentSupervisor
{
    public class ReviewRequestHistory
    {
        private readonly HashSet<string> _seenRequestIds;
        private readonly object _lockObject = new object();
        private readonly JsonFileStore<List<string>> _store =
            new JsonFileStore<List<string>>(Constants.ReviewRequestHistoryFileName);

        public ReviewRequestHistory()
        {
            var list = _store.Load(() => new List<string>());
            _seenRequestIds = new HashSet<string>(list);
        }

        private void Save()
        {
            // Always called from within _lockObject; JsonFileStore uses its own lock for file I/O.
            _store.Save(_seenRequestIds.ToList());
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
