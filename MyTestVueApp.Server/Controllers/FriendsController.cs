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
    public class FriendsController : ControllerBase
    {
        private readonly IOptions<ApplicationConfiguration> AppConfig;
        private readonly ILogger<FriendsController> Logger;
        private readonly IFriendsService FriendsService;
        private readonly ILoginService LoginService;

        public FriendsController(IOptions<ApplicationConfiguration> appConfig, ILogger<FriendsController> logger, IFriendsService friendsService, ILoginService loginService)
        {
            AppConfig = appConfig;
            Logger = logger;
            FriendsService = friendsService;
            LoginService = loginService;
            


        }

        /// <summary>
        /// Adds logged in user and another user to the friends table
        /// </summary>
        /// <param name="artistId">Id of the user the logged in user wants to be friends with</param>
        [HttpPost]
        [Route("InsertFriends")]
        public async Task<IActionResult> InsertDislike([FromQuery] int artistId)
        {
            try
            {
                // If the user is logged in
                if (Request.Cookies.TryGetValue("GoogleOAuth", out var userId))
                {
                    var artist = await LoginService.GetUserBySubId(userId);
                    var artist2 = await LoginService.GetArtistById(artistId);
                    if (artist != null)
                    {
                        // You can add additional checks here if needed
                        var rowsChanged = await FriendsService.InsertFriends(artist, artist2);
                        if (rowsChanged > 0) // If the friends has sucessfully been inserted
                        {
                            return Ok();
                        }
                        else
                        {
                            throw new ArgumentException("Failed to insert friends. User may have already be friends with this other user.");
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
        /// Remove friends from the database
        /// </summary>
        /// <param name="artistId">Id of the user the logged in user is trying to not be friends with</param>
        [HttpDelete]
        [Route("RemoveFriends")]
        public async Task<IActionResult> RemoveDislike([FromQuery] int artistId)
        {
            try
            {
                // If the user is logged in
                if (Request.Cookies.TryGetValue("GoogleOAuth", out var userId))
                {
                    var artist = await LoginService.GetUserBySubId(userId);
                    var artist2 = await LoginService.GetArtistById(artistId);
                    if (artist != null)
                    {
                        // You can add additional checks here if needed
                        var rowsChanged = await FriendsService.RemoveFriends(artist, artist2);
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
        [Route("GetArtistFriends")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> GetArtistFriends()
        {
            try
            {
                if (Request.Cookies.TryGetValue("GoogleOAuth", out var userId))
                {
                    var artist = await LoginService.GetUserBySubId(userId);
                    if (artist != null)
                    {
                        var friends = await FriendsService.GetArtistFriends(artist);
                        return Ok(friends);
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
            catch (NullReferenceException ex)
            {
                return Problem(ex.Message);
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
