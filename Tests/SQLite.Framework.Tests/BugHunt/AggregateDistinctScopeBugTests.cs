using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

public class AggregateDistinctScopeBugTests
{
    [Fact]
    public void DistinctMultiColumnThenSumSelector_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 1.0 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 1, Price = 2.0 });

        (int AuthorId, double Price)[] seed = [(1, 1.0), (1, 2.0)];
        int expected = seed.Distinct().Sum(x => x.AuthorId);
        int actual = db.Table<Book>().Select(b => new { b.AuthorId, b.Price }).Distinct().Sum(x => x.AuthorId);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DistinctMultiColumnThenAverageSelector_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "a", AuthorId = 1, Price = 10.0 });
        db.Table<Book>().Add(new Book { Id = 2, Title = "b", AuthorId = 1, Price = 20.0 });
        db.Table<Book>().Add(new Book { Id = 3, Title = "c", AuthorId = 2, Price = 30.0 });

        (int AuthorId, double Price)[] seed = [(1, 10.0), (1, 20.0), (2, 30.0)];
        double expected = seed.Distinct().Average(x => (double)x.AuthorId);
        double actual = db.Table<Book>().Select(b => new { b.AuthorId, b.Price }).Distinct().Average(x => (double)x.AuthorId);

        Assert.Equal(expected, actual);
    }
}
