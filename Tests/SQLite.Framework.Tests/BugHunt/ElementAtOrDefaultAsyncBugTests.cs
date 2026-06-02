using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

public class ElementAtOrDefaultAsyncBugTests
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
    public async Task ElementAtOrDefaultAsync_NegativeIndex_ReturnsDefault()
    {
        using TestDatabase db = NewDb();

        List<int> rows = new() { 1 };
        int expected = rows.ElementAtOrDefault(-1);

        using TestDatabase single = new();
        single.Table<Book>().Schema.CreateTable();
        single.Table<Book>().Add(new Book { Id = 1, Title = "A", AuthorId = 1, Price = 10 });

        int actual = await single.Table<Book>().OrderBy(b => b.Id).Select(b => b.Id).ElementAtOrDefaultAsync(-1);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task ElementAtOrDefaultAsync_NegativeIndexAfterTake_ReturnsDefault()
    {
        using TestDatabase db = NewDb();

        List<int> rows = new() { 1, 2, 3 };
        int expected = rows.Take(2).ElementAtOrDefault(-1);

        int actual = await db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Id).Take(2).ElementAtOrDefaultAsync(-1);

        Assert.Equal(expected, actual);
    }
}
