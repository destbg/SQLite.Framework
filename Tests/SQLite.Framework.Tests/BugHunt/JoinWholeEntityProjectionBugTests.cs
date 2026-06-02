using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

public class JoinWholeEntityProjectionBugTests
{
    [Fact]
    public void WholeEntitySelectAfterJoin_PrefixCollidingSibling_DistinctCollapses()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        List<Author> authors =
        [
            new Author { Id = 1, Name = "Alice", Email = "a", BirthDate = default },
            new Author { Id = 2, Name = "Bob", Email = "b", BirthDate = default }
        ];
        List<Book> books =
        [
            new Book { Id = 10, Title = "B1", AuthorId = 1, Price = 1 },
            new Book { Id = 11, Title = "B2", AuthorId = 1, Price = 2 },
            new Book { Id = 12, Title = "B3", AuthorId = 2, Price = 3 }
        ];

        foreach (Author a in authors)
        {
            db.Table<Author>().Add(a);
        }

        foreach (Book b in books)
        {
            db.Table<Book>().Add(b);
        }

        int expected = authors
            .Join(books, a => a.Id, b => b.AuthorId, (a, b) => new PrefixCollideDto { Author = a, AuthorBook = b })
            .Select(t => new { t.Author.Id, t.Author.Name, t.Author.Email, t.Author.BirthDate })
            .Distinct()
            .ToList()
            .Count;

        int actual = db.Table<Author>()
            .Join(db.Table<Book>(), a => a.Id, b => b.AuthorId, (a, b) => new PrefixCollideDto { Author = a, AuthorBook = b })
            .Select(t => t.Author)
            .Distinct()
            .ToList()
            .Count;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WholeEntitySelectAfterJoin_NonPrefixSibling_DistinctCollapses()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        List<Author> authors =
        [
            new Author { Id = 1, Name = "Alice", Email = "a", BirthDate = default },
            new Author { Id = 2, Name = "Bob", Email = "b", BirthDate = default }
        ];
        List<Book> books =
        [
            new Book { Id = 10, Title = "B1", AuthorId = 1, Price = 1 },
            new Book { Id = 11, Title = "B2", AuthorId = 1, Price = 2 },
            new Book { Id = 12, Title = "B3", AuthorId = 2, Price = 3 }
        ];

        foreach (Author a in authors)
        {
            db.Table<Author>().Add(a);
        }

        foreach (Book b in books)
        {
            db.Table<Book>().Add(b);
        }

        int expected = authors
            .Join(books, a => a.Id, b => b.AuthorId, (a, b) => new NonPrefixDto { Author = a, TheBook = b })
            .Select(t => new { t.Author.Id, t.Author.Name, t.Author.Email, t.Author.BirthDate })
            .Distinct()
            .ToList()
            .Count;

        int actual = db.Table<Author>()
            .Join(db.Table<Book>(), a => a.Id, b => b.AuthorId, (a, b) => new NonPrefixDto { Author = a, TheBook = b })
            .Select(t => t.Author)
            .Distinct()
            .ToList()
            .Count;

        Assert.Equal(expected, actual);
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
