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
    }
}

