using ImageMagick;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using MyTestVueApp.Server.Configuration;
using MyTestVueApp.Server.Entities;
using MyTestVueApp.Server.Interfaces;
using MyTestVueApp.Server.Models;
using System.Drawing;
using System.Linq;

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
        /// Gets all artworks from the database
        /// </summary>
        /// <returns>A List of Art objects</returns>
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
                      Points.Title
                    FROM Points;";
                using (var command = new SqlCommand(query1, connection))
                {
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
                            var point = new Point
                            { //Art Table + NumLikes and NumComments
                                Id = reader.GetInt32(0),
                                Title = reader.GetString(3),
                                Latitude = reader.GetDateTime(5),
                                Longitude = reader.GetBoolean(6),
                                IsGif = reader.GetBoolean(7),
                                NumLikes = reader.GetInt32(8),
                                NumDislikes = reader.GetInt32(9),
                                NumComments = reader.GetInt32(10),
                                GifID = reader.GetInt32(11),
                                GifFrameNum = reader.GetInt32(12),
                                PixelGrid = pixelGrid,
                            };
                            points.Add(point);
                        }
                    }
                }
            }
            return points;
        }
        /// <summary>
        /// Gets an artwork by it's Id
        /// </summary>
        /// <param name="id">Id of the artwork to be retrieved</param>
        /// <returns>An art object</returns>
        public async Task<Point> GetPointById(int id)
        {
            var connectionString = AppConfig.Value.ConnectionString;
            var painting = new Art();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var query =
                    $@"
                    Select	
                        Art.ID,
                        Art.Title, 
                        Art.Width, 
                        Art.Height, 
                        Art.Encode, 
                        Art.CreationDate,
                        Art.isPublic,
                        Art.IsGIF,
                        Art.GifId,
                        COUNT(distinct Likes.ArtistId) as Likes, 
                        COUNT(distinct Dislikes.ArtistId) as Dislikes,
                        Count(distinct Comment.Id) as Comments  
                    FROM ART  
                    LEFT JOIN Likes ON Art.ID = Likes.ArtID  
                    LEFT JOIN Dislikes on Art.ID = Dislikes.ArtID
                    LEFT JOIN Comment ON Art.ID = Comment.ArtID  
                    LEFT JOIN ContributingArtists ON Art.Id = ContributingArtists.ArtId
                    WHERE Art.ID = @artId 
                    GROUP BY Art.ID, Art.Title, Art.Width, Art.Height, Art.Encode, Art.CreationDate, Art.isPublic, Art.IsGIF, Art.gifId;
                    ";
                
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@artId", id);
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
                            painting = new Art
                            { //ArtId, ArtName, Width, ArtLength, Encode, Date, IsPublic
                                Id = reader.GetInt32(0),
                                Title = reader.GetString(1),
                                PixelGrid = pixelGrid,
                                CreationDate = reader.GetDateTime(5),
                                IsPublic = reader.GetBoolean(6),
                                NumLikes = reader.GetInt32(9),
                                NumDislikes = reader.GetInt32(10),
                                NumComments = reader.GetInt32(11),
                                IsGif = reader.GetBoolean(7),
                                GifID = reader.GetInt32(8)
                            };
                            painting.SetArtists((await GetArtistsByArtId(painting.Id)).ToList());
                            // Add this line to fetch and assign tags
                            painting.Tags = (await TagService.GetTagsForArt(painting.Id)).ToList();
                            return painting;
                        }
                    }
                }
            }
            return null;
        }
        /// <summary>
        /// Grabs all artwork than and artist made
        /// </summary>
        /// <param name="artistId">Id of the artist</param>
        /// <returns>A list of artworks</returns>
    }
}

