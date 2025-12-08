using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MyTestVueApp.Server.Entities;
using MyTestVueApp.Server.Hubs;
using MyTestVueApp.Server.Interfaces;
using MyTestVueApp.Server.ServiceImplementations;
using System.Security.Authentication;
using System.Threading;

namespace MyTestVueApp.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SocketController : ControllerBase
    {
        private readonly ILogger<SocketController> Logger;
        private readonly ILoginService LoginService;
        private readonly IArtAccessService ArtAccessService;
        private readonly IConnectionManager ConnectionManager;
        private readonly IHubContext<SignalHub> HubContext;
        
        public SocketController(IConnectionManager connectionManager, ILoginService loginService, IArtAccessService artAccessService, ILogger<SocketController> logger, IHubContext<SignalHub> hubContext)
        {
            ConnectionManager = connectionManager;
            LoginService = loginService;
            ArtAccessService = artAccessService;
            Logger = logger;
            HubContext = hubContext;
        }

        [HttpGet]
        [Route("GetGroups")]
        public IActionResult GetGroups()
        {
            try
            {
                return Ok(ConnectionManager.GetGroupAdverts());
            } catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [HttpPost]
        [Route("SaveGrid")]
        public async Task<IActionResult> SaveGrid([FromBody] string name)
        {
            try
            {
                if (Request.Cookies.TryGetValue("GoogleOAuth", out var userSubId))
                {
                    var gridArtist = LoginService.GetArtistById(0);
                    var artist = await LoginService.GetUserBySubId(userSubId);
                    if (artist == null)
                    {
                        throw new AuthenticationException("User does not have an account.");
                    }
                    else if (artist.IsAdmin != true)
                    {
                        throw new UnauthorizedAccessException("User does not have permission to save the grid");
                    }
                    var gridArt = ConnectionManager.ConvertGridToArt(name);
                    var newArt = await ArtAccessService.SaveNewArt(await gridArtist, gridArt);
                    ConnectionManager.ResetGrid();

                    var Grid = ConnectionManager.GetGrid();
                    var TimeOuts = ConnectionManager.TimeOuts(artist.Id);
                    await HubContext.Clients.Group(Grid.GroupName).SendAsync("GridConfig", Grid.CanvasSize, Grid.BackgroundColor, Grid.GetPixelsAsList(), Grid.isDisabled);
                    await HubContext.Clients.Group(Grid.GroupName).SendAsync("TimeOuts", TimeOuts);
                    return Ok(newArt);
                }
                else
                {
                    throw new AuthenticationException("User is not logged in.");
                }
            }
            catch (AuthenticationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch(UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }
        [HttpPost]
        [Route("DisableGrid")]
        public async Task<IActionResult> DisableGrid()
        {
            try
            {
                if (Request.Cookies.TryGetValue("GoogleOAuth", out var userSubId))
                {
                    var artist = await LoginService.GetUserBySubId(userSubId);

                    if (artist == null)
                    {
                        throw new AuthenticationException("User does not have an account.");
                    }
                    else if (artist.IsAdmin != true)
                    {
                        throw new UnauthorizedAccessException("User does not have permission to save the grid");
                    }
                    ConnectionManager.DisableGrid();
                    var grid = ConnectionManager.GetGrid();
                    await HubContext.Clients.Group(grid.GroupName).SendAsync("DisableGrid");
                    return Ok();
                }
                else
                {
                    throw new AuthenticationException("User is not logged in.");
                }
            }
            catch (AuthenticationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }
        [HttpPost]
        [Route("EnableGrid")]
        public async Task<IActionResult> EnableGrid()
        {
            try
            {
                if (Request.Cookies.TryGetValue("GoogleOAuth", out var userSubId))
                {
                    var artist = await LoginService.GetUserBySubId(userSubId);

                    if (artist == null)
                    {
                        throw new AuthenticationException("User does not have an account.");
                    }
                    else if (artist.IsAdmin != true)
                    {
                        throw new UnauthorizedAccessException("User does not have permission to save the grid");
                    }
                    ConnectionManager.EnableGrid();

                    var grid = ConnectionManager.GetGrid();
                    await HubContext.Clients.Group(grid.GroupName).SendAsync("EnableGrid");
                    return Ok();
                }
                else
                {
                    throw new AuthenticationException("User is not logged in.");
                }
            }
            catch (AuthenticationException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }
    }
}
