namespace AgentSupervisor
{
    public class ReviewRequestHistory
    {
        private readonly HashSet<string> _seenRequestIds;
        private readonly object _lockObject = new object();
        private readonly JsonFilePersistence<List<string>> _persistence;

        public ReviewRequestHistory()
        {
            _persistence = new JsonFilePersistence<List<string>>(Constants.ReviewRequestHistoryFileName);
            _seenRequestIds = new HashSet<string>(_persistence.Load(new List<string>()));
        }

        private void Save()
        {
            lock (_lockObject)
            {
                _persistence.Save(_seenRequestIds.ToList());
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
