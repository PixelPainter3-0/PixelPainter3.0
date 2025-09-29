using MyTestVueApp.Server.Entities;

namespace MyTestVueApp.Server.Models
{
    public class ArtModel
    {
        public Art Art { get; set; }
        public List<Tag> Tags { get; set; }
    }
}
