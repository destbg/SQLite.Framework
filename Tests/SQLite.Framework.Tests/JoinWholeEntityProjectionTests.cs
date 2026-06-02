using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class JoinWholeEntityProjectionTests
{
    private static List<Author> SeedAuthors =>
    [
        new Author { Id = 1, Name = "Alice", Email = "a", BirthDate = default },
        new Author { Id = 2, Name = "Bob", Email = "b", BirthDate = default }
    ];

    private static List<Book> SeedBooks =>
    [
        new Book { Id = 10, Title = "B1", AuthorId = 1, Price = 1 },
        new Book { Id = 11, Title = "B2", AuthorId = 1, Price = 2 },
        new Book { Id = 12, Title = "B3", AuthorId = 2, Price = 3 }
    ];

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        foreach (Author a in SeedAuthors)
        {
            db.Table<Author>().Add(a);
        }

        foreach (Book b in SeedBooks)
        {
            db.Table<Book>().Add(b);
        }

        return db;
    }

    [Fact]
    public void WholeEntitySelectAfterJoinWithPrefixCollidingSiblingMatchesInMemory()
    {
        using TestDatabase db = Seed();

        List<int> expected = SeedAuthors
            .Join(SeedBooks, a => a.Id, b => b.AuthorId, (a, b) => new PrefixCollideDto { Author = a, AuthorBook = b })
            .Select(t => t.Author)
            .Distinct()
            .Select(a => a.Id)
            .OrderBy(id => id)
            .ToList();

        List<int> actual = db.Table<Author>()
            .Join(db.Table<Book>(), a => a.Id, b => b.AuthorId, (a, b) => new PrefixCollideDto { Author = a, AuthorBook = b })
            .Select(t => t.Author)
            .Distinct()
            .ToList()
            .Select(a => a.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
        Assert.Equal(new[] { 1, 2 }, actual);
    }

    [Fact]
    public void WholeEntitySelectAfterJoinWithNonPrefixSiblingMatchesInMemory()
    {
        using TestDatabase db = Seed();

        List<int> expected = SeedAuthors
            .Join(SeedBooks, a => a.Id, b => b.AuthorId, (a, b) => new NonPrefixDto { Author = a, TheBook = b })
            .Select(t => t.Author)
            .Distinct()
            .Select(a => a.Id)
            .OrderBy(id => id)
            .ToList();

        List<int> actual = db.Table<Author>()
            .Join(db.Table<Book>(), a => a.Id, b => b.AuthorId, (a, b) => new NonPrefixDto { Author = a, TheBook = b })
            .Select(t => t.Author)
            .Distinct()
            .ToList()
            .Select(a => a.Id)
            .OrderBy(id => id)
            .ToList();

        Assert.Equal(expected, actual);
        Assert.Equal(new[] { 1, 2 }, actual);
    }
}

file class PrefixCollideDto
{
    public required Author Author { get; set; }
    public required Book AuthorBook { get; set; }
}

file class NonPrefixDto
{
    public required Author Author { get; set; }
    public required Book TheBook { get; set; }
}
