using SQLite.Framework.Maui.Models;

namespace SQLite.Framework.Maui.Data;

public class SeedDataDto
{
    public required List<ProjectSeedDto> Projects { get; set; }
}

public class ProjectSeedDto
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Icon { get; set; }
    public Category? Category { get; set; }
    public List<ProjectTask> Tasks { get; set; } = [];
    public List<Tag> Tags { get; set; } = [];
}
