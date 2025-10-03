using Microsoft.Data.SqlClient;
using MyTestVueApp.Server.Entities;

namespace MyTestVueApp.Server.Interfaces
{
    public interface ICommentLikeService
    {
        public Task<int> InsertCommentLike(int artId, Artist artist, int commentId);
        public Task<int> RemoveCommentLike(int artId, Artist artist, int commentId);
        public Task<bool> IsCommentLiked(int artId, Artist artist, int commentId);
        public Task<IEnumerable<CommentLike>> GetCommentLikesByArtwork(int artworkId);
        public Task<CommentLike> GetCommentLikeByIds(int artId, int artistId, int commentId);
    }
}
