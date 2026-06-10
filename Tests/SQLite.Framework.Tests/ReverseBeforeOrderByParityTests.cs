using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ReverseBeforeOrderByParityTests
{
    private static readonly Book[] Seed =
    [
        new Book { Id = 1, Title = "A", AuthorId = 1, Price = 1 },
        new Book { Id = 2, Title = "B", AuthorId = 1, Price = 2 },
        new Book { Id = 3, Title = "C", AuthorId = 1, Price = 3 },
    ];

    private static TestDatabase CreateDb()
    {
        TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(Seed);
        return db;
    }

    [Fact]
    public void Reverse_ThenOrderByAscending_MatchesDotNet()
    {
        using TestDatabase db = CreateDb();

        List<int> expected = Seed.AsQueryable().Reverse().OrderBy(b => b.Id).Select(b => b.Id).ToList();
        List<int> actual = db.Table<Book>().Reverse().OrderBy(b => b.Id).Select(b => b.Id).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Reverse_ThenOrderByDescending_MatchesDotNet()
    {
        using TestDatabase db = CreateDb();

        List<int> expected = Seed.AsQueryable().Reverse().OrderByDescending(b => b.Id).Select(b => b.Id).ToList();
        List<int> actual = db.Table<Book>().Reverse().OrderByDescending(b => b.Id).Select(b => b.Id).ToList();

        Assert.Equal(expected, actual);
    }
}
