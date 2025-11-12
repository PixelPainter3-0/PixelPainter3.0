using Microsoft.AspNetCore.Mvc;
using MyTestVueApp.Server.Entities;
using MyTestVueApp.Server.Interfaces;
using MyTestVueApp.Server.Models;
using MyTestVueApp.Server.ServiceImplementations;

namespace MyTestVueApp.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly ILogger<NotificationController> logger;
        private readonly INotificationService notificationService;
        public NotificationController(ILogger<NotificationController> Logger, INotificationService NotificationService)
        {
            logger = Logger;
            notificationService = NotificationService;
        }

        [HttpGet("GetNotificationsForArtist")]
        [ProducesResponseType(typeof(List<Notification>), 200)]
        public async Task<IActionResult> GetNotificationsForArtist([FromQuery] int userId)
        {
            try
            {
                var notifications = await notificationService.GetNotificationsForArtist(userId);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting notifications for user {UserId}", userId);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("MarkCommentViewed")]
        public async Task<IActionResult> MarkCommentViewed([FromBody] int commentId)
        {
            try
            {
                bool success = await notificationService.MarkComment(commentId);
                return success ? Ok() : StatusCode(500, "Failed to mark comment viewed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error marking comment {CommentId} viewed", commentId);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("MarkLikeViewed")]
        public async Task<IActionResult> MarkLikeViewed([FromBody] LikesModel likeModel)
        {
            try
            {
                bool success = await notificationService.MarkLike(likeModel.ArtId, likeModel.ArtistId);
                return success ? Ok() : StatusCode(500, "Failed to mark like viewed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error marking like for art {ArtId} and artist {ArtistId} viewed", likeModel.ArtId, likeModel.ArtistId);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("MarkDislikeViewed")]
        public async Task<IActionResult> MarkDislikeViewed([FromBody] DislikesModel dislikeModel)
        {
            try
            {
                bool success = await notificationService.MarkDislike(dislikeModel.ArtId, dislikeModel.ArtistId);
                return success ? Ok() : StatusCode(500, "Failed to mark dislike viewed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error marking dislike for art {ArtId} and artist {ArtistId} viewed", dislikeModel.ArtId, dislikeModel.ArtistId);
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost("UpdateNotificationsEnabled")]
        public async Task<IActionResult> UpdateNotificationsEnabled([FromBody] UpdateNotificationsModel model)
        {
            try
            {
                bool success = await notificationService.UpdateNotificationsEnabledAsync(model.ArtistId, model.NotificationsEnabled);
                return success ? Ok() : StatusCode(500, "Failed to update notifications settings");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating notifications for artist {ArtistId}", model.ArtistId);
                return StatusCode(500, ex.Message);
            }
        }
    }
}