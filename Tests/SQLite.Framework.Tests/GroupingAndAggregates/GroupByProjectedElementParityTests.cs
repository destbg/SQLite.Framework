using System;
using System.Collections.Generic;
using System.Linq;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class GroupByProjectedElementParityTests
{
    private static TestDatabase Seed(out List<Author> authors, out List<Book> books)
    {
        authors = new List<Author>
        {
            new() { Id = 1, Name = "Alice", Email = "a@x", BirthDate = new DateTime(1980, 1, 1) },
            new() { Id = 2, Name = "Bob", Email = "b@x", BirthDate = new DateTime(1990, 1, 1) },
        };
        books = new List<Book>
        {
            new() { Id = 1, AuthorId = 1, Title = "t1", Price = 10 },
            new() { Id = 2, AuthorId = 1, Title = "t2", Price = 20 },
            new() { Id = 3, AuthorId = 2, Title = "t3", Price = 30 },
        };
        TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().AddRange(authors);
        db.Table<Book>().AddRange(books);
        return db;
    }

    [Fact]
    public void JoinProjectionThenGroupByMemberKey()
    {
        using TestDatabase db = Seed(out var authors, out var books);

        var oracle = (from b in books
                join a in authors on b.AuthorId equals a.Id
                select new { a.Name, b.Price })
            .GroupBy(x => x.Name)
            .Select(g => new { Name = g.Key, Total = g.Sum(x => x.Price) })
            .OrderBy(x => x.Name, StringComparer.Ordinal)
            .ToList();

        var actual = (from b in db.Table<Book>()
                join a in db.Table<Author>() on b.AuthorId equals a.Id
                select new { a.Name, b.Price })
            .GroupBy(x => x.Name)
            .Select(g => new { Name = g.Key, Total = g.Sum(x => x.Price) })
            .ToList()
            .OrderBy(x => x.Name, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ProjectionAnonymousThenGroupByMemberKey()
    {
        using TestDatabase db = Seed(out _, out var books);

        var oracle = books
            .Select(b => new { b.AuthorId, b.Price })
            .GroupBy(x => x.AuthorId)
            .Select(g => new { Key = g.Key, Total = g.Sum(x => x.Price), Count = g.Count() })
            .OrderBy(x => x.Key)
            .ToList();

        var actual = db.Table<Book>()
            .Select(b => new { b.AuthorId, b.Price })
            .GroupBy(x => x.AuthorId)
            .Select(g => new { Key = g.Key, Total = g.Sum(x => x.Price), Count = g.Count() })
            .ToList()
            .OrderBy(x => x.Key)
            .ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void JoinProjectionThenGroupByComputedKey()
    {
        using TestDatabase db = Seed(out var authors, out var books);

        var oracle = (from b in books
                join a in authors on b.AuthorId equals a.Id
                select new { a.Name, b.Price })
            .GroupBy(x => x.Price > 15)
            .Select(g => new { Expensive = g.Key, Total = g.Sum(x => x.Price) })
            .OrderBy(x => x.Expensive)
            .ToList();

        var actual = (from b in db.Table<Book>()
                join a in db.Table<Author>() on b.AuthorId equals a.Id
                select new { a.Name, b.Price })
            .GroupBy(x => x.Price > 15)
            .Select(g => new { Expensive = g.Key, Total = g.Sum(x => x.Price) })
            .ToList()
            .OrderBy(x => x.Expensive)
            .ToList();

        Assert.Equal(oracle, actual);
    }
}
