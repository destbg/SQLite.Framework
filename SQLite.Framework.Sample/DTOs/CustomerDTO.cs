namespace SQLite.Framework.Sample.DTOs;

public class CustomerDTO
{
    public required int Id { get; init; }
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public string? Phone { get; init; }
    public int? Age { get; init; }
}
