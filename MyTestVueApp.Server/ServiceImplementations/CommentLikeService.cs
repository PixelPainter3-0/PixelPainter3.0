using Microsoft.Extensions.Options;
using MyTestVueApp.Server.Configuration;
using MyTestVueApp.Server.Entities;
using MyTestVueApp.Server.Interfaces;
using Microsoft.Data.SqlClient;
using System.Reflection.Metadata.Ecma335;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace MyTestVueApp.Server.ServiceImplementations
{
    public class CommentLikeService : ICommentLikeService
    {
        private readonly IOptions<ApplicationConfiguration> AppConfig;
        private readonly ILogger<CommentLikeService> Logger;
        public CommentLikeService(IOptions<ApplicationConfiguration> appConfig, ILogger<CommentLikeService> logger)
        {
            AppConfig = appConfig;
            Logger = logger;
        }
        /// <summary>
        /// Insert's into the database what artwork an artist has liked
        /// </summary>
        /// <param name="artist">Id of the artist who liked the artwork</param>
        /// <returns>0 if invalid input, -1 if the input failed, and 1+ if it succeeded</returns>
        public async Task<int> InsertCommentLike(Artist artist, int commentId)
        {
            var connectionString = AppConfig.Value.ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                //Check to make sure the user hasnt already liked this work of art
                var checkDupQuery = "SELECT Count(*) FROM CommentLikes WHERE ArtistID = @ArtistId AND CommentID = @CommentId";
                using (SqlCommand checkDupCommand = new SqlCommand(checkDupQuery, connection))
                {
                    checkDupCommand.Parameters.AddWithValue("@ArtistId", artist.Id);
                    checkDupCommand.Parameters.AddWithValue("@CommentId", commentId);

                    int count = (int)await checkDupCommand.ExecuteScalarAsync();
                    if (count > 0)
                    {
                        Console.WriteLine("This user has already liked this comment!");
                        return 0;
                    }
                }

                var query = "INSERT INTO CommentLikes (ArtistID, CommentID, Viewed) VALUES (@ArtistId, @CommentId, 0)";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ArtistId", artist.Id);
                    command.Parameters.AddWithValue("@CommentId", commentId);

                    int rowsChanged = await command.ExecuteNonQueryAsync();
                    if (rowsChanged > 0)
                    {
                        return rowsChanged;
                    }
                    else
                    {
                        return -1;
                    }
                }
            }
        }
        /// <summary>
        /// Removes the like relation from the database
        /// </summary>
        /// <param name="artist">Artist who is unliking the artwork</param>
        /// <returns>0 if bad input, -1 if it fails, 1+ if it succeeds</returns>
        public async Task<int> RemoveCommentLike(Artist artist, int commentId)
        {
            var connectionString = AppConfig.Value.ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                //Check to make sure like exists
                //Check to make sure the user hasnt already liked this work of art
                var checkDupQuery = "SELECT Count(*) FROM CommentLikes WHERE ArtistID = @ArtistId AND CommentID = @CommentId";
                using (SqlCommand checkDupCommand = new SqlCommand(checkDupQuery, connection))
                {
                    checkDupCommand.Parameters.AddWithValue("@ArtistId", artist.Id);
                    checkDupCommand.Parameters.AddWithValue("@CommentId", commentId);

                    int count = (int)await checkDupCommand.ExecuteScalarAsync();
                    if (count == 0)
                    {
                        Console.WriteLine("The like you are trying to delete doesnt exist!");
                        return 0;
                    }
                }

                var query = "DELETE FROM CommentLikes WHERE ArtistID = @ArtistId AND CommentID = @CommentId";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ArtistId", artist.Id);
                    command.Parameters.AddWithValue("CommentId", commentId);

                    int rowsChanged = await command.ExecuteNonQueryAsync();
                    if (rowsChanged > 0)
                    {
                        return rowsChanged;
                    }
                    else
                    {
                        return -1;
                    }
                }
            }
        }
        /// <summary>
        /// Checks to see if an comment is liked by the user
        /// </summary>
        /// <param name="artist">Id of the user who would've liked the comment</param>
        /// <returns>Returns true if it is liked by the given artist, false otherwise</returns>
        public async Task<bool> IsCommentLiked(Artist artist, int commentId)
        {
            var connectionString = AppConfig.Value.ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string likedQuery = "SELECT Count(*) FROM CommentLikes WHERE ArtistId = @ArtistId AND CommentID = @CommentId";
                using (SqlCommand command = new SqlCommand(likedQuery, connection))
                {

                    command.Parameters.AddWithValue("@CommentID", commentId);
                    command.Parameters.AddWithValue("@ArtistID", artist.Id);

                    int count = (int)await command.ExecuteScalarAsync();

                    if (count > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
    }
}

