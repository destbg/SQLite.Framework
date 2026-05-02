using SQLite.Framework.Avalonia.Models;
using SQLite.Framework.Extensions;

namespace SQLite.Framework.Avalonia.Data;

public class TagRepository
{
    private readonly AppDatabase _db;

    public TagRepository(AppDatabase db)
    {
        _db = db;
    }

    public Task<List<Tag>> ListAsync()
    {
        return _db.Tags.OrderBy(t => t.Title).ToListAsync();
    }

    public async Task<List<Tag>> ListForProjectAsync(int projectId)
    {
        List<int> tagIds = await _db.ProjectsTags
            .Where(pt => pt.ProjectId == projectId)
            .Select(pt => pt.TagId)
            .ToListAsync();

        if (tagIds.Count == 0)
        {
            return [];
        }

        return await _db.Tags
            .Where(t => tagIds.Contains(t.Id))
            .OrderBy(t => t.Title)
            .ToListAsync();
    }

    public async Task<int> SaveAsync(Tag item)
    {
        if (item.Id == 0)
        {
            await _db.Tags.AddAsync(item);
        }
        else
        {
            await _db.Tags.UpdateAsync(item);
        }

        return item.Id;
    }

    public Task<int> RemoveAsync(Tag item)
    {
        return _db.Tags.RemoveAsync(item);
    }

    public async Task LinkAsync(int projectId, int tagId)
    {
        bool exists = await _db.ProjectsTags
            .AnyAsync(pt => pt.ProjectId == projectId && pt.TagId == tagId);
        if (!exists)
        {
            await _db.ProjectsTags.AddAsync(new ProjectsTags { ProjectId = projectId, TagId = tagId });
        }
    }
}
