using SQLite.Framework.Avalonia.Models;
using SQLite.Framework.Extensions;

namespace SQLite.Framework.Avalonia.Data;

public class ProjectRepository
{
    private readonly AppDatabase _db;

    public ProjectRepository(AppDatabase db)
    {
        _db = db;
    }

    public Task<List<Project>> ListAsync()
    {
        return _db.Projects.OrderBy(p => p.Name).ToListAsync();
    }

    public async Task<List<ProjectListItem>> ListWithCategoriesAsync()
    {
        var rows = await (
            from p in _db.Projects
            join c in _db.Categories on p.CategoryId equals c.Id into cs
            from c in cs.DefaultIfEmpty()
            orderby p.Name
            select new { Project = p, Category = c }
        ).ToListAsync();

        List<ProjectListItem> result = new(rows.Count);
        foreach (var row in rows)
        {
            result.Add(new ProjectListItem { Project = row.Project, Category = row.Category });
        }
        return result;
    }

    public Task<Project?> GetAsync(int id)
    {
        return _db.Projects.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<int> SaveAsync(Project item)
    {
        if (item.Id == 0)
        {
            await _db.Projects.AddAsync(item);
        }
        else
        {
            await _db.Projects.UpdateAsync(item);
        }

        return item.Id;
    }

    public Task<int> RemoveAsync(Project item)
    {
        return _db.Projects.RemoveAsync(item);
    }
}
