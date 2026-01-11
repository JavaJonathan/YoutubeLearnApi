namespace YoutubeLearnAPI.Models
{
    public class PlaylistVideoMap
    {
        public Guid Id { get; set; }
        public Guid PlaylistId { get; set; }
        public Guid VideoId { get; set; }
        public YoutubeVideo Video { get; set; } = null!;
        public YoutubePlaylist Playlist { get; set; } = null!;
    }
}
