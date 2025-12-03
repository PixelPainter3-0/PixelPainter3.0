using ImageMagick;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using MyTestVueApp.Server.Configuration;
using MyTestVueApp.Server.Entities;
using MyTestVueApp.Server.Interfaces;
using MyTestVueApp.Server.Models;
using System.Drawing;
using System.Linq;
using Point = MyTestVueApp.Server.Entities.Point;

namespace MyTestVueApp.Server.ServiceImplementations
{
    public class MapAccessService : IMapAccessService
    {
        private readonly IOptions<ApplicationConfiguration> AppConfig;
        private readonly ILogger<MapAccessService> Logger;
        private readonly ITagService TagService;
        private readonly IArtAccessService ArtService;
        private readonly ILoginService LoginService;
        public MapAccessService(IOptions<ApplicationConfiguration> appConfig, ILogger<MapAccessService> logger, ILoginService loginService, ITagService tagService)
        {
            AppConfig = appConfig;
            Logger = logger;
            LoginService = loginService;
            TagService = tagService;
        }

        /// <summary>
        /// Gets all points from the database
        /// </summary>
        /// <returns>A List of map objects</returns>
        public async Task<IEnumerable<Point>> GetAllPoints()
        {
            var points = new List<Point>();
            var connectionString = AppConfig.Value.ConnectionString;

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query1 =
                    @"
                    SELECT Points.PointId,
                      Points.Latitude,
                      Points.Longitude,
                      Points.ArtspaceId,
                      Points.Title
                    FROM Points;";
                using (var command = new SqlCommand(query1, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            var point = new Point()
                            {
                                Id = reader.GetInt32(0),
                                Latitude = reader.GetDecimal(1),
                                Longitude = reader.GetDecimal(2),
                                ArtspaceId = reader.GetInt32(3),
                                Title = reader.GetString(4)
                            };
                            points.Add(point);
                        }
                    }
                }
            }
            return points;
        }

        /// <summary>
        /// Gets point by id
        /// </summary>
        /// <returns>A point</returns>
        public async Task<Point> GetPointById(int id)
        {
            Point point = null;
            var connectionString = AppConfig.Value.ConnectionString;

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query1 =
                    $@"
                    SELECT Points.PointId,
                      Points.Latitude,
                      Points.Longitude,
                      Points.ArtspaceId,
                      Points.Title
                    FROM Points
                    WHERE Points.PointId = @pointId ;";
                using (var command = new SqlCommand(query1, connection))
                {
                    command.Parameters.AddWithValue("@pointId", id);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            point = new Point()
                            {
                                Id = reader.GetInt32(0),
                                Latitude = reader.GetDecimal(1),
                                Longitude = reader.GetDecimal(2),
                                ArtspaceId = reader.GetInt32(3),
                                Title = reader.GetString(4)
                            };
                        }
                    }
                }
            }
            return point;
        }

        /// <summary>
        /// Gets point by artspace
        /// </summary>
        /// <returns>Points</returns>
        public async Task<IEnumerable<Point>> GetArtspacePoints(int id)
        {
            var points = new List<Point>();
            var connectionString = AppConfig.Value.ConnectionString;

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query1 =
                    $@"
                    SELECT Points.PointId,
                      Points.Latitude,
                      Points.Longitude,
                      Points.ArtspaceId,
                      Points.Title
                    FROM Points
                    WHERE Points.ArtspaceId = @artspaceId ;";
                using (var command = new SqlCommand(query1, connection))
                {
                    command.Parameters.AddWithValue("@artspaceId", id);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            var point = new Point()
                            {
                                Id = reader.GetInt32(0),
                                Latitude = reader.GetDecimal(1),
                                Longitude = reader.GetDecimal(2),
                                ArtspaceId = reader.GetInt32(3),
                                Title = reader.GetString(4)
                            };
                            points.Add(point);
                        }
                    }
                }
            }
            return points;
        }

        /// <summary>
        /// Gets all points from the database
        /// </summary>
        /// <returns>A List of map objects</returns>
        public async Task<IEnumerable<Artspace>> GetAllArtspaces()
        {
            var artspaces = new List<Artspace>();
            var connectionString = AppConfig.Value.ConnectionString;

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query1 =
                    @"
                    SELECT Artspace.ArtspaceId,
                      Artspace.Title,
                      Artspace.Shape.STAsText()
                    FROM Artspace;";
                using (var command = new SqlCommand(query1, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            var artspace = new Artspace()
                            {
                                Id = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Shape = reader.GetString(2)
                            };
                            artspaces.Add(artspace);
                        }
                    }
                }
            }
            return artspaces;
        }

    /// <summary>
        /// Gets all points from the database
        /// </summary>
        /// <returns>A List of map objects</returns>
        public async Task<Artspace> GetArtspaceById(int id)
        {
            Artspace artspace = null;
            var connectionString = AppConfig.Value.ConnectionString;

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var query1 =
                    @"
                    SELECT Artspace.ArtspaceId,
                      Artspace.Title,
                      Artspace.Shape.STAsText()
                    FROM Artspace
                    WHERE Artspace.ArtspaceId = 1";
                using (var command = new SqlCommand(query1, connection))
                {
                    command.Parameters.AddWithValue("@artspaceId", id);
                    using (var reader = await command.ExecuteReaderAsync())
                    {

                        if (await reader.ReadAsync())
                        {

                            artspace = new Artspace()
                            {
                                Id = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                Shape = reader.GetString(2)
                            };
                        }
                    }
                }
            }
            return artspace;
        }

        /// <summary>
        /// Gets art by point
        /// </summary>
        /// <returns>Art</returns>
        public async Task<IEnumerable<Art>> GetArtByPoint(int id)
        {
            var art = new List<Art>();
            var connectionString = AppConfig.Value.ConnectionString;

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query1 =
                    $@"
                    Select	
                        Art.ID,
                        Art.Title, 
                        Art.Width, 
                        Art.Height, 
                        Art.Encode, 
                        Art.CreationDate,
                        Art.isPublic,
                        Art.pointId
                    FROM ART  
                    WHERE Art.pointId = @pointId 
                    AND isPublic = 1
                    ";
                using (var command = new SqlCommand(query1, connection))
                {
                    command.Parameters.AddWithValue("@pointId", id);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            var pixelGrid = new PixelGrid()
                            {
                                Width = reader.GetInt32(2),
                                Height = reader.GetInt32(3),
                                EncodedGrid = reader.GetString(4)
                            };
                            var painting = new Art
                            { //Art Table + NumLikes and NumComments
                                Id = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                CreationDate = reader.GetDateTime(5),
                                IsPublic = reader.GetBoolean(6),
                                PointId = reader.GetInt32(7),
                                PixelGrid = pixelGrid,
                            };
                            art.Add(painting);
                        }
                    }
                }
            }
            return art;
        }

        /// <summary>
        /// Adds a point to database
        /// </summary>
        /// <param name="latitude">lat</param>
        /// <param name="longitude">long</param>
        ///  <param name="title">title</param>
        ///  <param name="artspace">artspace</param>
        public async Task<int> CreatePoint(float latitude, float longitude, string title, int artspace)
        {
            try
            {
                using (var connection = new SqlConnection(AppConfig.Value.ConnectionString))
                {
                    connection.Open();

                    var query = @"
                    INSERT INTO Points(latitude,longitude, title, artspaceId) OUTPUT INSERTED.PointId VALUES (@Latitude,@Longitude, @Title, @Artspace);
                ";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Latitude", latitude);
                        command.Parameters.AddWithValue("@Longitude", longitude);
                        command.Parameters.AddWithValue("@Title", title);
                        command.Parameters.AddWithValue("@Artspace", artspace);
                        
                        var newPointId = (int)await command.ExecuteScalarAsync();
                        //Console.Write(newPointId);
                        return newPointId;

                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in Creating Point");
                throw;
                return -1;
            }
        }

        /// <summary>
        /// Tag existing art with location
        /// </summary>
        /// <param name="artId">artId</param>
        /// <param name="pointId">pointId</param>
        public async Task<bool> UpdateArtLocation(int artId, int pointId)
        {
            try
            {
                using (var connection = new SqlConnection(AppConfig.Value.ConnectionString))
                {
                    connection.Open();

                    var query = @"
                    UPDATE Art SET pointId = @PointId WHERE Id = @ArtId;
                ";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ArtId", artId);
                        command.Parameters.AddWithValue("@PointId", pointId);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in Tagging Art with Location");
                throw;
                return false;
            }
        }
    }
}

