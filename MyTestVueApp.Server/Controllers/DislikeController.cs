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
    public class DislikeController : ControllerBase
    {
        private readonly IOptions<ApplicationConfiguration> AppConfig;
        private readonly ILogger<DislikeController> Logger;
        private readonly IDislikeService DislikeService;
        private readonly IArtAccessService ArtService;
        private readonly ILoginService LoginService;

        public DislikeController(IOptions<ApplicationConfiguration> appConfig, ILogger<DislikeController> logger, IDislikeService dislikeService, ILoginService loginService, IArtAccessService artService)
        {
            AppConfig = appConfig;
            Logger = logger;
            ArtService = artService;
            DislikeService = dislikeService;
            LoginService = loginService;
        }

        /// <summary>
        /// Marks the current user to dislike an artwork
        /// </summary>
        /// <param name="artId">Id of the art the user is disliking</param>
        [HttpPost]
        [Route("InsertDislike")]
        public async Task<IActionResult> InsertDislike([FromQuery] int artId)
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
                        var rowsChanged = await DislikeService.InsertDislike(artId, artist);
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
        /// <param name="artId">Id of the art the user is undisliking</param>
        [HttpDelete]
        [Route("RemoveDislike")]
        public async Task<IActionResult> RemoveDislike([FromQuery] int artId)
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
                        var rowsChanged = await DislikeService.RemoveDislike(artId, artist);
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
        /// <param name="artId">Id of the art being checked</param>
        /// <returns>True if it is disliked, false otherwise</returns>
        [HttpGet]
        [Route("IsDisliked")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> IsDisliked([FromQuery] int artId)
        {
            try
            {
                var art = ArtService.GetArtById(artId);
                if (Request.Cookies.TryGetValue("GoogleOAuth", out var userId))
                {
                    var artist = await LoginService.GetUserBySubId(userId);
                    if (await art == null)
                    {
                        throw new ArgumentOutOfRangeException("Art was not found.");
                    }
                    if (artist != null)
                    {
                        var disliked = await DislikeService.IsDisliked(artId, artist);
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

