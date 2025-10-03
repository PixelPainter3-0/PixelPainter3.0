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
        /// <param name="artId">Id being lliked</param>
        /// <param name="artist">Id of the artist who disliked the artwork</param>
        /// <param name="commentId">Id of the comment that is being disliked</param>
        /// <returns>0 if invalid input, -1 if the input failed, and 1+ if it succeeded</returns>
        public async Task<int> InsertCommentDislike(int artId, Artist artist, int commentId)
        {
            var connectionString = AppConfig.Value.ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                //Check to make sure the user hasnt already liked this work of art
                var checkDupQuery = "SELECT Count(*) FROM CommentDislikes WHERE ArtistID = @ArtistId AND ArtID = @ArtId AND CommentID = @CommentId";
                using (SqlCommand checkDupCommand = new SqlCommand(checkDupQuery, connection))
                {
                    checkDupCommand.Parameters.AddWithValue("@ArtistId", artist.Id);
                    checkDupCommand.Parameters.AddWithValue("@ArtId", artId);
                    checkDupCommand.Parameters.AddWithValue("@CommentId", commentId);

                    int count = (int)await checkDupCommand.ExecuteScalarAsync();
                    if (count > 0)
                    {
                        Console.WriteLine("This user has already disliked this comment!");
                        return 0;
                    }
                }

                var query = "INSERT INTO CommentDislikes (ArtistID, ArtID, CommentID, Viewed) VALUES (@ArtistId, @ArtId, @CommentId, 0)";
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
        /// Removes the dislike relation from the database
        /// </summary>
        /// <param name="artId">Artwork being unliked</param>
        /// <param name="artist">Artist who is unliking the artwork</param>
        /// <param name="commentId">Id of the comment that is being undisliked</param>
        /// <returns>0 if bad input, -1 if it fails, 1+ if it succeeds</returns>
        public async Task<int> RemoveCommentDislike(int artId, Artist artist, int commentId)
        {
            var connectionString = AppConfig.Value.ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                //Check to make sure dislike exists
                //Check to make sure the user hasnt already disliked this work of art
                var checkDupQuery = "SELECT Count(*) FROM CommentDislikes WHERE ArtistID = @ArtistId AND ArtID = @ArtId AND CommentID = @CommentId";
                using (SqlCommand checkDupCommand = new SqlCommand(checkDupQuery, connection))
                {
                    checkDupCommand.Parameters.AddWithValue("@ArtistId", artist.Id);
                    checkDupCommand.Parameters.AddWithValue("@ArtId", artId);
                    checkDupCommand.Parameters.AddWithValue("@CommentId", commentId);

                    int count = (int)await checkDupCommand.ExecuteScalarAsync();
                    if (count == 0)
                    {
                        Console.WriteLine("The dislike you are trying to delete doesnt exist!");
                        return 0;
                    }
                }

                var query = "DELETE FROM CommentDislikes WHERE ArtistID = @ArtistId AND ArtID = @ArtId AND CommentID = @CommentId";
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
        /// Checks to see if an comment is disliked by the user
        /// </summary>
        /// <param name="artId">Id of the artwork being checked</param>
        /// <param name="artist">Id of the user who would've disliked the comment</param>
        /// <param name="commentId">Id of the comment that would've been disliked</param>
        /// <returns>Returns true if it is disliked by the given artist, false otherwise</returns>
        public async Task<bool> IsCommentDisliked(int artId, Artist artist, int commentId)
        {
            var connectionString = AppConfig.Value.ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string dislikedQuery = "SELECT Count(*) FROM CommentDislikes WHERE ArtistId = @ArtistId AND ArtID = @ArtID AND CommentID = @CommentId";
                using (SqlCommand command = new SqlCommand(dislikedQuery, connection))
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
        /// Gets all dislikes an artwork has
        /// </summary>
        /// <param name="artworkId">Id of the artwork being referenced</param>
        /// <returns>A list of Like objects</returns>
        public async Task<IEnumerable<CommentDislike>> GetCommentDislikesByArtwork(int artworkId)
        {
            var commentdislikes = new List<CommentDislike>();
            var connectionString = AppConfig.Value.ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                //Need to Append Created On to query when added to database
                string dislikedQuery =
                    $@"
                        SELECT Artist.Name, Art.Title, CommentDislikes.ArtId, CommentDislikes.ArtistId, CommentDislikes.Viewed, CommentDislikes.CommentId
                        FROM CommentDislikes
                        LEFT JOIN Art ON Art.ID = CommentDislikes.ArtID 
                        LEFT join Artist on Artist.Id = CommentDislikes.ArtistId
                        LEFT join Comment on Comment.Id = CommentDislikes.CommentId
                        WHERE CommentDislikes.ArtId = @artworkId";
                using (SqlCommand command = new SqlCommand(dislikedQuery, connection))
                {
                    command.Parameters.AddWithValue("@artworkId", artworkId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            var commentdislike = new CommentDislike
                            {   //ArtId, ArtName
                                CommentId = reader.GetInt32(0),
                                Artist = reader.GetString(1),
                                Artwork = reader.GetString(2),
                                ArtId = reader.GetInt32(3),
                                ArtistId = reader.GetInt32(4),
                                Viewed = reader.GetInt32(5) == 1 ? true : false,
                                DislikedOn = new DateTime()
                            };
                            commentdislikes.Add(commentdislike);
                        }
                    }
                }
            }
            return commentdislikes;
        }
        /// <summary>
        /// Gets the disLike object that belong to the artist and artwork referenced
        /// </summary>
        /// <param name="artId">Id of the art being checked</param>
        /// <param name="artistId">Id of the artist who would've made the dislike</param>
        /// <param name="commentId">Id of comment in which there would be a dislike</param>
        /// <returns>A disLike object if found, null otherwise</returns>
        public async Task<CommentDislike> GetCommentDislikeByIds(int artId, int artistId, int commentId)
        {
            var connectionString = AppConfig.Value.ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                //Need to Append Created On to query when added to database
                string dislikedQuery =
                    $@"
                          SELECT Artist.Name, Art.Title, CommentDislikes.ArtId, CommentDislikes.ArtistId, CommentDislikes.Viewed, CommentDislikes.CommentId
                          FROM CommentDislikes
                          LEFT JOIN Art ON Art.ID = CommentDislikes.ArtID 
                          LEFT JOIN Artist ON CommentDislikes.ArtistId = Artist.Id
                          LEFT JOIN Comment on CommentDislikes.CommentId = Comment.Id
                          WHERE CommentDislikes.ArtId = @art and CommentDislikes.ArtistId = @artist
                          ";
                using (SqlCommand command = new SqlCommand(dislikedQuery, connection))
                {
                    command.Parameters.AddWithValue("@artist", artistId);
                    command.Parameters.AddWithValue("@art", artId);
                    command.Parameters.AddWithValue("@comment", commentId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            var commentdislike = new CommentDislike
                            {   //ArtId, ArtName
                                CommentId = reader.GetInt32(0),
                                Artist = reader.GetString(1),
                                Artwork = reader.GetString(2),
                                ArtId = reader.GetInt32(3),
                                ArtistId = reader.GetInt32(4),
                                Viewed = reader.GetInt32(5) == 1 ? true : false,
                                DislikedOn = new DateTime()
                            };
                            return commentdislike;
                        }
                    }
                }
            }
            throw new ArgumentException("No dislike data in the datbase matches values art id: " + artId + " and artist id: " + artistId);
        }
    }
}
