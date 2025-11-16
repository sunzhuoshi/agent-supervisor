namespace AgentSupervisor
{
    /// <summary>
    /// Observer interface for receiving notifications about review request changes.
    /// Implements the Observer pattern for model/view separation.
    /// </summary>
    public interface IReviewRequestObserver
    {
        /// <summary>
        /// Called when review requests are added, updated, or removed.
        /// </summary>
        void OnReviewRequestsChanged();
    }
}
