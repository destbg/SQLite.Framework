using System.Collections.Generic;
using System.Linq;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class GroupByAnonymousKeyProjectionParityTests
{
    private static TestDatabase Seed(out List<Book> books)
    {
        books =
        [
            new Book { Id = 1, Title = "a", AuthorId = 10, Price = 5.0 },
            new Book { Id = 2, Title = "b", AuthorId = 10, Price = 15.0 },
            new Book { Id = 3, Title = "c", AuthorId = 20, Price = 7.0 },
        ];
        TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(books);
        return db;
    }

    [Fact]
    public void ProjectionThenAnonymousKeyThenKeyMember_MatchesLinqToObjects()
    {
        using TestDatabase db = Seed(out List<Book> books);

        var oracle = books.Select(b => new { b.AuthorId, b.Title })
            .GroupBy(x => new { x.AuthorId })
            .Select(g => new { g.Key.AuthorId, C = g.Count() })
            .OrderBy(x => x.AuthorId).ToList();
        var actual = db.Table<Book>().Select(b => new { b.AuthorId, b.Title })
            .GroupBy(x => new { x.AuthorId })
            .Select(g => new { g.Key.AuthorId, C = g.Count() })
            .ToList()
            .OrderBy(x => x.AuthorId).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ProjectionThenAnonymousKeyThenSum_MatchesLinqToObjects()
    {
        using TestDatabase db = Seed(out List<Book> books);

        var oracle = books.Select(b => new { b.AuthorId, b.Price })
            .GroupBy(x => new { x.AuthorId })
            .Select(g => new { g.Key.AuthorId, Total = g.Sum(x => x.Price) })
            .OrderBy(x => x.AuthorId).ToList();
        var actual = db.Table<Book>().Select(b => new { b.AuthorId, b.Price })
            .GroupBy(x => new { x.AuthorId })
            .Select(g => new { g.Key.AuthorId, Total = g.Sum(x => x.Price) })
            .ToList()
            .OrderBy(x => x.AuthorId).ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ProjectionThenTwoMemberAnonymousKey_MatchesLinqToObjects()
    {
        using TestDatabase db = Seed(out List<Book> books);

        var oracle = books.Select(b => new { b.AuthorId, b.Title, b.Price })
            .GroupBy(x => new { x.AuthorId, x.Title })
            .Select(g => new { g.Key.AuthorId, g.Key.Title, C = g.Count() })
            .OrderBy(x => x.AuthorId).ThenBy(x => x.Title).ToList();
        var actual = db.Table<Book>().Select(b => new { b.AuthorId, b.Title, b.Price })
            .GroupBy(x => new { x.AuthorId, x.Title })
            .Select(g => new { g.Key.AuthorId, g.Key.Title, C = g.Count() })
            .ToList()
            .OrderBy(x => x.AuthorId).ThenBy(x => x.Title).ToList();

        Assert.Equal(oracle, actual);
    }
}
