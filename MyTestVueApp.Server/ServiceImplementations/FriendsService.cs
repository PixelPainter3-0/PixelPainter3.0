using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using MyTestVueApp.Server.Configuration;
using MyTestVueApp.Server.Entities;
using MyTestVueApp.Server.Enums;
using MyTestVueApp.Server.Interfaces;
using System.ComponentModel.Design;

namespace MyTestVueApp.Server.ServiceImplementations
{
    public class FriendsService : IFriendsService
    {
        private readonly IOptions<ApplicationConfiguration> appConfig;
        private readonly ILogger<FriendsService> logger;


        public FriendsService(IOptions<ApplicationConfiguration> AppConfig, ILogger<FriendsService> Logger)
        {
            appConfig = AppConfig;
            logger = Logger;
        }

        public async Task<int> InsertFriends(Artist artist1, Artist artist2)
        {
            var connectionString = appConfig.Value.ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                //Check to make sure the users aren't already friends
                var checkDupQuery = "SELECT Count(*) FROM Friends WHERE ((Friend1Id = @Friend1Id AND Friend2Id = @Friend2Id))";
                using (SqlCommand checkDupCommand = new SqlCommand(checkDupQuery, connection))
                {
                    checkDupCommand.Parameters.AddWithValue("@Friend1Id", artist1.Id);
                    checkDupCommand.Parameters.AddWithValue("@Friend2Id", artist2.Id);

                    int count = (int)await checkDupCommand.ExecuteScalarAsync();
                    if (count > 0)
                    {
                        Console.WriteLine("These users are already friends!");
                        return 0;
                    }
                }

                var query = "INSERT INTO Friends (Friend1ID, Friend2ID, FriendsOnDate) VALUES (@Friend1Id, @Friend2Id, @Now)";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Friend1Id", artist1.Id);
                    command.Parameters.AddWithValue("@Friend2Id", artist2.Id);
                    command.Parameters.AddWithValue("@Now", DateTime.Now);

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
        public async Task<int> RemoveFriends(Artist artist1, Artist artist2)
        {
            var connectionString = appConfig.Value.ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                //Check to make sure the users are friends
                var checkDupQuery = "SELECT Count(*) FROM Friends WHERE ((Friend1Id = @Friend1Id AND Friend2Id = @Friend2Id))";

                using (SqlCommand checkDupCommand = new SqlCommand(checkDupQuery, connection))
                {
                    checkDupCommand.Parameters.AddWithValue("@Friend1Id", artist1.Id);
                    checkDupCommand.Parameters.AddWithValue("@Friend2Id", artist2.Id);

                    var result = await checkDupCommand.ExecuteScalarAsync();
                    int count = (result == DBNull.Value) ? 0 : Convert.ToInt32(result);
                    if (count == 0)
                    {
                        Console.WriteLine("These users are not friends!");
                        return 0;
                    }
                }
                var query = "DELETE FROM Friends WHERE ((Friend1ID = @Friend1Id AND Friend2ID = @Friend2Id))";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Friend1Id", artist1.Id);
                    command.Parameters.AddWithValue("@Friend2Id", artist2.Id);

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
        public async Task<IEnumerable<Friends>> GetArtistFriends(Artist artist)
        {
            try
            {
                var friends = new List<Friends>();
                var connectionString = appConfig.Value.ConnectionString;

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    var query = @$"
                        SELECT 
                            Friends.Friend1Id,
                            Friends.Friend2Id,
                            Friends.FriendsOnDate,
                            Artist.Id, 
                            Artist.[Name]
                        FROM Friends 
                        JOIN Artist ON Artist.Id = Friends.Friend2Id
                        WHERE Friends.Friend1Id = @id
                        ORDER BY Friends.FriendsOnDate DESC;";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.Add(new SqlParameter("@id", artist.Id));
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                var friend = new Friends
                                { 
                                    Friend1Id = reader.GetInt32(0),
                                    Friend2Id = reader.GetInt32(1),
                                    FriendsOnDate = reader.GetDateTime(2),
                                };
                                friends.Add(friend);
                            }
                        }
                    }
                }
                return friends;
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Error retrieving comments");
                throw;
            }
        }
    }
}