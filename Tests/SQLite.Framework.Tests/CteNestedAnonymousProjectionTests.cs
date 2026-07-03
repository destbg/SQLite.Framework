using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

// ReSharper disable AccessToDisposedClosure

namespace SQLite.Framework.Tests;

public class CteNestedAnonymousProjectionTests
{
    private static List<Book> Rows() =>
    [
        new Book { Id = 1, Title = "a", AuthorId = 1, Price = 10 },
        new Book { Id = 2, Title = "b", AuthorId = 1, Price = 20 },
        new Book { Id = 3, Title = "c", AuthorId = 2, Price = 30 },
    ];

    private static TestDatabase CreateDb()
    {
        TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void NestedAnonymousMemberRead()
    {
        using TestDatabase db = CreateDb();

        List<double> expected = Rows().OrderBy(b => b.Id).Select(b => b.Price).ToList();
        Assert.Equal([10.0, 20.0, 30.0], expected);

        var cte = db.With(() => db.Table<Book>().Select(b => new { b.Id, Inner = new { b.Price } }));
        List<double> actual = cte
            .OrderBy(x => x.Id)
            .Select(x => x.Inner.Price)
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NestedEntityMemberRead()
    {
        using TestDatabase db = CreateDb();

        List<double> expected = Rows()
            .Select(b => new { b.Id, B = b })
            .OrderBy(x => x.Id)
            .Select(x => x.B.Price)
            .ToList();
        Assert.Equal([10.0, 20.0, 30.0], expected);

        var cte = db.With(() => db.Table<Book>().Select(b => new { b.Id, B = b }));
        List<double> actual = cte
            .OrderBy(x => x.Id)
            .Select(x => x.B.Price)
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NestedAnonymousWholeRowsRead()
    {
        using TestDatabase db = CreateDb();

        List<(int Id, double Price)> expected = Rows()
            .Select(b => new { b.Id, Inner = new { b.Price } })
            .OrderBy(x => x.Id)
            .Select(x => (x.Id, x.Inner.Price))
            .ToList();
        Assert.Equal([(1, 10.0), (2, 20.0), (3, 30.0)], expected);

        var cte = db.With(() => db.Table<Book>().Select(b => new { b.Id, Inner = new { b.Price } }));
        List<(int Id, double Price)> actual = cte
            .ToList()
            .OrderBy(x => x.Id)
            .Select(x => (x.Id, x.Inner.Price))
            .ToList();
        Assert.Equal(expected, actual);
    }
}
