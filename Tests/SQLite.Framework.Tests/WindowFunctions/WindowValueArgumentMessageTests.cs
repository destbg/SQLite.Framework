using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21gWeightRows")]
public class H21gWeightRow
{
    [Key]
    public int Id { get; set; }

    public int Weight { get; set; }
}

public class WindowValueArgumentMessageTests
{
    [Fact]
    public void SumValueArgumentWithoutSqlThrowsCleanError()
    {
        using TestDatabase db = Setup();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => db.Table<H21gWeightRow>()
            .Select(x => new
            {
                x.Id,
                S = SQLiteWindowFunctions.Sum(new { Y = x.Weight }).Over().PartitionBy(x.Id).AsValue()
            })
            .ToList());

        Assert.Equal("The value argument of Sum cannot be translated to SQL.", ex.Message);
    }

    [Fact]
    public void NTileBucketArgumentWithoutSqlThrowsCleanError()
    {
        using TestDatabase db = Setup();
        Func<int, long> buckets = v => v;

        Assert.Throws<NotSupportedException>(() => db.Table<H21gWeightRow>()
            .Select(x => new
            {
                x.Id,
                N = SQLiteWindowFunctions.NTile(buckets(x.Weight)).Over().OrderBy(x.Id).AsValue()
            })
            .ToList());
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H21gWeightRow>().Schema.CreateTable();
        db.Table<H21gWeightRow>().AddRange([
            new H21gWeightRow { Id = 1, Weight = 10 },
            new H21gWeightRow { Id = 2, Weight = 20 }
        ]);
        return db;
    }
}
