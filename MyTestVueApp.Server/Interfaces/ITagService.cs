using MyTestVueApp.Server.Entities;

namespace MyTestVueApp.Server.Interfaces
{
    public interface ITagService
    {
        Task<IEnumerable<Tag>> GetAllTags();
        Task<Tag> CreateTag(Tag tag);
        Task<bool> AssignTagsToArt(int artId, List<int> tagIds);
        Task<IEnumerable<Tag>> GetTagsForArt(int artId);
        Task<bool> RemoveTagFromArt(int artId, int tagId, Artist artist);
    }
}
