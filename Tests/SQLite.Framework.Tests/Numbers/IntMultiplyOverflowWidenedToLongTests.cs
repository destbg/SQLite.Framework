using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class IntWidenRow
{
    [Key]
    public int Id { get; set; }

    public int I { get; set; }
}

public class IntMultiplyOverflowWidenedToLongTests
{
    private static readonly IntWidenRow[] Data =
    [
        new IntWidenRow { Id = 1, I = 100000 },
        new IntWidenRow { Id = 2, I = 3 },
    ];

    private static TestDatabase Create()
    {
        TestDatabase db = new();
        db.Table<IntWidenRow>().Schema.CreateTable();
        foreach (IntWidenRow r in Data)
        {
            db.Table<IntWidenRow>().Add(r);
        }

        return db;
    }

    [Fact]
    public void CastToLongOfOverflowingIntProductKeepsFullProduct()
    {
        using TestDatabase db = Create();

        List<long> actual = db.Table<IntWidenRow>().Select(x => (long)(x.I * x.I)).OrderBy(v => v).ToList();

        Assert.Equal([9L, 10000000000L], actual);
    }

    [Fact]
    public void WhereOnWidenedIntProductDoesNotOverflow()
    {
        using TestDatabase db = Create();

        List<int> actual = db.Table<IntWidenRow>().Where(x => (long)(x.I * x.I) < 0).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Empty(actual);
    }
}
