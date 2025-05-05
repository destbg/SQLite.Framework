namespace SQLite.Framework.Tests.DTObjects;

public class BookDTO
{
    public required int Id { get; init; }
    public required string Title { get; init; }
    public required AuthorDTO Author { get; init; }
}