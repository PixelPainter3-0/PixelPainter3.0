using Microsoft.Data.SqlClient;
using MyTestVueApp.Server.Entities;

namespace MyTestVueApp.Server.Interfaces
{
    public interface ICommentLikeService
    {
        public Task<int> InsertCommentLike(Artist artist, int commentId);
        public Task<int> RemoveCommentLike(Artist artist, int commentId);
        public Task<bool> IsCommentLiked(Artist artist, int commentId);

        public Task<IEnumerable<CommentLike>> GetLikesByComment(int commentId);
    }
}
