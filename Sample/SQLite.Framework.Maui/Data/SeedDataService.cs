using System.Text.Json;
using Microsoft.Extensions.Logging;
using SQLite.Framework.Extensions;
using SQLite.Framework.Maui.Models;

namespace SQLite.Framework.Maui.Data;

public class SeedDataService
{
    private readonly AppDatabase _db;
    private readonly ProjectRepository _projectRepository;
    private readonly TaskRepository _taskRepository;
    private readonly TagRepository _tagRepository;
    private readonly CategoryRepository _categoryRepository;
    private readonly string _seedDataFilePath = "SeedData.json";
    private readonly ILogger<SeedDataService> _logger;

    public SeedDataService(AppDatabase db, ProjectRepository projectRepository, TaskRepository taskRepository, TagRepository tagRepository, CategoryRepository categoryRepository, ILogger<SeedDataService> logger)
    {
        _db = db;
        _projectRepository = projectRepository;
        _taskRepository = taskRepository;
        _tagRepository = tagRepository;
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    public async Task LoadSeedDataAsync()
    {
        await ClearTablesAsync();

        await using Stream templateStream = await FileSystem.OpenAppPackageFileAsync(_seedDataFilePath);

        SeedDataDto? payload = null;
        try
        {
            payload = JsonSerializer.Deserialize(templateStream, JsonContext.Default.SeedDataDto);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error deserializing seed data");
        }

        if (payload is null)
        {
            return;
        }

        try
        {
            await using SQLiteTransaction tx = await _db.BeginTransactionAsync();

            foreach (ProjectSeedDto seed in payload.Projects)
            {
                int categoryId = 0;
                if (seed.Category is not null)
                {
                    await _categoryRepository.SaveItemAsync(seed.Category);
                    categoryId = seed.Category.Id;
                }

                Project project = new()
                {
                    Name = seed.Name,
                    Description = seed.Description,
                    Icon = seed.Icon,
                    CategoryId = categoryId,
                };
                await _projectRepository.SaveItemAsync(project);

                foreach (ProjectTask task in seed.Tasks)
                {
                    task.ProjectId = project.Id;
                    await _taskRepository.SaveItemAsync(task);
                }

                foreach (Tag tag in seed.Tags)
                {
                    await _tagRepository.SaveItemAsync(tag, project.Id);
                }
            }

            await tx.CommitAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error saving seed data");
            throw;
        }
    }

    private async Task ClearTablesAsync()
    {
        try
        {
            await _db.Projects.ClearAsync();
            await _db.Tasks.ClearAsync();
            await _db.Tags.ClearAsync();
            await _db.ProjectsTags.ClearAsync();
            await _db.Categories.ClearAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error clearing tables");
        }
    }
}
