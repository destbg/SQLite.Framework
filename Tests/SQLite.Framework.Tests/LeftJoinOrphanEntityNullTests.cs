using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class LeftJoinOrphanEntityNullTests
{
    [Fact]
    public void LeftJoinProjectingWholeInnerEntityNullsUnmatchedLikeDotNet()
    {
        List<Author> authors =
        [
            new Author { Id = 1, Name = "Alice", Email = "a", BirthDate = default },
            new Author { Id = 2, Name = "Bob", Email = "b", BirthDate = default },
        ];
        List<Book> books =
        [
            new Book { Id = 10, Title = "Matched", AuthorId = 1, Price = 1 },
            new Book { Id = 11, Title = "Orphan", AuthorId = 99, Price = 2 },
        ];

        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();
        foreach (Author a in authors)
        {
            db.Table<Author>().Add(a);
        }

        foreach (Book b in books)
        {
            db.Table<Book>().Add(b);
        }

        List<bool> oracle = (from b in books
                join a in authors on b.AuthorId equals a.Id into g
                from a in g.DefaultIfEmpty()
                orderby b.Id
                select new { b.Title, Author = a })
            .ToList()
            .Select(x => x.Author == null)
            .ToList();

        List<bool> actual = (from b in db.Table<Book>()
                join a in db.Table<Author>() on b.AuthorId equals a.Id into g
                from a in g.DefaultIfEmpty()
                orderby b.Id
                select new { b.Title, Author = a })
            .ToList()
            .Select(x => x.Author == null)
            .ToList();

        Assert.Equal([false, true], oracle);
        Assert.Equal(oracle, actual);
    }
}
