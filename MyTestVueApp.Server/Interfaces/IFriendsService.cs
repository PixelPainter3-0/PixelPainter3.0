using MyTestVueApp.Server.Entities;

namespace MyTestVueApp.Server.Interfaces
{
    public interface IFriendsService
    {
        public Task<int> InsertFriends(Artist artist1, Artist artist2);
        public Task<int> RemoveFriends(Artist artist1, Artist artist2);

        public Task<IEnumerable<Friends>> GetArtistFriends(Artist artist);
    }
}
