using System.Drawing;
using Microsoft.Data.SqlClient;
using MyTestVueApp.Server.Entities;

namespace MyTestVueApp.Server.Interfaces
{
    /// <summary>
    /// Interface defines the SQL service.
    /// </summary>
    public interface IMapAccessService
    {
        public Task<IEnumerable<Point>> GetAllPoints();
        public Task<Point> GetPointById(int id);
    }
}