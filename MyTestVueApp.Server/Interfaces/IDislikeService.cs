using Microsoft.Data.SqlClient;
using MyTestVueApp.Server.Entities;

namespace MyTestVueApp.Server.Interfaces
{
    public interface IDislikeService
    {
        public Task<int> InsertDislike(int artId, Artist artist);
        public Task<int> RemoveDislike(int artId, Artist artist);
        public Task<bool> IsDisliked(int artId, Artist artist);
        public Task<IEnumerable<Dislike>> GetDislikesByArtwork(int artworkId);
        public Task<Dislike> GetDislikeByIds(int artId, int artistId);
    }
}
