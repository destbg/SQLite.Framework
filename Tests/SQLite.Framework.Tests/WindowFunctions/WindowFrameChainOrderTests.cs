using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21gFrameRows")]
public class H21gFrameRow
{
    [Key]
    public int Id { get; set; }

    public int Grp { get; set; }

    public int Amount { get; set; }
}

public class WindowFrameChainOrderTests
{
    [Fact]
    public void PartitionByAfterRowsFrameThrows()
    {
        using TestDatabase db = Setup();

        Assert.Throws<NotSupportedException>(() => db.Table<H21gFrameRow>()
            .Select(x => SQLiteWindowFunctions.Sum(x.Amount)
                .Over()
                .OrderBy(x.Id)
                .Rows(SQLiteFrameBoundary.UnboundedPreceding(), SQLiteFrameBoundary.CurrentRow())
                .PartitionBy(x.Grp)
                .AsValue())
            .ToList());
    }

    [Fact]
    public void OrderByAfterRowsFrameThrows()
    {
        using TestDatabase db = Setup();

        Assert.Throws<NotSupportedException>(() => db.Table<H21gFrameRow>()
            .Select(x => SQLiteWindowFunctions.Sum(x.Amount)
                .Over()
                .Rows(SQLiteFrameBoundary.UnboundedPreceding(), SQLiteFrameBoundary.CurrentRow())
                .OrderBy(x.Id)
                .AsValue())
            .ToList());
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H21gFrameRow>().Schema.CreateTable();
        db.Table<H21gFrameRow>().AddRange([
            new H21gFrameRow { Id = 1, Grp = 1, Amount = 10 },
            new H21gFrameRow { Id = 2, Grp = 1, Amount = 20 },
            new H21gFrameRow { Id = 3, Grp = 2, Amount = 30 }
        ]);
        return db;
    }
}
