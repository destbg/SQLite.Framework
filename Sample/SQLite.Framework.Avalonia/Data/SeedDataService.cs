using System.Text.Json;
using Avalonia.Platform;
using SQLite.Framework.Avalonia.Models;
using SQLite.Framework.Extensions;

namespace SQLite.Framework.Avalonia.Data;

/// <summary>
/// Loads bundled seed data from <c>Resources/SeedData.json</c> the first time the app
/// runs. Wraps every insert in a single transaction so the work commits or rolls back
/// as one unit.
/// </summary>
public class SeedDataService
{
    private static readonly Uri SeedUri = new("avares://SQLite.Framework.Avalonia/Resources/SeedData.json");

    private readonly AppDatabase _db;
    private readonly ProjectRepository _projects;
    private readonly TaskRepository _tasks;
    private readonly TagRepository _tags;
    private readonly CategoryRepository _categories;

    public SeedDataService(
        AppDatabase db,
        ProjectRepository projects,
        TaskRepository tasks,
        TagRepository tags,
        CategoryRepository categories)
    {
        _db = db;
        _projects = projects;
        _tasks = tasks;
        _tags = tags;
        _categories = categories;
    }

    public async Task SeedIfEmptyAsync()
    {
        int existingProjects = await _db.Projects.CountAsync();
        if (existingProjects > 0)
        {
            return;
        }

        await using Stream stream = AssetLoader.Open(SeedUri);
        SeedDataDto? payload = JsonSerializer.Deserialize(stream, JsonContext.Default.SeedDataDto);
        if (payload is null)
        {
            return;
        }

        await using SQLiteTransaction tx = await _db.BeginTransactionAsync();

        foreach (ProjectSeedDto seed in payload.Projects)
        {
            int categoryId = 0;
            if (seed.Category is not null)
            {
                await _categories.SaveAsync(seed.Category);
                categoryId = seed.Category.Id;
            }

            Project project = new()
            {
                Name = seed.Name,
                Description = seed.Description,
                CategoryId = categoryId,
            };
            await _projects.SaveAsync(project);

            foreach (ProjectTask task in seed.Tasks)
            {
                task.ProjectId = project.Id;
                await _tasks.SaveAsync(task);
            }

            foreach (Tag tag in seed.Tags)
            {
                await _tags.SaveAsync(tag);
                await _tags.LinkAsync(project.Id, tag.Id);
            }
        }

        await tx.CommitAsync();
    }
}
