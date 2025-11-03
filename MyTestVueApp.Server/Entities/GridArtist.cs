namespace MyTestVueApp.Server.Entities
{
    public class GridArtist
    {
        public int ArtistId { get; set; }
        public List<DateTime> Additions { get; set; }

        public GridArtist(Artist artist)
        {
            ArtistId = artist.Id;
            Additions = new List<DateTime>();
        }
    }
}
