using Microsoft.Extensions.Options;
using MyTestVueApp.Server.Configuration;
using MyTestVueApp.Server.Entities;
using MyTestVueApp.Server.Interfaces;
using Microsoft.Data.SqlClient;
using System.Reflection.Metadata.Ecma335;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace MyTestVueApp.Server.ServiceImplementations
{
    public class CommentDislikeService : ICommentDislikeService
    {
        private readonly IOptions<ApplicationConfiguration> AppConfig;
        private readonly ILogger<CommentDislikeService> Logger;
        public CommentDislikeService(IOptions<ApplicationConfiguration> appConfig, ILogger<CommentDislikeService> logger)
        {
            AppConfig = appConfig;
            Logger = logger;
        }
        /// <summary>
        /// Insert's into the database what artwork an artist has liked
        /// </summary>
        /// <param name="artist">Id of the artist who disliked the artwork</param>
        /// <returns>0 if invalid input, -1 if the input failed, and 1+ if it succeeded</returns>
        public async Task<int> InsertCommentDislike(Artist artist, int commentId)
        {
            var connectionString = AppConfig.Value.ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                //Check to make sure the user hasnt already liked this work of art
                var checkDupQuery = "SELECT Count(*) FROM CommentDislikes WHERE ArtistID = @ArtistId AND CommentID = @CommentId";
                using (SqlCommand checkDupCommand = new SqlCommand(checkDupQuery, connection))
                {
                    checkDupCommand.Parameters.AddWithValue("@ArtistId", artist.Id);
                    checkDupCommand.Parameters.AddWithValue("@CommentId", commentId);

                    int count = (int)await checkDupCommand.ExecuteScalarAsync();
                    if (count > 0)
                    {
                        Console.WriteLine("This user has already disliked this comment!");
                        return 0;
                    }
                }

                var query = "INSERT INTO CommentDislikes (ArtistID, CommentID, Viewed) VALUES (@ArtistId, @CommentId, 0)";
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
        /// Removes the dislike relation from the database
        /// </summary>
        /// <param name="artist">Artist who is unliking the artwork</param>
        /// <returns>0 if bad input, -1 if it fails, 1+ if it succeeds</returns>
        public async Task<int> RemoveCommentDislike(Artist artist, int commentId)
        {
            var connectionString = AppConfig.Value.ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                //Check to make sure dislike exists
                //Check to make sure the user hasnt already disliked this work of art
                var checkDupQuery = "SELECT Count(*) FROM CommentDislikes WHERE ArtistID = @ArtistId AND CommentID = @CommentId";
                using (SqlCommand checkDupCommand = new SqlCommand(checkDupQuery, connection))
                {
                    checkDupCommand.Parameters.AddWithValue("@ArtistId", artist.Id);
                    checkDupCommand.Parameters.AddWithValue("@CommentId", commentId);

                    int count = (int)await checkDupCommand.ExecuteScalarAsync();
                    if (count == 0)
                    {
                        Console.WriteLine("The dislike you are trying to delete doesnt exist!");
                        return 0;
                    }
                }

                var query = "DELETE FROM CommentDislikes WHERE ArtistID = @ArtistId AND CommentID = @CommentId";
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
        /// Checks to see if an comment is disliked by the user
        /// </summary>
        /// <param name="artist">Id of the user who would've disliked the comment</param>
        /// <returns>Returns true if it is disliked by the given artist, false otherwise</returns>
        public async Task<bool> IsCommentDisliked(Artist artist, int commentId)
        {
            var connectionString = AppConfig.Value.ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string dislikedQuery = "SELECT Count(*) FROM CommentDislikes WHERE ArtistId = @ArtistId  AND CommentID = @CommentId";
                using (SqlCommand command = new SqlCommand(dislikedQuery, connection))
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


        /// <summary>
        /// Gets all dislikes a comment has
        /// </summary>
        /// <param name="commentId">Id of the comment being referenced</param>
        /// <returns>A list of CommentDislike objects</returns>
        public async Task<IEnumerable<CommentDislike>> GetDislikesByComment(int commentId)
        {
            var commentDislikes = new List<CommentDislike>();
            var connectionString = AppConfig.Value.ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                //Need to Append Created On to query when added to database
                string commentdislikedQuery =
                    $@"
                        SELECT Artist.Name, CommentDislikes.CommentId, CommentDislikes.ArtistId, CommentDislikes.Viewed 
                        FROM CommentDislikes
                        LEFT JOIN Comment ON Comment.ID = CommentDislikes.CommentID 
                        LEFT JOIN Artist on Artist.Id = CommentDislikes.ArtistId
                        WHERE CommentDislikes.CommentId = @commentId";
                using (SqlCommand command = new SqlCommand(commentdislikedQuery, connection))
                {
                    command.Parameters.AddWithValue("@commentId", commentId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            var commentDislike = new CommentDislike
                            {
                                Artist = reader.GetString(0),
                                CommentId = reader.GetInt32(1),
                                ArtistId = reader.GetInt32(2),
                                Viewed = reader.GetInt32(4) == 1 ? true : false,
                            };
                            commentDislikes.Add(commentDislike);
                        }
                    }
                }
            }
            return commentDislikes;
        }
    }
}