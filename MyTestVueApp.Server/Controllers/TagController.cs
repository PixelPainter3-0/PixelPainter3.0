using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MyTestVueApp.Server.Configuration;
using MyTestVueApp.Server.Entities;
using MyTestVueApp.Server.Interfaces;
using System.Security.Authentication;
using MyTestVueApp.Server.Models;

namespace MyTestVueApp.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TagController : ControllerBase
    {
        private readonly IOptions<ApplicationConfiguration> AppConfig;
        private readonly ILogger<TagController> Logger;
        private readonly ITagService TagService;
        private readonly ILoginService LoginService;

        public TagController(
            IOptions<ApplicationConfiguration> appConfig,
            ILogger<TagController> logger,
            ITagService tagService,
            ILoginService loginService)
        {
            AppConfig = appConfig;
            Logger = logger;
            TagService = tagService;
            LoginService = loginService;
        }

        /// <summary>
        /// Get all tags
        /// </summary>
        [HttpGet]
        [Route("GetAll")]
        public async Task<IActionResult> GetAllTags()
        {
            try
            {
                var tags = await TagService.GetAllTags();
                return Ok(tags);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        /// <summary>
        /// Create a new tag
        /// </summary>
        [HttpPost]
        [Route("Create")]
        public async Task<IActionResult> CreateTag([FromBody] Tag tag)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tag.Name))
                    return BadRequest("Tag name is required.");

                var created = await TagService.CreateTag(tag);
                if (created!=null)
                    return Ok(created);
                else
                    return BadRequest("Tag could not be created.");
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        /// <summary>
        /// Assign tags to an art piece
        /// </summary>
        [HttpPost]
        [Route("AssignToArt")]
        public async Task<IActionResult> AssignTagsToArt([FromBody] TagModel Tags)
        {
            try
            {
                if (Request.Cookies.TryGetValue("GoogleOAuth", out var userId))
                {
                    var artist = await LoginService.GetUserBySubId(userId);
                    if (artist == null)
                        throw new AuthenticationException("User does not have an account.");

                    var result = await TagService.AssignTagsToArt(Tags.ArtId, Tags.TagIDs);
                    if (result)
                        return Ok();
                    else
                        return BadRequest("Failed to assign tags.");
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
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        /// <summary>
        /// Get tags for a specific art piece
        /// </summary>
        [HttpGet]
        [Route("GetTagsForArt")]
        public async Task<IActionResult> GetTagsForArt([FromQuery] int artId)
        {
            try
            {
                var tags = await TagService.GetTagsForArt(artId);
                return Ok(tags);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        /// <summary>
        /// Remove a tag from an art piece
        /// </summary>
        [HttpDelete]
        [Route("RemoveFromArt")]
        public async Task<IActionResult> RemoveTagFromArt([FromQuery] int artId, [FromQuery] int tagId)
        {
            try
            {
                if (Request.Cookies.TryGetValue("GoogleOAuth", out var userId))
                {
                    var artist = await LoginService.GetUserBySubId(userId);
                    if (artist == null)
                        throw new AuthenticationException("User does not have an account.");

                    var result = await TagService.RemoveTagFromArt(artId, tagId, artist);
                    if (result)
                        return Ok();
                    else
                        return BadRequest("Failed to remove tag.");
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
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        /// <summary>
        /// Delete a tag (admin only). Also removes any Art-Tag associations.
        /// </summary>
        [HttpDelete]
        [Route("DeleteTag")]
        public async Task<IActionResult> DeleteTag([FromQuery] int tagId)
        {
            try
            {

                if (Request.Cookies.TryGetValue("GoogleOAuth", out var userId))
                {
                    var user = await LoginService.GetUserBySubId(userId);
                    if (user == null)
                        throw new AuthenticationException("User does not have an account.");
                    if (!user.IsAdmin)
                        throw new AuthenticationException("User does not have permission to delete tags.");

                    var deleted = await TagService.DeleteTag(tagId);
                    if (deleted)
                        return Ok();
                    else
                        return NotFound($"Tag with id {tagId} was not found.");
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
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }
    }
}
