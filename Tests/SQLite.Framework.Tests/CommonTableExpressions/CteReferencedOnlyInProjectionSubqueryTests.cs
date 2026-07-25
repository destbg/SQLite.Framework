using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

// ReSharper disable AccessToDisposedClosure

namespace SQLite.Framework.Tests;

public class CteReferencedOnlyInProjectionSubqueryTests
{
    private static List<Book> Rows() =>
    [
        new Book { Id = 1, Title = "a", AuthorId = 1, Price = 10 },
        new Book { Id = 2, Title = "b", AuthorId = 1, Price = 20 },
        new Book { Id = 3, Title = "c", AuthorId = 2, Price = 30 },
        new Book { Id = 4, Title = "d", AuthorId = 2, Price = 40 },
    ];

    [Fact]
    public void CteCountSubqueryInProjection()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(Rows());

        List<Book> cteRows = Rows().Where(b => b.Price > 15).ToList();
        List<int> expected = Rows().OrderBy(b => b.Id)
            .Select(b => cteRows.Count(c => c.AuthorId == b.AuthorId)).ToList();
        Assert.Equal([1, 1, 2, 2], expected);

        SQLiteCte<Book> cte = db.With(() => db.Table<Book>().Where(b => b.Price > 15));
        List<int> actual = db.Table<Book>()
            .Select(b => new { b.Id, Cnt = cte.Count(c => c.AuthorId == b.AuthorId) })
            .ToList()
            .OrderBy(x => x.Id).Select(x => x.Cnt).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CteSourceWithComputedProjection()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(Rows());

        List<double> expected = Rows().Where(b => b.Price > 15).OrderBy(b => b.Id).Select(b => b.Price * 2).ToList();
        Assert.Equal([40.0, 60.0, 80.0], expected);

        SQLiteCte<Book> cte = db.With(() => db.Table<Book>().Where(b => b.Price > 15));
        List<double> actual = cte
            .Select(b => new { b.Id, Total = b.Price * 2 })
            .OrderBy(x => x.Id)
            .Select(x => x.Total)
            .ToList();
        Assert.Equal(expected, actual);
    }
}
