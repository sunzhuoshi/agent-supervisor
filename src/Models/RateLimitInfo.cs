namespace AgentSupervisor.Models
{
    /// <summary>
    /// Represents GitHub API rate limit information from response headers
    /// </summary>
    public class RateLimitInfo
    {
        /// <summary>
        /// Maximum number of requests per hour
        /// </summary>
        public int Limit { get; set; }

        /// <summary>
        /// Number of requests remaining in the current rate limit window
        /// </summary>
        public int Remaining { get; set; }

        /// <summary>
        /// Unix timestamp when the rate limit window resets
        /// </summary>
        public long ResetTimestamp { get; set; }

        /// <summary>
        /// DateTime when the rate limit window resets (UTC)
        /// </summary>
        public DateTime ResetTime => DateTimeOffset.FromUnixTimeSeconds(ResetTimestamp).UtcDateTime;

        /// <summary>
        /// Time remaining until the rate limit resets
        /// </summary>
        public TimeSpan TimeUntilReset => ResetTime - DateTime.UtcNow;

        /// <summary>
        /// Calculates the optimal polling interval in seconds based on remaining requests and time until reset
        /// </summary>
        /// <param name="minIntervalSeconds">Minimum interval to enforce (default: 10 seconds)</param>
        /// <param name="maxIntervalSeconds">Maximum interval to enforce (default: 3600 seconds)</param>
        /// <param name="safetyMargin">Safety margin factor (default: 1.2 for 20% buffer)</param>
        /// <returns>Recommended polling interval in seconds</returns>
        public int CalculateOptimalInterval(int minIntervalSeconds = 10, int maxIntervalSeconds = 3600, double safetyMargin = 1.2)
        {
            // If we have no remaining requests, wait until reset
            if (Remaining <= 0)
            {
                return Math.Min((int)Math.Ceiling(TimeUntilReset.TotalSeconds), maxIntervalSeconds);
            }

            // Calculate the time window in seconds
            var timeWindowSeconds = TimeUntilReset.TotalSeconds;
            
            // If the reset time has passed or is very soon, use minimum interval
            if (timeWindowSeconds <= 0)
            {
                return minIntervalSeconds;
            }

            // Calculate optimal interval: distribute remaining requests evenly over time window
            // Add safety margin to avoid hitting the limit
            var optimalInterval = (timeWindowSeconds / Remaining) * safetyMargin;

            // Clamp to min/max bounds
            var clampedInterval = Math.Max(minIntervalSeconds, Math.Min(optimalInterval, maxIntervalSeconds));

            return (int)Math.Ceiling(clampedInterval);
        }

        /// <summary>
        /// Determines if we're getting close to the rate limit
        /// </summary>
        /// <param name="threshold">Percentage threshold (default: 0.1 for 10%)</param>
        /// <returns>True if remaining requests are below the threshold</returns>
        public bool IsNearLimit(double threshold = 0.1)
        {
            if (Limit <= 0) return false;
            return (double)Remaining / Limit < threshold;
        }
    }
}
