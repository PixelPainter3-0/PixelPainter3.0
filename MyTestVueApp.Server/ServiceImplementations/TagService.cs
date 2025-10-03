using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using MyTestVueApp.Server.Configuration;
using MyTestVueApp.Server.Entities;
using MyTestVueApp.Server.Interfaces;
using Microsoft.Extensions.Logging;

namespace MyTestVueApp.Server.ServiceImplementations
{
    public class TagService : ITagService
    {
        private readonly IOptions<ApplicationConfiguration> _appConfig;
        private readonly ILogger<TagService> _logger;

        public TagService(IOptions<ApplicationConfiguration> appConfig, ILogger<TagService> logger)
        {
            _appConfig = appConfig;
            _logger = logger;
        }

        public async Task<IEnumerable<Tag>> GetAllTags()
        {
            var tags = new List<Tag>();
            using (var conn = new SqlConnection(_appConfig.Value.ConnectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand("SELECT Id, Name, CreationDate FROM Tag", conn);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        tags.Add(new Tag
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            CreationDate = reader.GetDateTime(2)
                        });
                    }
                }
            }
            return tags;
        }

        public async Task<Tag> CreateTag(Tag tag)
        {
            using (var conn = new SqlConnection(_appConfig.Value.ConnectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand(
                    "INSERT INTO Tag (Name, CreationDate) VALUES (@Name, @CreationDate)", conn);
                cmd.Parameters.AddWithValue("@Name", tag.Name);
                cmd.Parameters.AddWithValue("@CreationDate", tag.CreationDate == default ? DateTime.UtcNow : tag.CreationDate);

                var tagID = await cmd.ExecuteScalarAsync();
                tag.Id = Convert.ToInt32(tagID);

                return tag;
            }
        }

        public async Task<bool> AssignTagsToArt(int artId, List<int> tagIds)
        {
            using (var conn = new SqlConnection(_appConfig.Value.ConnectionString))
            {
                await conn.OpenAsync();

                // Check if artId exists
                var checkArtCmd = new SqlCommand("SELECT COUNT(1) FROM Art WHERE Id = @ArtId", conn);
                checkArtCmd.Parameters.AddWithValue("@ArtId", artId);
                var exists = (int)await checkArtCmd.ExecuteScalarAsync() > 0;
                if (!exists)
                {
                    throw new ArgumentException($"Art with Id {artId} does not exist.");
                }

                using (var tran = conn.BeginTransaction())
                {
                    try
                    {
                        // Remove existing tags for this art
                        var deleteCmd = new SqlCommand(
                            "DELETE FROM ArtTags WHERE ArtId = @ArtId", conn, tran);
                        deleteCmd.Parameters.AddWithValue("@ArtId", artId);
                        await deleteCmd.ExecuteNonQueryAsync();

                        // Insert new tags
                        foreach (var tagId in tagIds.Distinct())
                        {
                            var insertCmd = new SqlCommand(
                                "INSERT INTO ArtTags (ArtId, TagId, CreationDate) VALUES (@ArtId, @TagId, @CreationDate)", conn, tran);
                            insertCmd.Parameters.AddWithValue("@ArtId", artId);
                            insertCmd.Parameters.AddWithValue("@TagId", tagId);
                            insertCmd.Parameters.AddWithValue("@CreationDate", DateTime.UtcNow);
                            await insertCmd.ExecuteNonQueryAsync();
                        }

                        tran.Commit();
                        return true;
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<IEnumerable<Tag>> GetTagsForArt(int artId)
        {
            var tags = new List<Tag>();
            using (var conn = new SqlConnection(_appConfig.Value.ConnectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand(
                    @"SELECT t.Id, t.Name, t.CreationDate
                      FROM Tag t
                      INNER JOIN ArtTags at ON t.Id = at.TagId
                      WHERE at.ArtId = @ArtId", conn);
                cmd.Parameters.AddWithValue("@ArtId", artId);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        tags.Add(new Tag
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            CreationDate = reader.GetDateTime(2)
                        });
                    }
                }
            }
            return tags;
        }

        public async Task<bool> RemoveTagFromArt(int artId, int tagId, Artist artist)
        {
            using (var conn = new SqlConnection(_appConfig.Value.ConnectionString))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand(
                    "DELETE FROM ArtTags WHERE ArtId = @ArtId AND TagId = @TagId", conn);
                cmd.Parameters.AddWithValue("@ArtId", artId);
                cmd.Parameters.AddWithValue("@TagId", tagId);

                var rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0;
            }
        }
    }
}