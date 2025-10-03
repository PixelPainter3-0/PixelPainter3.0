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
        /// <param name="artId">Id being liked</param>
        /// <param name="artist">Id of the artist who liked the artwork</param>
        /// <param name="commentId">Id of the comment that is being liked</param>
        /// <returns>0 if invalid input, -1 if the input failed, and 1+ if it succeeded</returns>
        public async Task<int> InsertCommentLike(int artId, Artist artist, int commentId)
        {
            var connectionString = AppConfig.Value.ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                //Check to make sure the user hasnt already liked this work of art
                var checkDupQuery = "SELECT Count(*) FROM CommentLikes WHERE ArtistID = @ArtistId AND ArtID = @ArtId AND CommentID = @CommentId";
                using (SqlCommand checkDupCommand = new SqlCommand(checkDupQuery, connection))
                {
                    checkDupCommand.Parameters.AddWithValue("@ArtistId", artist.Id);
                    checkDupCommand.Parameters.AddWithValue("@ArtId", artId);
                    checkDupCommand.Parameters.AddWithValue("@CommentId", commentId);

                    int count = (int)await checkDupCommand.ExecuteScalarAsync();
                    if (count > 0)
                    {
                        Console.WriteLine("This user has already liked this comment!");
                        return 0;
                    }
                }

                var query = "INSERT INTO CommentLikes (ArtistID, ArtID, CommentID, Viewed) VALUES (@ArtistId, @ArtId, @CommentId, 0)";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ArtistId", artist.Id);
                    command.Parameters.AddWithValue("@ArtId", artId);
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
        /// <param name="artId">Artwork being unliked</param>
        /// <param name="artist">Artist who is unliking the artwork</param>
        /// <param name="commentId">Id of the comment that is being unliked</param>
        /// <returns>0 if bad input, -1 if it fails, 1+ if it succeeds</returns>
        public async Task<int> RemoveCommentLike(int artId, Artist artist, int commentId)
        {
            var connectionString = AppConfig.Value.ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                //Check to make sure like exists
                //Check to make sure the user hasnt already liked this work of art
                var checkDupQuery = "SELECT Count(*) FROM CommentLikes WHERE ArtistID = @ArtistId AND ArtID = @ArtId AND CommentID = @CommentId";
                using (SqlCommand checkDupCommand = new SqlCommand(checkDupQuery, connection))
                {
                    checkDupCommand.Parameters.AddWithValue("@ArtistId", artist.Id);
                    checkDupCommand.Parameters.AddWithValue("@ArtId", artId);
                    checkDupCommand.Parameters.AddWithValue("@CommentId", commentId);

                    int count = (int)await checkDupCommand.ExecuteScalarAsync();
                    if (count == 0)
                    {
                        Console.WriteLine("The like you are trying to delete doesnt exist!");
                        return 0;
                    }
                }

                var query = "DELETE FROM CommentLikes WHERE ArtistID = @ArtistId AND ArtID = @ArtId AND CommentID = @CommentId";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ArtistId", artist.Id);
                    command.Parameters.AddWithValue("@ArtId", artId);
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
        /// <param name="artId">Id of the artwork being checked</param>
        /// <param name="artist">Id of the user who would've liked the comment</param>
        /// <param name="commentId">Id of the comment that would've been liked</param>
        /// <returns>Returns true if it is liked by the given artist, false otherwise</returns>
        public async Task<bool> IsCommentLiked(int artId, Artist artist, int commentId)
        {
            var connectionString = AppConfig.Value.ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string likedQuery = "SELECT Count(*) FROM CommentLikes WHERE ArtistId = @ArtistId AND ArtID = @ArtID AND CommentID = @CommentId";
                using (SqlCommand command = new SqlCommand(likedQuery, connection))
                {
                    command.Parameters.AddWithValue("@ArtistId", artist.Id);
                    command.Parameters.AddWithValue("@ArtID", artId);
                    command.Parameters.AddWithValue("@CommentID", commentId);

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
        /// Gets all likes an artwork has
        /// </summary>
        /// <param name="artworkId">Id of the artwork being referenced</param>
        /// <returns>A list of Like objects</returns>
        public async Task<IEnumerable<CommentLike>> GetCommentLikesByArtwork(int artworkId)
        {
            var commentlikes = new List<CommentLike>();
            var connectionString = AppConfig.Value.ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                //Need to Append Created On to query when added to database
                string likedQuery =
                    $@"
                        SELECT Artist.Name, Art.Title, CommentLikes.ArtId, CommentLikes.ArtistId, CommentLikes.Viewed, CommentLikes.CommentId
                        FROM CommentLikes
                        LEFT JOIN Art ON Art.ID = CommentLikes.ArtID 
                        LEFT join Artist on Artist.Id = CommentLikes.ArtistId
                        LEFT join Comment on Comment.Id = CommentLikes.CommentId
                        WHERE CommentLikes.ArtId = @artworkId";
                using (SqlCommand command = new SqlCommand(likedQuery, connection))
                {
                    command.Parameters.AddWithValue("@artworkId", artworkId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            var commentlike = new CommentLike
                            {   //ArtId, ArtName
                                CommentId = reader.GetInt32(0),
                                Artist = reader.GetString(1),
                                Artwork = reader.GetString(2),
                                ArtId = reader.GetInt32(3),
                                ArtistId = reader.GetInt32(4),
                                Viewed = reader.GetInt32(5) == 1 ? true : false,
                                LikedOn = new DateTime()
                            };
                            commentlikes.Add(commentlike);
                        }
                    }
                }
            }
            return commentlikes;
        }
        /// <summary>
        /// Gets the Like object that belong to the artist and artwork referenced
        /// </summary>
        /// <param name="artId">Id of the art being checked</param>
        /// <param name="artistId">Id of the artist who would've made the like</param>
        /// <param name="commentId">Id of comment in which there would be a like</param>
        /// <returns>A Like object if found, null otherwise</returns>
        public async Task<CommentLike> GetCommentLikeByIds(int artId, int artistId, int commentId)
        {
            var connectionString = AppConfig.Value.ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                //Need to Append Created On to query when added to database
                string likedQuery =
                    $@"
                          SELECT Artist.Name, Art.Title, CommentLikes.ArtId, CommentLikes.ArtistId, CommentLikes.Viewed, CommentLikes.CommentId
                          FROM CommentLikes
                          LEFT JOIN Art ON Art.ID = CommentLikes.ArtID 
                          LEFT JOIN Artist ON CommentLikes.ArtistId = Artist.Id
                          LEFT JOIN Comment on CommentLikes.CommentId = Comment.Id
                          WHERE CommentLikes.ArtId = @art and CommentLikes.ArtistId = @artist
                          ";
                using (SqlCommand command = new SqlCommand(likedQuery, connection))
                {
                    command.Parameters.AddWithValue("@artist", artistId);
                    command.Parameters.AddWithValue("@art", artId);
                    command.Parameters.AddWithValue("@comment", commentId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            var commentlike = new CommentLike
                            {   //ArtId, ArtName
                                CommentId = reader.GetInt32(0),
                                Artist = reader.GetString(1),
                                Artwork = reader.GetString(2),
                                ArtId = reader.GetInt32(3),
                                ArtistId = reader.GetInt32(4),
                                Viewed = reader.GetInt32(5) == 1 ? true : false,
                                LikedOn = new DateTime()
                            };
                            return commentlike;
                        }
                    }
                }
            }
            throw new ArgumentException("No like data in the datbase matches values art id: " + artId + " and artist id: " + artistId);
        }
    }
}

