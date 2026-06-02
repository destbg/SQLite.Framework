using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ElementAtNegativeIndexTests
{
    private static readonly int[] SeedIds = [1, 2, 3];

    private static TestDatabase CreateDb()
    {
        TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        foreach (int id in SeedIds)
        {
            db.Table<Book>().Add(new Book { Id = id, Title = "T", AuthorId = 1, Price = id });
        }

        return db;
    }

    [Fact]
    public void ElementAtOrDefault_NegativeIndex_ReturnsDefault()
    {
        using TestDatabase db = CreateDb();

        int expected = SeedIds.ElementAtOrDefault(-1);
        int actual = db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Id).ElementAtOrDefault(-1);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ElementAtOrDefault_NegativeIndexFar_ReturnsDefault()
    {
        using TestDatabase db = CreateDb();

        int expected = SeedIds.ElementAtOrDefault(-5);
        int actual = db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Id).ElementAtOrDefault(-5);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task ElementAtOrDefaultAsync_NegativeIndex_ReturnsDefault()
    {
        using TestDatabase db = CreateDb();

        int expected = SeedIds.ElementAtOrDefault(-1);
        int actual = await db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Id).ElementAtOrDefaultAsync(-1);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task ElementAtOrDefaultAsync_NegativeIndexAfterTake_ReturnsDefault()
    {
        using TestDatabase db = CreateDb();

        int expected = SeedIds.Take(2).ElementAtOrDefault(-1);
        int actual = await db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Id).Take(2).ElementAtOrDefaultAsync(-1);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ElementAtOrDefault_NegativeIndexAfterSkip_ReturnsDefault()
    {
        using TestDatabase db = CreateDb();

        int expected = SeedIds.Skip(1).ElementAtOrDefault(-1);
        int actual = db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Id).Skip(1).ElementAtOrDefault(-1);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ElementAtOrDefault_WholeEntityNegativeIndex_ReturnsNull()
    {
        using TestDatabase db = CreateDb();

        Book? actual = db.Table<Book>().OrderBy(b => b.Id).ElementAtOrDefault(-1);

        Assert.Null(actual);
    }

    [Fact]
    public void ElementAtOrDefault_ZeroIndex_ReturnsFirst()
    {
        using TestDatabase db = CreateDb();

        int expected = SeedIds.ElementAtOrDefault(0);
        int actual = db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Id).ElementAtOrDefault(0);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ElementAt_NegativeIndex_Throws()
    {
        using TestDatabase db = CreateDb();

        Assert.Throws<ArgumentOutOfRangeException>(() => SeedIds.ElementAt(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Id).ElementAt(-1));
    }

    [Fact]
    public async Task ElementAtAsync_NegativeIndex_Throws()
    {
        using TestDatabase db = CreateDb();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Id).ElementAtAsync(-1));
    }
}
