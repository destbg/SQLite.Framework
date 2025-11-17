namespace SQLite.Framework.Sample.DTOs;

public class ProductDTO
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required decimal Price { get; init; }
    public required CategoryDTO Category { get; init; }
    public required int Stock { get; init; }
    public required bool IsActive { get; init; }
}
