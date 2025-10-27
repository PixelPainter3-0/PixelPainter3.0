using MyTestVueApp.Server.Models;
using System.Reflection.Metadata.Ecma335;

namespace MyTestVueApp.Server.Entities
{
    public class Point
    {
        //Required
        public int Id { get; set; }
        public int ArtspaceId { get; set; }
        public string Title { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }
}
