using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MyTestVueApp.Server.Configuration;
using MyTestVueApp.Server.Entities;
using MyTestVueApp.Server.Interfaces;
using MyTestVueApp.Server.ServiceImplementations;
using System.Security.Authentication;


namespace MyTestVueApp.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CommentDislikeController : ControllerBase
    {
        private readonly IOptions<ApplicationConfiguration> AppConfig;
        private readonly ILogger<CommentDislikeController> Logger;
        private readonly ICommentDislikeService CommentDislikeService;
        private readonly ICommentAccessService CommentService;
        private readonly ILoginService LoginService;

        public CommentDislikeController(IOptions<ApplicationConfiguration> appConfig, ILogger<CommentDislikeController> logger, ICommentDislikeService commentDislikeService, ILoginService loginService, ICommentAccessService commentService)
        {
            AppConfig = appConfig;
            Logger = logger;
            CommentService = commentService;
            CommentDislikeService = commentDislikeService;
            LoginService = loginService;
        }

        /// <summary>
        /// Marks the current user to dislike an artwork
        /// </summary>
        [HttpPost]
        [Route("InsertCommentDislike")]
        public async Task<IActionResult> InsertCommentDislike([FromQuery] int commentId)
        {
            try
            {
                // If the user is logged in
                if (Request.Cookies.TryGetValue("GoogleOAuth", out var userId))
                {
                    var artist = await LoginService.GetUserBySubId(userId);
                    if (artist != null)
                    {
                        // You can add additional checks here if needed
                        var rowsChanged = await CommentDislikeService.InsertCommentDislike(artist, commentId);
                        if (rowsChanged > 0) // If the dislike has sucessfully been inserted
                        {
                            return Ok();
                        }
                        else
                        {
                            throw new ArgumentException("Failed to insert dislike. User may have already disliked this post.");
                        }
                    }
                    else
                    {
                        throw new AuthenticationException("User does not have an account.");
                    }
                }
                else
                {
                    throw new AuthenticationException("User is not logged in!");
                }
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (AuthenticationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        /// <summary>
        /// Remove a dislike from the database
        /// </summary>
        [HttpDelete]
        [Route("RemoveCommentDislike")]
        public async Task<IActionResult> RemoveCommentDislike([FromQuery] int commentId)
        {
            try
            {
                // If the user is logged in
                if (Request.Cookies.TryGetValue("GoogleOAuth", out var userId))
                {
                    var artist = await LoginService.GetUserBySubId(userId);
                    if (artist != null)
                    {
                        // You can add additional checks here if needed
                        var rowsChanged = await CommentDislikeService.RemoveCommentDislike(artist, commentId);
                        if (rowsChanged > 0) // If the dislike has sucessfully been removed
                        {
                            return Ok();
                        }
                        else
                        {
                            throw new ArgumentException("Did not remove target dislike. It may not have existed.");
                        }
                    }
                    else
                    {
                        throw new AuthenticationException("User does not have an account");
                    }
                }
                else
                {
                    throw new AuthenticationException("User is not logged in!");
                }
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (AuthenticationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        /// <summary>
        /// Checks if artwork is disliked by current user
        /// </summary>
        /// <returns>True if it is disliked, false otherwise</returns>
        [HttpGet]
        [Route("IsCommentDisliked")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> IsCommentDisliked([FromQuery]int commentId)
        {
            try
            {
                if (Request.Cookies.TryGetValue("GoogleOAuth", out var userId))
                {
                    var artist = await LoginService.GetUserBySubId(userId);
                    if (artist != null)
                    {
                        var disliked = await CommentDislikeService.IsCommentDisliked(artist, commentId);
                        return Ok(disliked);
                    }
                    else
                    {
                        throw new AuthenticationException("User does not have an account");
                    }
                }
                else
                {
                    throw new AuthenticationException("User is not logged in!");
                }
            }
            catch (AuthenticationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }
    }
}