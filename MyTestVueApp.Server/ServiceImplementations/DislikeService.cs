using Microsoft.Extensions.Options;
using MyTestVueApp.Server.Configuration;
using MyTestVueApp.Server.Entities;
using MyTestVueApp.Server.Interfaces;
using Microsoft.Data.SqlClient;
using System.Reflection.Metadata.Ecma335;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace MyTestVueApp.Server.ServiceImplementations
{
      public class DislikeService : IDislikeService
        {
            private readonly IOptions<ApplicationConfiguration> AppConfig;
            private readonly ILogger<DislikeService> Logger;
            public DislikeService(IOptions<ApplicationConfiguration> appConfig, ILogger<DislikeService> logger)
            {
                AppConfig = appConfig;
                Logger = logger;
            }
            /// <summary>
            /// Insert's into the database what artwork an artist has liked
            /// </summary>
            /// <param name="artId">Id being lliked</param>
            /// <param name="artist">Id of the artist who liked the artwork</param>
            /// <returns>0 if invalid input, -1 if the input failed, and 1+ if it succeeded</returns>
            public async Task<int> InsertDislike(int artId, Artist artist)
            {
                var connectionString = AppConfig.Value.ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    //Check to make sure the user hasnt already liked this work of art
                    var checkDupQuery = "SELECT Count(*) FROM Dislikes WHERE ArtistID = @ArtistId AND ArtID = @ArtId";
                    using (SqlCommand checkDupCommand = new SqlCommand(checkDupQuery, connection))
                    {
                        checkDupCommand.Parameters.AddWithValue("@ArtistId", artist.Id);
                        checkDupCommand.Parameters.AddWithValue("@ArtId", artId);

                        int count = (int)await checkDupCommand.ExecuteScalarAsync();
                        if (count > 0)
                        {
                            Console.WriteLine("This user has already disliked this art piece!");
                            return 0;
                        }
                    }

                    var query = "INSERT INTO Dislikes (ArtistID, ArtID, Viewed) VALUES (@ArtistId, @ArtId, 0)";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ArtistId", artist.Id);
                        command.Parameters.AddWithValue("@ArtId", artId);

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
            /// <returns>0 if bad input, -1 if it fails, 1+ if it succeeds</returns>
            public async Task<int> RemoveDislike(int artId, Artist artist)
            {
                var connectionString = AppConfig.Value.ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    //Check to make sure dislike exists
                    //Check to make sure the user hasnt already disliked this work of art
                    var checkDupQuery = "SELECT Count(*) FROM Dislikes WHERE ArtistID = @ArtistId AND ArtID = @ArtId";
                    using (SqlCommand checkDupCommand = new SqlCommand(checkDupQuery, connection))
                    {
                        checkDupCommand.Parameters.AddWithValue("@ArtistId", artist.Id);
                        checkDupCommand.Parameters.AddWithValue("@ArtId", artId);

                        int count = (int)await checkDupCommand.ExecuteScalarAsync();
                        if (count == 0)
                        {
                            Console.WriteLine("The dislike you are trying to delete doesnt exist!");
                            return 0;
                        }
                    }

                    var query = "DELETE FROM Dislikes WHERE ArtistID = @ArtistId AND ArtID = @ArtId";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ArtistId", artist.Id);
                        command.Parameters.AddWithValue("@ArtId", artId);

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
        /// Checks to see if an artwork is liked by the user
        /// </summary>
        /// <param name="artId">Id of the artwork being checked</param>
        /// <param name="artist">Id of the user who would've liked the post</param>
        /// <returns>Returns true if it is liked by the given artist, false otherwise</returns>
        public async Task<bool> IsDisliked(int artId, Artist artist)
            {
                var connectionString = AppConfig.Value.ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string dislikedQuery = "SELECT Count(*) FROM Dislikes WHERE ArtistId = @ArtistId AND ArtID = @ArtID";
                    using (SqlCommand command = new SqlCommand(dislikedQuery, connection))
                    {
                        command.Parameters.AddWithValue("@ArtistId", artist.Id);
                        command.Parameters.AddWithValue("@ArtID", artId);

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
            public async Task<IEnumerable<Dislike>> GetDislikesByArtwork(int artworkId)
            {
                var dislikes = new List<Dislike>();
                var connectionString = AppConfig.Value.ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    //Need to Append Created On to query when added to database
                    string dislikedQuery =
                        $@"
                        SELECT Artist.Name, Art.Title, Dislikes.ArtId, Dislikes.ArtistId, Dislikes.Viewed 
                        FROM Dislikes
                        LEFT JOIN Art ON Art.ID = Dislikes.ArtID 
                        LEft join Artist on Artist.Id = Dislikes.ArtistId
                        WHERE Dislikes.ArtId = @artworkId";
                    using (SqlCommand command = new SqlCommand(dislikedQuery, connection))
                    {
                        command.Parameters.AddWithValue("@artworkId", artworkId);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                var dislike = new Dislike
                                {   //ArtId, ArtName
                                    Artist = reader.GetString(0),
                                    Artwork = reader.GetString(1),
                                    ArtId = reader.GetInt32(2),
                                    ArtistId = reader.GetInt32(3),
                                    Viewed = reader.GetInt32(4) == 1 ? true : false,
                                    DislikedOn = new DateTime()
                                };
                                dislikes.Add(dislike);
                            }
                        }
                    }
                }
                return dislikes;
            }
        /// <summary>
        /// Gets the disLike object that belong to the artist and artwork referenced
        /// </summary>
        /// <param name="artId">Id of the art being checked</param>
        /// <param name="artistId">Id of the artist who would've made the like</param>
        /// <returns>A disLike object if found, null otherwise</returns>
        public async Task<Dislike> GetDislikeByIds(int artId, int artistId)
            {
                var connectionString = AppConfig.Value.ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    //Need to Append Created On to query when added to database
                    string dislikedQuery =
                        $@"
                          SELECT Artist.Name, Art.Title, Dislikes.ArtId, Dislikes.ArtistId, Dislikes.Viewed 
                          FROM Dislikes
                          LEFT JOIN Art ON Art.ID = Dislikes.ArtID 
                          LEFT JOIN Artist ON Dislikes.ArtistId = Artist.Id
                          WHERE Dislikes.ArtId = @art and Dislikes.ArtistId = @artist
                          ";
                    using (SqlCommand command = new SqlCommand(dislikedQuery, connection))
                    {
                        command.Parameters.AddWithValue("@artist", artistId);
                        command.Parameters.AddWithValue("@art", artId);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                var dislike = new Dislike
                                {   //ArtId, ArtName
                                    Artist = reader.GetString(0),
                                    Artwork = reader.GetString(1),
                                    ArtId = reader.GetInt32(2),
                                    ArtistId = reader.GetInt32(3),
                                    Viewed = reader.GetInt32(4) == 1 ? true : false,
                                    DislikedOn = new DateTime()
                                };
                                return dislike;
                            }
                        }
                    }
                }
                throw new ArgumentException("No dislike data in the datbase matches values art id: " + artId + " and artist id: " + artistId);
            }
      }
}
