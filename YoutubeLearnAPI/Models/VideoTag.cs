namespace YoutubeLearnAPI.Models
{
    public class VideoTag
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public ICollection<VideoTagMap> VideoTagMaps { get; set; } = new List<VideoTagMap>();
    }
}
