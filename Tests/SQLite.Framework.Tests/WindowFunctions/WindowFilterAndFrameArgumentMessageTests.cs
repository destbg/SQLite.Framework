using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22kGaugeRows")]
public class H22kGaugeRow
{
    [Key]
    public int Id { get; set; }

    public int Weight { get; set; }
}

public class WindowFilterAndFrameArgumentMessageTests
{
    [Fact]
    public void FilterPredicateWithoutSqlThrowsCleanError()
    {
        using TestDatabase db = Setup();
        Func<int, bool> keep = v => v > 10;

        Assert.Throws<NotSupportedException>(() => db.Table<H22kGaugeRow>()
            .Select(x => new
            {
                x.Id,
                S = SQLiteWindowFunctions.Sum(x.Weight).Filter(keep(x.Weight)).Over().AsValue()
            })
            .ToList());
    }

    [Fact]
    public void FramePrecedingOffsetWithoutSqlThrowsCleanError()
    {
        using TestDatabase db = Setup();
        Func<int, long> offset = v => v;

        Assert.Throws<NotSupportedException>(() => db.Table<H22kGaugeRow>()
            .Select(x => new
            {
                x.Id,
                S = SQLiteWindowFunctions.Sum(x.Weight)
                    .Over()
                    .OrderBy(x.Id)
                    .Rows(SQLiteFrameBoundary.Preceding(offset(x.Weight)), SQLiteFrameBoundary.CurrentRow())
                    .AsValue()
            })
            .ToList());
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H22kGaugeRow>().Schema.CreateTable();
        db.Table<H22kGaugeRow>().AddRange([
            new H22kGaugeRow { Id = 1, Weight = 10 },
            new H22kGaugeRow { Id = 2, Weight = 20 }
        ]);
        return db;
    }
}
