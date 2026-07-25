using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ElementAtOutOfRangeTests
{
    private static TestDatabase CreateDb(int count)
    {
        TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        for (int i = 1; i <= count; i++)
        {
            db.Table<Book>().Add(new Book { Id = i, Title = "T", AuthorId = 1, Price = i });
        }

        return db;
    }

    [Fact]
    public void ElementAt_JustPastEnd_ThrowsArgumentOutOfRangeLikeBcl()
    {
        using TestDatabase db = CreateDb(3);

        Assert.Throws<ArgumentOutOfRangeException>(() => new[] { 1, 2, 3 }.ElementAt(3));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Id).ElementAt(3));
    }

    [Fact]
    public void ElementAt_FarPastEnd_ThrowsArgumentOutOfRangeLikeBcl()
    {
        using TestDatabase db = CreateDb(3);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Id).ElementAt(99));
    }

    [Fact]
    public void ElementAt_EmptyTable_ThrowsArgumentOutOfRangeLikeBcl()
    {
        using TestDatabase db = CreateDb(0);

        Assert.Throws<ArgumentOutOfRangeException>(() => Array.Empty<int>().ElementAt(0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Id).ElementAt(0));
    }

    [Fact]
    public void ElementAt_InRange_ReturnsElement()
    {
        using TestDatabase db = CreateDb(3);

        int expected = new[] { 1, 2, 3 }.ElementAt(2);
        int actual = db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Id).ElementAt(2);

        Assert.Equal(3, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task ElementAtAsync_PastEnd_ThrowsArgumentOutOfRange()
    {
        using TestDatabase db = CreateDb(3);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Id).ElementAtAsync(5));
    }

    [Fact]
    public void ElementAtOrDefault_PastEnd_ReturnsDefault()
    {
        using TestDatabase db = CreateDb(3);

        int expected = new[] { 1, 2, 3 }.ElementAtOrDefault(99);
        int actual = db.Table<Book>().OrderBy(b => b.Id).Select(b => b.Id).ElementAtOrDefault(99);

        Assert.Equal(0, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void First_OnEmpty_StillThrowsInvalidOperation()
    {
        using TestDatabase db = CreateDb(0);

        Assert.Throws<InvalidOperationException>(() => db.Table<Book>().Select(b => b.Id).First());
    }

    [Fact]
    public void Single_OnEmpty_StillThrowsInvalidOperation()
    {
        using TestDatabase db = CreateDb(0);

        Assert.Throws<InvalidOperationException>(() => db.Table<Book>().Select(b => b.Id).Single());
    }
}
