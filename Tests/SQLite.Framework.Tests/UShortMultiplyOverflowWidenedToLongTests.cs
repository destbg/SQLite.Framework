using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class UShortWidenRow
{
    [Key]
    public int Id { get; set; }

    public ushort US { get; set; }
}

public class UShortMultiplyOverflowWidenedToLongTests
{
    private static readonly UShortWidenRow[] Data =
    [
        new UShortWidenRow { Id = 1, US = 60000 },
        new UShortWidenRow { Id = 2, US = 4 },
    ];

    private static TestDatabase Create()
    {
        TestDatabase db = new();
        db.Table<UShortWidenRow>().Schema.CreateTable();
        foreach (UShortWidenRow r in Data)
        {
            db.Table<UShortWidenRow>().Add(r);
        }

        return db;
    }

    [Fact]
    public void CastToLongOfOverflowingUShortProductKeepsFullProduct()
    {
        using TestDatabase db = Create();

        List<long> actual = db.Table<UShortWidenRow>().Select(x => (long)(x.US * x.US)).OrderBy(v => v).ToList();

        Assert.Equal([16L, 3600000000L], actual);
    }
}
