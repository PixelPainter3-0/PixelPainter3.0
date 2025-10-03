namespace MyTestVueApp.Server.Entities
{
    public class CommentLike
    {
        public int ArtistId { get; set; }
        public string Artist { get; set; }
        public int CommentId { get; set; }
        public string Artwork { get; set; }
        public bool Viewed { get; set; }
        public DateTime LikedOn { get; set; }
    }
}
