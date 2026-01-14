namespace YoutubeLearnAPI.Models
{
    public class AddVideoModel
    {
        public string Title { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public string? Channel { get; set; }
    }

}
