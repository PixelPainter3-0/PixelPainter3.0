using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MyTestVueApp.Server.Configuration;
using MyTestVueApp.Server.Entities;
using MyTestVueApp.Server.Interfaces;
using MyTestVueApp.Server.ServiceImplementations;
using System.Security.Authentication;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MyTestVueApp.Server.Controllers
{
    [ApiController]
    [Route("MapAccess")]
    public class MapAccessController : ControllerBase
    {
        private readonly ILogger<MapAccessController> Logger; 
        private readonly IMapAccessService MapAccessService;
        private readonly ILoginService LoginService;

        public MapAccessController(ILogger<MapAccessController> logger, IMapAccessService mapAccessService, ILoginService loginService)
        {
            Logger = logger;
            MapAccessService = mapAccessService;
            LoginService = loginService;
        }

        /// <summary>
        /// Gets All Points
        /// </summary>
        [HttpGet]
        [Route("GetAllPoints")]
        [ProducesResponseType(typeof(List<Point>), 200)]
        public async Task<IActionResult> GetAllPoints()
        {
            try
            {
                var points = await MapAccessService.GetAllPoints();
                return Ok(points);
            } 
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        /// <summary>
        /// Gets point by id
        /// </summary>
        [Produces("application/json")]
        [HttpGet]
        [Route("GetPointById")]
        [ProducesResponseType(typeof(Point), 200)]
        public async Task<IActionResult> GetPointById([FromQuery] int id)
        {
            try
            {
                var point = await MapAccessService.GetPointById(id);

                if (point == null)
                {
                    throw new ArgumentException("Point with id: " + id + " can not be found");
                }

                return Ok(point);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        /// <summary>
        /// Gets All Points from artspace
        /// </summary>
        [HttpGet]
        [Route("GetArtspacePoints")]
        [ProducesResponseType(typeof(List<Point>), 200)]
        public async Task<IActionResult> GetArtspacePoints([FromQuery] int id)
        {
            try
            {
                var points = await MapAccessService.GetArtspacePoints(id);

                if (points == null)
                {
                    throw new ArgumentException("Points with artspace id: " + id + " can not be found");
                }

                return Ok(points);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        /// <summary>
        /// Gets All Artspaces
        /// </summary>
        [HttpGet]
        [Route("GetAllArtspaces")]
        [ProducesResponseType(typeof(List<Artspace>), 200)]
        public async Task<IActionResult> GetAllArtspaces()
        {
            try
            {
                var spaces = await MapAccessService.GetAllArtspaces();
                return Ok(spaces);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        /// <summary>
        /// Gets artspace by id
        /// </summary>
        [HttpGet]
        [Route("GetArtspaceById")]
        [ProducesResponseType(typeof(Artspace), 200)]
        public async Task<IActionResult> GetArtspaceById([FromQuery] int id)
        {
            try
            {
                var space = await MapAccessService.GetArtspaceById(id);

                if (space == null)
                {
                    throw new ArgumentException("Artspace with id: " + id + " can not be found");
                }

                return Ok(space);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        /// <summary>
        /// Gets All Artspaces
        /// </summary>
        [HttpGet]
        [Route("GetArtByPoint")]
        [ProducesResponseType(typeof(List<Art>), 200)]
        public async Task<IActionResult> GetArtByPoint([FromQuery] int id)
        {
            try
            {
                var art = await MapAccessService.GetArtByPoint(id);

                if (art == null)
                {
                    throw new ArgumentException("Art with point id: " + id + " can not be found");
                }

                return Ok(art);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        /// <summary>
        /// Creates a point
        /// </summary>
        /// <param name="latitude">lat</param>
        /// <param name="longitude">long</param>
        /// <param name="title">title</param>
        /// <param name="artspace">artspace</param>
        [HttpPut]
        [Route("CreatePoint")]
        public async Task<IActionResult> CreatePoint([FromQuery] float latitude, [FromQuery] float longitude, [FromQuery] string title, [FromQuery] int artspace)
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
                        var pointId = await MapAccessService.CreatePoint(latitude, longitude, title, artspace);
                        if (pointId != 0) // If the like has sucessfully been inserted
                        {
                            return Ok(new { pointId });
                        }
                        else
                        {
                            throw new ArgumentException("Failed to create point.");
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
        /// Update existing art location
        /// </summary>
        /// <param name="artId">artId</param>
        /// <param name="pointId">pointId</param>
        [HttpPut]
        [Route("UpdateArtLocation")]
        public async Task<IActionResult> UpdateArtLocation([FromQuery] int artId, [FromQuery] int pointId)
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
                        var updated = await MapAccessService.UpdateArtLocation(artId, pointId);
                        if (updated) // If the like has sucessfully been inserted
                        {
                            return Ok();
                        }
                        else
                        {
                            throw new ArgumentException("Failed to update lcoation.");
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
        /// Delete location
        /// </summary>
        /// <param name="pointId">pointId</param>
        [HttpPut]
        [Route("DeleteLocation")]
        public async Task<IActionResult> DeleteLocation([FromQuery] int pointId)
        {
            try
            {
                // If the user is logged in
                if (Request.Cookies.TryGetValue("GoogleOAuth", out var userId))
                {
                    var artist = await LoginService.GetUserBySubId(userId);
                    if (!artist.IsAdmin)
                    {
                        throw new AuthenticationException("User does not have permission to delete tags.");
                    }
                    else if (artist != null)
                    {
                        // You can add additional checks here if needed
                        var deleted = await MapAccessService.DeleteLocation(pointId);
                        if (deleted) // If the like has sucessfully been inserted
                        {
                            return Ok();
                        }
                        else
                        {
                            throw new ArgumentException("Failed to delete location.");
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
    }
}
