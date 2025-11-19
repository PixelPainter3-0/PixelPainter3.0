using Microsoft.Data.SqlClient;
using MyTestVueApp.Server.Entities;

namespace MyTestVueApp.Server.Interfaces
{
    public interface ICommentDislikeService
    {
        public Task<int> InsertCommentDislike(Artist artist, int commentId);
        public Task<int> RemoveCommentDislike(Artist artist, int commentId);
        public Task<bool> IsCommentDisliked(Artist artist, int commentId);
        public Task<IEnumerable<CommentDislike>> GetDislikesByComment(int commentId);

    }
}
