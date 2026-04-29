using SQLite.Framework.Extensions;
using SQLite.Framework.Maui.Models;

namespace SQLite.Framework.Maui.Data;

/// <summary>
/// Repository for project tasks. Pure LINQ over <see cref="AppDatabase.Tasks" />.
/// </summary>
public class TaskRepository
{
    private readonly AppDatabase _db;

    public TaskRepository(AppDatabase db)
    {
        _db = db;
    }

    public Task<List<ProjectTask>> ListAsync() => _db.Tasks.ToListAsync();

    public Task<List<ProjectTask>> ListAsync(int projectId)
    {
        return _db.Tasks.Where(t => t.ProjectId == projectId).ToListAsync();
    }

    public Task<ProjectTask?> GetAsync(int id)
    {
        return _db.Tasks.FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<int> SaveItemAsync(ProjectTask item)
    {
        if (item.Id == 0)
        {
            await _db.Tasks.AddAsync(item);
        }
        else
        {
            await _db.Tasks.UpdateAsync(item);
        }

        return item.Id;
    }

    public async Task<int> DeleteItemAsync(ProjectTask item)
    {
        int affected = await _db.Tasks.RemoveAsync(item);
        return affected;
    }

    public async Task DropTableAsync()
    {
        await _db.Schema.DropTableAsync<ProjectTask>();
        await _db.Schema.CreateTableAsync<ProjectTask>();
    }
}