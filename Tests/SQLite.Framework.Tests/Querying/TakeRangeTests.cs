using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class TakeRangeTests
{
    [Fact]
    public void TakeRangeMatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        for (int i = 1; i <= 6; i++)
        {
            db.Table<Book>().Add(new Book { Id = i, Title = "T" + i, AuthorId = 1, Price = i });
        }

        List<Book> seed = Enumerable.Range(1, 6)
            .Select(i => new Book { Id = i, Title = "T" + i, AuthorId = 1, Price = i })
            .ToList();

        List<int> oracle = seed.OrderBy(b => b.Id).Take(1..4).Select(b => b.Id).ToList();
        List<int> actual = db.Table<Book>().OrderBy(b => b.Id).Take(1..4).Select(b => b.Id).ToList();

        Assert.Equal([2, 3, 4], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void TakeThenTakeRangeMatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        for (int i = 1; i <= 6; i++)
        {
            db.Table<Book>().Add(new Book { Id = i, Title = "T" + i, AuthorId = 1, Price = i });
        }

        List<Book> seed = Enumerable.Range(1, 6)
            .Select(i => new Book { Id = i, Title = "T" + i, AuthorId = 1, Price = i })
            .ToList();

        List<int> oracle = seed.OrderBy(b => b.Id).Take(5).Take(1..3).Select(b => b.Id).ToList();
        List<int> actual = db.Table<Book>().OrderBy(b => b.Id).Take(5).Take(1..3).Select(b => b.Id).ToList();

        Assert.Equal([2, 3], oracle);
        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void TakeRangeFromEndStartThrows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<NotSupportedException>(() => db.Table<Book>().OrderBy(b => b.Id).Take(^2..).ToList());
    }

    [Fact]
    public void TakeRangeFromEndBoundThrows()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        Assert.Throws<NotSupportedException>(() => db.Table<Book>().OrderBy(b => b.Id).Take(1..^1).ToList());
    }
}
