using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ReindexTests
{
    [Fact]
    public void Reindex_NoArgument_RebuildsAllIndexes()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        db.Reindex();

        Book row = db.Table<Book>().First();
        Assert.Equal("x", row.Title);
    }

    [Fact]
    public void Reindex_TableName_RebuildsIndexesForTable()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        db.Reindex("Books");

        Book row = db.Table<Book>().First();
        Assert.Equal("x", row.Title);
    }

    [Fact]
    public void Reindex_CollationName_Runs()
    {
        using TestDatabase db = new();
        db.Reindex("NOCASE");
    }

    [Fact]
    public void Reindex_InvalidName_Throws()
    {
        using TestDatabase db = new();
        Assert.Throws<ArgumentException>(() => db.Reindex("bad name"));
    }

    [Fact]
    public async Task ReindexAsync_RunsOnLockedConnection()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        await db.ReindexAsync();
    }

    [Fact]
    public async Task ReindexAsync_WithName_Runs()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        await db.ReindexAsync("Books");
    }
}
