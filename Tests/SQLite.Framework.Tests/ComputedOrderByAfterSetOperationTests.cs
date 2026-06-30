using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class ComputedOrderBySetRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }

    public int B { get; set; }
}

public class ComputedOrderByAfterSetOperationTests
{
    private static readonly ComputedOrderBySetRow[] Data =
    [
        new ComputedOrderBySetRow { Id = 1, A = 3, B = 1 },
        new ComputedOrderBySetRow { Id = 2, A = 1, B = 2 },
        new ComputedOrderBySetRow { Id = 3, A = 2, B = 4 },
    ];

    [Fact]
    public void OrderByComputedKeyAfterUnionThrows()
    {
        using TestDatabase db = new();
        db.Table<ComputedOrderBySetRow>().Schema.CreateTable();
        foreach (ComputedOrderBySetRow r in Data)
        {
            db.Table<ComputedOrderBySetRow>().Add(r);
        }

        Assert.Throws<SQLiteException>(() => db.Table<ComputedOrderBySetRow>().Select(x => x.A)
            .Union(db.Table<ComputedOrderBySetRow>().Select(x => x.B))
            .OrderBy(x => -x)
            .ToList());
    }

    [Fact]
    public void OrderByBareColumnAfterUnionWorks()
    {
        using TestDatabase db = new();
        db.Table<ComputedOrderBySetRow>().Schema.CreateTable();
        foreach (ComputedOrderBySetRow r in Data)
        {
            db.Table<ComputedOrderBySetRow>().Add(r);
        }

        List<int> expected = Data.Select(x => x.A)
            .Union(Data.Select(x => x.B))
            .OrderBy(x => x)
            .ToList();

        List<int> actual = db.Table<ComputedOrderBySetRow>().Select(x => x.A)
            .Union(db.Table<ComputedOrderBySetRow>().Select(x => x.B))
            .OrderBy(x => x)
            .ToList();

        Assert.Equal([1, 2, 3, 4], expected);
        Assert.Equal(expected, actual);
    }
}
