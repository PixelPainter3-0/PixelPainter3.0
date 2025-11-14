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
    public class NotificationService : INotificationService
    {
        private readonly IOptions<ApplicationConfiguration> appConfig;
        private readonly ILogger<NotificationService> logger;
        private readonly IArtAccessService artService;
        private readonly ICommentAccessService commentService;
        private readonly ILikeService likeService;
        private readonly IDislikeService dislikeService;
        private readonly IArtistService artistService;
        private readonly ICommentLikeService commentLikeService;
        private readonly ICommentDislikeService commentDislikeService;


        public NotificationService(IOptions<ApplicationConfiguration> AppConfig, ILogger<NotificationService> Logger, IArtAccessService ArtAccessService, ICommentAccessService CommentAccessService, ILikeService LikeService, IDislikeService DislikeService, IArtistService ArtistService, ICommentLikeService CommentLikeService, ICommentDislikeService CommentDislikeService)
        {
            appConfig = AppConfig;
            logger = Logger;
            artService = ArtAccessService;
            commentService = CommentAccessService;
            likeService = LikeService;
            dislikeService = DislikeService;
            artistService = ArtistService;
            commentLikeService = CommentLikeService;
            commentDislikeService = CommentDislikeService;
        }

        public async Task<IEnumerable<Notification>> GetNotificationsForArtist(int artistId)
        {
            DateTime thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var notifications = new List<Notification>();

            // Get artist and notificationsEnabled
            var artist = await artistService.GetArtistById(artistId);
            if (artist == null)
            {
                logger.LogWarning("Artist with ID {ArtistId} not found", artistId);
                return notifications;
            }

            int enabledMask = artist.NotificationsEnabled == 0 ? 63 : artist.NotificationsEnabled;

            var artworks = await artService.GetArtByArtist(artistId);

            var comments = await commentService.GetCommentByUserId(artistId);

            foreach (var artwork in artworks)
            {
                // Likes
                if ((enabledMask & 1) != 0)
                {
                    var likes = await likeService.GetLikesByArtwork(artwork.Id);
                    foreach (var like in likes)
                    {
                        if (like.ArtistId == artistId || like.LikedOn < thirtyDaysAgo) continue;

                        notifications.Add(new Notification
                        {
                            CommentId = -1,
                            ArtId = like.ArtId,
                            ArtistId = like.ArtistId,
                            Type = 1,
                            User = like.Artist,
                            Viewed = like.Viewed,
                            ArtName = artwork.Title
                        });
                    }
                }
                if ((enabledMask & 32) != 0)
                {
                    var dislikes = await dislikeService.GetDislikesByArtwork(artwork.Id);
                    foreach (var dislike in dislikes)
                    {
                        if (dislike.ArtistId == artistId || dislike.DislikedOn < thirtyDaysAgo) continue;

                        notifications.Add(new Notification
                        {
                            CommentId = -1,
                            ArtId = dislike.ArtId,
                            ArtistId = dislike.ArtistId,
                            Type = 2,
                            User = dislike.Artist,
                            Viewed = dislike.Viewed,
                            ArtName = artwork.Title
                        });
                    }
                }

                // Comments
                if ((enabledMask & 2) != 0)
                {
                    foreach (var comment in comments)
                    {
                        if (comment.ArtistId == artistId || comment.CreationDate < thirtyDaysAgo || comment.ReplyId != null) continue;

                        notifications.Add(new Notification
                        {
                            CommentId = comment.Id,
                            ArtId = -1,
                            ArtistId = -1,
                            Type = 0,
                            User = comment.CommenterName,
                            Viewed = comment.Viewed,
                            ArtName = artwork.Title
                        });
                    }
                }

            }

            // Replies
            if ((enabledMask & 4) != 0)
            {
                var userComments = await commentService.GetCommentByUserId(artistId);
                foreach (var comment in userComments)
                {
                    var replies = await commentService.GetReplyByCommentId(comment.Id);
                    foreach (var reply in replies)
                    {
                        if (reply.ArtistId == artistId || reply.CreationDate < thirtyDaysAgo) continue;

                        notifications.Add(new Notification
                        {
                            CommentId = reply.Id,
                            ArtId = -1,
                            ArtistId = -1,
                            Type = 3,
                            User = reply.CommenterName,
                            Viewed = reply.Viewed,
                            ArtName = ""
                        });
                    }
                }
            }

            //Comment Like 
            if ((enabledMask & 8) != 0)
            {
                var userComments = await commentService.GetCommentByUserId(artistId);

                    foreach (var userComment in userComments)
                    {
                    var commentLikes = await commentLikeService.GetLikesByComment(userComment.Id);
                        foreach (var commentLike in commentLikes)
                        {
                            if (userComment.ArtistId != commentLike.ArtistId)
                            {
                                notifications.Add(new Notification
                                {
                                    CommentId = commentLike.CommentId,
                                    ArtistId = userComment.ArtistId,
                                    ArtId = -1,
                                    Type = 4,
                                    User = userComment.CommenterName,
                                    Viewed = userComment.Viewed,
                                    ArtName = ""
                                });
                            }
                        }
                    }
            }

            //Comment Dislike
            if ((enabledMask & 16) != 0)
            {
                var userComments = await commentService.GetCommentByUserId(artistId);

                foreach (var userComment in userComments)
                {
                    var commentDislikes = await commentDislikeService.GetDislikesByComment(userComment.Id);
                    foreach (var commentDislike in commentDislikes)
                    {
                        if (userComment.ArtistId != commentDislike.ArtistId)
                        {
                            notifications.Add(new Notification
                            {
                                CommentId = commentDislike.CommentId,
                                ArtistId = userComment.ArtistId,
                                ArtId = -1,
                                Type = 5,
                                User = userComment.CommenterName,
                                Viewed = userComment.Viewed,
                                ArtName = ""
                            });
                        }
                    }
                }
            }

            return notifications;
        }
        public async Task<bool> MarkComment(int commentId)
        {
            var connectionString = appConfig.Value.ConnectionString;
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            string query = "UPDATE Comment SET Viewed = 1 WHERE Id = @commentId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@commentId", commentId);

            return await command.ExecuteNonQueryAsync() > 0;
        }
        public async Task<bool> MarkLike(int artId, int artistId)
        {
            var connectionString = appConfig.Value.ConnectionString;
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            string query = "UPDATE Likes SET Viewed = 1 WHERE ArtId = @artId AND ArtistId = @artistId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@artId", artId);
            command.Parameters.AddWithValue("@artistId", artistId);

            return await command.ExecuteNonQueryAsync() > 0;
        }
        public async Task<bool> MarkDislike(int artId, int artistId)
        {
            var connectionString = appConfig.Value.ConnectionString;
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            string query = "UPDATE Dislikes SET Viewed = 1 WHERE ArtId = @artId AND ArtistId = @artistId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@artId", artId);
            command.Parameters.AddWithValue("@artistId", artistId);

            return await command.ExecuteNonQueryAsync() > 0;
        }
        public async Task<bool> UpdateNotificationsEnabledAsync(int artistId, int notificationsEnabled)
        {
            var connectionString = appConfig.Value.ConnectionString;
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            string query = "UPDATE Artist SET NotificationsEnabled = @notificationsEnabled WHERE Id = @artistId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@notificationsEnabled", notificationsEnabled);
            command.Parameters.AddWithValue("@artistId", artistId);

            return await command.ExecuteNonQueryAsync() > 0;
        }
    }
}
