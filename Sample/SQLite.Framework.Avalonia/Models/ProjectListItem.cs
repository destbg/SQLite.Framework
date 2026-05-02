namespace SQLite.Framework.Avalonia.Models;

public class ProjectListItem
{
    public required Project Project { get; init; }
    public Category? Category { get; init; }
}
