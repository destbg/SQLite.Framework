using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class QueryTranslationTests
{
    [Fact]
    public void MathLogIsNaturalLog()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 10 });

        double value = db.Table<Book>().Where(b => b.Id == 1).Select(b => Math.Log(b.Price)).First();

        Assert.Equal(Math.Log(10), value, 5);
    }

    [Fact]
    public void AddMonthsWithNegativeArgument()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        DateTime birth = new(2000, 6, 15);
        db.Table<Author>().Add(new Author { Id = 1, Name = "a", Email = "e", BirthDate = birth });

        DateTime result = db.Table<Author>().Where(a => a.Id == 1).Select(a => a.BirthDate.AddMonths(-3)).First();

        Assert.Equal(birth.AddMonths(-3), result);
    }

    [Fact]
    public void CompositeKeyGroupByAggregatesElementColumn()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 5 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 1, Price = 1 });

        var rows = (
            from book in db.Table<Book>()
            group book by new { book.AuthorId, Cheap = book.Price < 3.0 } into g
            select new { g.Key.AuthorId, g.Key.Cheap, Total = g.Sum(x => x.Price) }
        ).ToList();

        var notCheap = rows.Single(r => !r.Cheap);
        Assert.Equal(5.0, notCheap.Total);
    }

    [Fact]
    public void RightJoinKeepsUnmatchedRightRows()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Add(new Author { Id = 1, Name = "Alice", Email = "a", BirthDate = new DateTime(1980, 1, 1) });
        db.Table<Author>().Add(new Author { Id = 2, Name = "Bob", Email = "b", BirthDate = new DateTime(1980, 1, 1) });
        db.Table<Book>().Add(new Book { Id = 1, Title = "T", AuthorId = 1, Price = 1 });

        List<int> ids = db.Table<Book>()
            .RightJoin(db.Table<Author>(), b => b.AuthorId, a => a.Id, (b, a) => a.Id)
            .OrderBy(x => x)
            .ToList();

        Assert.Equal(new[] { 1, 2 }, ids);
    }
}
