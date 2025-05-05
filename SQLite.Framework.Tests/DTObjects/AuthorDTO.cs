namespace SQLite.Framework.Tests.DTObjects;

public class AuthorDTO
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required DateTime BirthDate { get; init; }
}