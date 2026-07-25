using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DistinctAggregateScopeTests
{
    private static TestDatabase CreateDb()
    {
        TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 10.0 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 1, Price = 20.0 });
        db.Table<Book>().Add(new Book { Id = 3, Title = "c", AuthorId = 2, Price = 30.0 });
        return db;
    }

    [Fact]
    public void MultiColumnDistinctThenSumSelector_ThrowsClearError()
    {
        using TestDatabase db = CreateDb();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Select(b => new { b.AuthorId, b.Price }).Distinct().Sum(x => x.AuthorId));

        Assert.Contains("single-column projection", ex.Message);
    }

    [Fact]
    public void MultiColumnDistinctThenAverageSelector_ThrowsClearError()
    {
        using TestDatabase db = CreateDb();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Select(b => new { b.AuthorId, b.Price }).Distinct().Average(x => (double)x.AuthorId));
    }

    [Fact]
    public void SingleColumnDistinctThenSum_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        int[] seed = [1, 1, 2];
        int expected = seed.Distinct().Sum();
        int actual = db.Table<Book>().Select(b => b.AuthorId).Distinct().Sum();

        Assert.Equal(3, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SingleColumnDistinctThenAverage_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        int[] seed = [1, 1, 2];
        double expected = seed.Distinct().Average();
        double actual = db.Table<Book>().Select(b => b.AuthorId).Distinct().Average();

        Assert.Equal(1.5, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MultiColumnDistinctThenMinSelector_StillCorrect()
    {
        using TestDatabase db = CreateDb();

        (int AuthorId, double Price)[] seed = [(1, 10.0), (1, 20.0), (2, 30.0)];
        int expected = seed.Distinct().Min(x => x.AuthorId);
        int actual = db.Table<Book>().Select(b => new { b.AuthorId, b.Price }).Distinct().Min(x => x.AuthorId);

        Assert.Equal(1, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MultiColumnProjectionSumSelector_WithoutDistinct_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        (int AuthorId, double Price)[] seed = [(1, 10.0), (1, 20.0), (2, 30.0)];
        int expected = seed.Sum(x => x.AuthorId);
        int actual = db.Table<Book>().Select(b => new { b.AuthorId, b.Price }).Sum(x => x.AuthorId);

        Assert.Equal(4, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MultiColumnDistinctThenCountSelector_ThrowsClearError()
    {
        using TestDatabase db = CreateDb();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Select(b => new { b.AuthorId, b.Price }).Distinct().Count(x => x.AuthorId == 1));
    }
}
