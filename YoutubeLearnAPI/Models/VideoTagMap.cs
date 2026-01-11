namespace YoutubeLearnAPI.Models
{
    public class VideoTagMap
    {
        public Guid Id { get; set; }
        public Guid VideoId { get; set; }
        public Guid TagId { get; set; }
        public YoutubeVideo Video { get; set; } = null!;
        public VideoTag Tag { get; set; } = null!;
    }
}
