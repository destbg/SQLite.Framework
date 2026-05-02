using SQLite.Framework.Extensions;
using SQLite.Framework.Maui.Models;

namespace SQLite.Framework.Maui.Data;

/// <summary>
/// Repository for projects. Demonstrates LINQ projections, joins, and
/// the framework's typed Add/Update/Remove API.
/// </summary>
public class ProjectRepository
{
    private readonly AppDatabase _db;
    private readonly TaskRepository _taskRepository;
    private readonly TagRepository _tagRepository;
    private readonly CategoryRepository _categoryRepository;

    public ProjectRepository(AppDatabase db, TaskRepository taskRepository, TagRepository tagRepository, CategoryRepository categoryRepository)
    {
        _db = db;
        _taskRepository = taskRepository;
        _tagRepository = tagRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<List<ProjectListItem>> ListAsync()
    {
        var rows = await (
            from p in _db.Projects
            join c in _db.Categories on p.CategoryId equals c.Id into cs
            from c in cs.DefaultIfEmpty()
            select new { Project = p, Category = c }
        ).ToListAsync();

        List<ProjectListItem> result = new(rows.Count);
        foreach (var row in rows)
        {
            List<Tag> tags = await _tagRepository.ListAsync(row.Project.Id);
            result.Add(new ProjectListItem
            {
                Project = row.Project,
                Category = row.Category,
                Tags = tags,
            });
        }
        return result;
    }

    public async Task<ProjectDetail?> GetAsync(int id)
    {
        Project? project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id);
        if (project is null)
        {
            return null;
        }

        Category? category = project.CategoryId == 0
            ? null
            : await _categoryRepository.GetAsync(project.CategoryId);
        List<ProjectTask> tasks = await _taskRepository.ListAsync(project.Id);
        List<Tag> tags = await _tagRepository.ListAsync(project.Id);

        return new ProjectDetail
        {
            Project = project,
            Category = category,
            Tasks = tasks,
            Tags = tags,
        };
    }

    public async Task<int> SaveItemAsync(Project item)
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

    public async Task<int> DeleteItemAsync(Project item)
    {
        int affected = await _db.Projects.RemoveAsync(item);
        return affected;
    }

    public async Task DropTableAsync()
    {
        await _db.Schema.DropTableAsync<Project>();
        await _taskRepository.DropTableAsync();
        await _tagRepository.DropTableAsync();
        _db.EnsureSchema();
    }
}
