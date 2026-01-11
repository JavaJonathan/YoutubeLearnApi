namespace YoutubeLearnAPI.Models
{
    public class YoutubeVideo
    {
        public Guid Id { get; set; }
        public string Link { get; set; } = null!;
        public string Channel { get; set; } = null!;
        public string Title { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string? CoreInsights { get; set; }

        public ICollection<PlaylistVideoMap> PlaylistVideoMaps { get; set; } = new List<PlaylistVideoMap>();
        public ICollection<VideoTagMap> VideoTagMaps { get; set; } = new List<VideoTagMap>();
    }
}
