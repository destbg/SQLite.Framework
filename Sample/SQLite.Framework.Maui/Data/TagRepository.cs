using SQLite.Framework.Extensions;
using SQLite.Framework.Maui.Models;

namespace SQLite.Framework.Maui.Data;

/// <summary>
/// Repository for tags. Demonstrates many-to-many handling via the
/// <see cref="ProjectsTags" /> join table.
/// </summary>
public class TagRepository
{
    private readonly AppDatabase _db;

    public TagRepository(AppDatabase db)
    {
        _db = db;
    }

    public Task<List<Tag>> ListAsync()
    {
        return _db.Tags.ToListAsync();
    }

    public Task<List<Tag>> ListAsync(int projectID)
    {
        return (
            from t in _db.Tags
            join pt in _db.ProjectsTags on t.Id equals pt.TagId
            where pt.ProjectId == projectID
            select t
        ).ToListAsync();
    }

    public Task<Tag?> GetAsync(int id)
    {
        return _db.Tags.FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<int> SaveItemAsync(Tag item)
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

    public async Task<int> SaveItemAsync(Tag item, int projectID)
    {
        await SaveItemAsync(item);

        bool alreadyAssociated = await _db.ProjectsTags
            .AnyAsync(pt => pt.ProjectId == projectID && pt.TagId == item.Id);
        if (alreadyAssociated)
        {
            return 0;
        }

        return await _db.ProjectsTags.AddAsync(new ProjectsTags
        {
            ProjectId = projectID,
            TagId = item.Id
        });
    }

    public async Task<int> DeleteItemAsync(Tag item)
    {
        int affected = await _db.Tags.RemoveAsync(item);
        return affected;
    }

    public async Task<int> DeleteItemAsync(Tag item, int projectID)
    {
        ProjectsTags? link = await _db.ProjectsTags
            .FirstOrDefaultAsync(pt => pt.ProjectId == projectID && pt.TagId == item.Id);
        if (link == null)
        {
            return 0;
        }

        return await _db.ProjectsTags.RemoveAsync(link);
    }

    public async Task DropTableAsync()
    {
        await _db.Schema.DropTableAsync<Tag>();
        await _db.Schema.DropTableAsync<ProjectsTags>();
        await _db.Schema.CreateTableAsync<Tag>();
        await _db.Schema.CreateTableAsync<ProjectsTags>();
    }
}