using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyTestVueApp.Server.Configuration;
using MyTestVueApp.Server.Entities;
using MyTestVueApp.Server.Interfaces;

namespace MyTestVueApp.Server.ServiceImplementations
{
    public class ArtistService : IArtistService
    {
        private readonly IOptions<ApplicationConfiguration> _appConfig;
        private readonly ILogger<ArtistService> _logger;

        public ArtistService(IOptions<ApplicationConfiguration> appConfig, ILogger<ArtistService> logger)
        {
            _appConfig = appConfig;
            _logger = logger;
        }

        public async Task<Artist?> GetArtistById(int artistId)
        {
            var connectionString = _appConfig.Value.ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = @"SELECT Id, Name, IsAdmin, PrivateProfile, CreationDate, Email, NotificationsEnabled 
                                 FROM Artist 
                                 WHERE Id = @artistId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@artistId", artistId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new Artist
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Name = reader.GetString(reader.GetOrdinal("Name")),
                                IsAdmin = reader.GetBoolean(reader.GetOrdinal("IsAdmin")),
                                PrivateProfile = reader.GetBoolean(reader.GetOrdinal("PrivateProfile")),
                                Email = reader.GetString(reader.GetOrdinal("Email")),
                                NotificationsEnabled = reader.GetInt32(reader.GetOrdinal("NotificationsEnabled"))
                            };
                        }
                    }
                }
            }

            _logger.LogWarning("Artist with ID {ArtistId} not found.", artistId);
            return null;
        }

        public async Task<IEnumerable<Artist>> GetAllArtists()
        {
            var artists = new List<Artist>();
            var connectionString = _appConfig.Value.ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = @"SELECT Id, Name, IsAdmin, PrivateProfile, CreationDate, Email, NotificationsEnabled 
                                 FROM Artist";

                using (SqlCommand command = new SqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        artists.Add(new Artist
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            IsAdmin = reader.GetBoolean(reader.GetOrdinal("IsAdmin")),
                            PrivateProfile = reader.GetBoolean(reader.GetOrdinal("PrivateProfile")),
                            Email = reader.GetString(reader.GetOrdinal("Email")),
                            NotificationsEnabled = reader.GetInt32(reader.GetOrdinal("NotificationsEnabled"))
                        });
                    }
                }
            }

            return artists;
        }

        public async Task<bool> UpdateArtistNotificationsEnabled(int artistId, int notificationsEnabled)
        {
            var connectionString = _appConfig.Value.ConnectionString;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = @"
                    UPDATE Artist
                    SET NotificationsEnabled = @notificationsEnabled
                    WHERE Id = @artistId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@notificationsEnabled", notificationsEnabled);
                    command.Parameters.AddWithValue("@artistId", artistId);

                    int rowsChanged = await command.ExecuteNonQueryAsync();

                    if (rowsChanged > 0)
                    {
                        _logger.LogInformation("Updated NotificationsEnabled to {Value} for artist {ArtistId}", notificationsEnabled, artistId);
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("No artist found with ID {ArtistId} when updating NotificationsEnabled", artistId);
                        return false;
                    }
                }
            }
        }
    }
}
