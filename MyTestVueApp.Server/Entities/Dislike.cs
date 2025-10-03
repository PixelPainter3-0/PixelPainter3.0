namespace MyTestVueApp.Server.Entities
{
    public class Dislike
    {
        public int ArtistId { get; set; }
        public string Artist { get; set; }
        public int ArtId { get; set; }
        public string Artwork { get; set; }
        public bool Viewed { get; set; }
        public DateTime DislikedOn { get; set; }
    }
}
