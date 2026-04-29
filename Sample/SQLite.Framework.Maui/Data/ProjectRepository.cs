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

    public ProjectRepository(AppDatabase db, TaskRepository taskRepository, TagRepository tagRepository)
    {
        _db = db;
        _taskRepository = taskRepository;
        _tagRepository = tagRepository;
    }

    public async Task<List<Project>> ListAsync()
    {
        List<Project> projects = await _db.Projects.ToListAsync();

        foreach (Project project in projects)
        {
            project.Tags = await _tagRepository.ListAsync(project.Id);
            project.Tasks = await _taskRepository.ListAsync(project.Id);
        }

        return projects;
    }

    public async Task<Project?> GetAsync(int id)
    {
        Project? project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id);
        if (project == null)
        {
            return null;
        }

        project.Tags = await _tagRepository.ListAsync(project.Id);
        project.Tasks = await _taskRepository.ListAsync(project.Id);
        return project;
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
