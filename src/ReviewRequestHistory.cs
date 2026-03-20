namespace AgentSupervisor
{
    public class ReviewRequestHistory
    {
        private readonly HashSet<string> _seenRequestIds;
        private readonly object _lockObject = new object();

        public ReviewRequestHistory()
        {
            _seenRequestIds = Load();
        }

        private HashSet<string> Load()
        {
            var list = JsonFileStore.LoadList<string>(Constants.ReviewRequestHistoryFileName, "review request history");
            return new HashSet<string>(list);
        }

        private void Save()
        {
            lock (_lockObject)
            {
                JsonFileStore.SaveList(_seenRequestIds, Constants.ReviewRequestHistoryFileName, "review request history");
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
