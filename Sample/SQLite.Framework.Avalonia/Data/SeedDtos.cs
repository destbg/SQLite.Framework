using SQLite.Framework.Avalonia.Models;

namespace SQLite.Framework.Avalonia.Data;

public class SeedDataDto
{
    public required List<ProjectSeedDto> Projects { get; set; }
}

public class ProjectSeedDto
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public Category? Category { get; set; }
    public List<ProjectTask> Tasks { get; set; } = [];
    public List<Tag> Tags { get; set; } = [];
}
