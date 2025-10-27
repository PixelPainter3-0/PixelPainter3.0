using System.Drawing;
using System.Net.NetworkInformation;
using Microsoft.Data.SqlClient;
using MyTestVueApp.Server.Entities;
using Point = MyTestVueApp.Server.Entities.Point;
using Artspace = MyTestVueApp.Server.Entities.Artspace;

namespace MyTestVueApp.Server.Interfaces
{
    /// <summary>
    /// Interface defines the SQL service.
    /// </summary>
    public interface IMapAccessService
    {
        public Task<IEnumerable<Point>> GetAllPoints();
        public Task<Point> GetPointById(int id);
        public Task<IEnumerable<Point>> GetArtspacePoints(int id);
        public Task<IEnumerable<Artspace>> GetAllArtspaces();
        public Task<Artspace> GetArtspaceById(int id);
    }
}