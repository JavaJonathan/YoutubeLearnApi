namespace YoutubeLearnAPI.Models
{
    public class YoutubePlaylist
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public ICollection<PlaylistVideoMap> PlaylistVideoMaps { get; set; } = new List<PlaylistVideoMap>();
    }
}
