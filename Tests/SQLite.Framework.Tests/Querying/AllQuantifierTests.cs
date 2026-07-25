using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class AllQuantifierTests
{
    [Fact]
    public void AllAfterWhereOnlyConsidersFilteredRows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 5 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 2, Price = 50 });

        bool all = db.Table<Book>()
            .Where(b => b.AuthorId == 1)
            .All(b => b.Price < 10);

        Assert.True(all);
    }

    [Fact]
    public void SubqueryAllReturnsTrueWhenAllRowsSatisfyPredicate()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 5 });

        List<Book> rows = db.Table<Book>()
            .Where(outer => db.Table<Book>().All(inner => inner.Price > 0))
            .ToList();

        Assert.Single(rows);
    }

    [Fact]
    public void SubqueryAllReturnsFalseWhenSomeRowViolatesPredicate()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 5 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 2, Price = -1 });

        List<Book> rows = db.Table<Book>()
            .Where(outer => outer.Id == 1 && db.Table<Book>().All(inner => inner.Price > 0))
            .ToList();

        Assert.Empty(rows);
    }
}
