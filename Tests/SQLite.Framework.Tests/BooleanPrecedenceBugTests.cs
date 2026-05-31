using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class BooleanPrecedenceBugTests
{
    [Fact]
    public void OrGroupAndedWithAnotherConditionRespectsPrecedence()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });

        List<Book> rows = db.Table<Book>()
            .Where(b => (b.AuthorId == 1 || b.AuthorId == 2) && b.Price > 10)
            .ToList();

        Assert.Empty(rows);
    }

    [Fact]
    public void ChainedWhereWithOrPredicateAndsCorrectly()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1 });

        List<Book> rows = db.Table<Book>()
            .Where(b => b.Id == 1 || b.Id == 2)
            .Where(b => b.Price > 10)
            .ToList();

        Assert.Empty(rows);
    }

    [Fact]
    public void NotOverCompoundConditionScopesCorrectly()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 5 });

        List<int> ids = db.Table<Book>()
            .Where(b => !(b.Id == 1 && b.Price > 10))
            .Select(b => b.Id)
            .ToList();

        Assert.Equal(new[] { 1 }, ids);
    }
}
