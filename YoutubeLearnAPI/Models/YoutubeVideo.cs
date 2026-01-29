namespace YoutubeLearnAPI.Models
{
    public class YoutubeVideo
    {
        public Guid Id { get; set; }
        public string Link { get; set; } = String.Empty;
        public string Channel { get; set; } = String.Empty;
        public string Title { get; set; } = String.Empty;
        public DateTime CreatedAt { get; set; }
        public string? CoreInsights { get; set; }
        public int? Impact { get; set; }
        public ICollection<PlaylistVideoMap> PlaylistVideoMaps { get; set; } = new List<PlaylistVideoMap>();
        public ICollection<VideoTagMap> VideoTagMaps { get; set; } = new List<VideoTagMap>();
    }
}
