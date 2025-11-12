using MyTestVueApp.Server.Entities;

namespace MyTestVueApp.Server.Interfaces
{
    public interface IArtistService
    {
        Task<Artist?> GetArtistById(int artistId);
        Task<IEnumerable<Artist>> GetAllArtists();
        Task<bool> UpdateArtistNotificationsEnabled(int artistId, int notificationsEnabled);
    }
}

