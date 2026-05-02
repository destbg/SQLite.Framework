namespace SQLite.Framework.Maui.Models;

public class ProjectDetail
{
    public required Project Project { get; init; }
    public Category? Category { get; set; }
    public required List<ProjectTask> Tasks { get; set; }
    public required List<Tag> Tags { get; set; }
}
