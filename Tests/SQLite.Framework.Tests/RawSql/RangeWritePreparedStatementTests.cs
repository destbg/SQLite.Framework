using System.Runtime.CompilerServices;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public sealed class SeededRangeWritesDatabase : TestDatabase
{
    public SeededRangeWritesDatabase([CallerMemberName] string? methodName = null)
        : base(methodName)
    {
    }

    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        Table<Book>().Schema.CreateTable();
        Table<Book>().AddRange(
        [
            new Book { Id = 1, Title = "seed", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "temp", AuthorId = 1, Price = 2 },
        ]);
        Table<Book>().UpdateRange([new Book { Id = 1, Title = "seeded", AuthorId = 1, Price = 1 }]);
        Table<Book>().RemoveRange([new Book { Id = 2, Title = "temp", AuthorId = 1, Price = 2 }]);
        Table<Book>().AddOrUpdateRange([new Book { Id = 3, Title = "upserted", AuthorId = 1, Price = 3 }]);
    }
}

public class RangeWritePreparedStatementTests
{
    [Fact]
    public void UpdateRange_SameTableTwice_PersistsLatestValues()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange([new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 }]);

        int first = db.Table<Book>().UpdateRange([new Book { Id = 1, Title = "B", AuthorId = 2, Price = 2 }]);
        int second = db.Table<Book>().UpdateRange([new Book { Id = 1, Title = "C", AuthorId = 3, Price = 3 }]);

        Assert.Equal(1, first);
        Assert.Equal(1, second);
        Book row = db.Table<Book>().Single();
        Assert.Equal("C", row.Title);
        Assert.Equal(3, row.AuthorId);
        Assert.Equal(3, row.Price);
    }

    [Fact]
    public void RemoveRange_SameTableTwice_DeletesEachBatch()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(
        [
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
            new Book { Id = 2, Title = "B", AuthorId = 2, Price = 2 },
        ]);

        int first = db.Table<Book>().RemoveRange([new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 }]);
        int second = db.Table<Book>().RemoveRange([new Book { Id = 2, Title = "B", AuthorId = 2, Price = 2 }]);

        Assert.Equal(1, first);
        Assert.Equal(1, second);
        Assert.Equal(0, db.Table<Book>().Count());
    }

    [Fact]
    public void AddOrUpdateRange_SameKeyTwice_KeepsSingleRowWithLatestValues()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddOrUpdateRange([new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 }]);
        db.Table<Book>().AddOrUpdateRange([new Book { Id = 1, Title = "B", AuthorId = 2, Price = 2 }]);

        Book row = db.Table<Book>().Single();
        Assert.Equal("B", row.Title);
    }

    [Fact]
    public void AddOrUpdateRange_UnknownConflictValue_BehavesLikeReplace()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddOrUpdateRange([new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 }], conflict: (SQLiteConflict)99);
        db.Table<Book>().AddOrUpdateRange([new Book { Id = 1, Title = "B", AuthorId = 2, Price = 2 }], conflict: (SQLiteConflict)99);

        Book row = db.Table<Book>().Single();
        Assert.Equal("B", row.Title);
    }

    [Fact]
    public void RangeWrites_InsideOnModelCreating_WorkAndLaterWritesStillWork()
    {
        using SeededRangeWritesDatabase db = new();

        List<Book> rows = db.Table<Book>().OrderBy(b => b.Id).ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal("seeded", rows[0].Title);
        Assert.Equal("upserted", rows[1].Title);

        db.Table<Book>().AddRange([new Book { Id = 4, Title = "later", AuthorId = 4, Price = 4 }]);
        Assert.Equal(3, db.Table<Book>().Count());
    }
}
