namespace AgentSupervisor.Models
{
    /// <summary>
    /// Represents GitHub release information
    /// </summary>
    public class ReleaseInfo
    {
        public string TagName { get; set; } = string.Empty;
        public string HtmlUrl { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
        public List<ReleaseAsset> Assets { get; set; } = new List<ReleaseAsset>();
    }

    /// <summary>
    /// Represents a GitHub release asset
    /// </summary>
    public class ReleaseAsset
    {
        public string Name { get; set; } = string.Empty;
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }
}
