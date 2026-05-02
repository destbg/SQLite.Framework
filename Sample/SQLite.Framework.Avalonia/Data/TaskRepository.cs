using SQLite.Framework.Avalonia.Models;
using SQLite.Framework.Extensions;

namespace SQLite.Framework.Avalonia.Data;

public class TaskRepository
{
    private readonly AppDatabase _db;

    public TaskRepository(AppDatabase db)
    {
        _db = db;
    }

    public Task<List<ProjectTask>> ListForProjectAsync(int projectId)
    {
        return _db.Tasks
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.IsCompleted)
            .ThenBy(t => t.Title)
            .ToListAsync();
    }

    public async Task<int> SaveAsync(ProjectTask item)
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

    public Task<int> RemoveAsync(ProjectTask item)
    {
        return _db.Tasks.RemoveAsync(item);
    }

    public Task<int> RemoveByProjectAsync(int projectId)
    {
        return _db.Tasks.Where(t => t.ProjectId == projectId).ExecuteDeleteAsync();
    }
}
