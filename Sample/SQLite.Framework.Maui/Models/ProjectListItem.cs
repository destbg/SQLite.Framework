namespace SQLite.Framework.Maui.Models;

public class ProjectListItem
{
    public required Project Project { get; init; }
    public Category? Category { get; init; }
    public required List<Tag> Tags { get; init; }

    public string AccessibilityDescription => $"{Project.Name} Project. {Project.Description}";
}
