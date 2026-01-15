namespace YoutubeLearnAPI.Models
{
    public class CreatePlaylistWithVideosModel
    {
        public string Title { get; set; } = string.Empty;
        public List<AddVideoModel> Videos { get; set; } = new List<AddVideoModel>();
    }
}
