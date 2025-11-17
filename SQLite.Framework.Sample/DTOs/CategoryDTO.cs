namespace SQLite.Framework.Sample.DTOs;

public class CategoryDTO
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
}
