using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ReturningComputedProjectionExecuteTests
{
    private static List<Book> Rows() =>
    [
        new Book { Id = 1, Title = "banana", AuthorId = 1, Price = 2.0 },
        new Book { Id = 2, Title = "apple", AuthorId = 1, Price = 4.5 },
        new Book { Id = 3, Title = "kiwi", AuthorId = 2, Price = 5.0 },
    ];

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void ExecuteDeleteReturnsComputedValues()
    {
        using TestDatabase db = Seed();

        List<double> expected = Rows().Where(b => b.Price >= 4.5).Select(b => b.Price * 2).OrderBy(v => v).ToList();
        Assert.Equal([9.0, 10.0], expected);

        List<double> actual = db.Table<Book>()
            .Where(b => b.Price >= 4.5)
            .Returning(b => b.Price * 2)
            .ExecuteDelete();
        Assert.Equal(expected, actual.OrderBy(v => v).ToList());
    }

    [Fact]
    public void ExecuteUpdateReturnsComputedValues()
    {
        using TestDatabase db = Seed();

        List<double> expected = Rows().Where(b => b.Id == 1).Select(b => b.Price + 100).ToList();
        Assert.Equal([102.0], expected);

        List<double> actual = db.Table<Book>()
            .Where(b => b.Id == 1)
            .Returning(b => b.Price + 100)
            .ExecuteUpdate(s => s.Set(b => b.AuthorId, 7));
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ExecuteDeleteReturnsConcatenatedString()
    {
        using TestDatabase db = Seed();

        List<string> expected = Rows().Where(b => b.Id == 3).Select(b => b.Title + "?").ToList();
        Assert.Equal(["kiwi?"], expected);

        List<string> actual = db.Table<Book>()
            .Where(b => b.Id == 3)
            .Returning(b => b.Title + "?")
            .ExecuteDelete();
        Assert.Equal(expected, actual);
    }
}
