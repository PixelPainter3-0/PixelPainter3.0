using Microsoft.Data.SqlClient;
using MyTestVueApp.Server.Entities;

namespace MyTestVueApp.Server.Interfaces
{
    public interface ICommentDislikeService
    {
        public Task<int> InsertCommentDislike(int artId, Artist artist, int commentId);
        public Task<int> RemoveCommentDislike(int artId, Artist artist, int commentId);
        public Task<bool> IsCommentDisliked(int artId, Artist artist, int commentId);
        public Task<IEnumerable<CommentDislike>> GetCommentDislikesByArtwork(int artworkId);
        public Task<CommentDislike> GetCommentDislikeByIds(int artId, int artistId, int commentId);
    }
}
