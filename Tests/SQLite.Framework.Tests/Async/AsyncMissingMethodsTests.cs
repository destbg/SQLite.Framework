using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class AsyncMissingMethodsTests
{
    private static TestDatabase NewDb()
    {
        TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "B", AuthorId = 2, Price = 30 },
            new Book { Id = 3, Title = "C", AuthorId = 1, Price = 20 },
        });
        return db;
    }

    [Fact]
    public async Task ElementAtAsync_ReturnsRowAtIndex()
    {
        using TestDatabase db = NewDb();
        Book row = await db.Table<Book>().OrderBy(b => b.Id).ElementAtAsync(1);
        Assert.Equal(2, row.Id);
    }

    [Fact]
    public async Task ElementAtAsync_OutOfRange_Throws()
    {
        using TestDatabase db = NewDb();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await db.Table<Book>().OrderBy(b => b.Id).ElementAtAsync(99));
    }

    [Fact]
    public async Task ElementAtOrDefaultAsync_ReturnsRowAtIndex()
    {
        using TestDatabase db = NewDb();
        Book? row = await db.Table<Book>().OrderBy(b => b.Id).ElementAtOrDefaultAsync(2);
        Assert.NotNull(row);
        Assert.Equal(3, row.Id);
    }

    [Fact]
    public async Task ElementAtOrDefaultAsync_OutOfRange_ReturnsNull()
    {
        using TestDatabase db = NewDb();
        Book? row = await db.Table<Book>().OrderBy(b => b.Id).ElementAtOrDefaultAsync(99);
        Assert.Null(row);
    }

    [Fact]
    public async Task ElementAtAsync_NotSQLite_Throws()
    {
        IQueryable<int> q = new[] { 1, 2, 3 }.AsQueryable();
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await q.ElementAtAsync(0));
    }
}
