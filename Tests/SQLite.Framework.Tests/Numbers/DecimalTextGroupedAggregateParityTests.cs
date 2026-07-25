using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21mBucketRows")]
public class H21mBucketRow
{
    [Key]
    public int Id { get; set; }

    public int Grp { get; set; }

    public decimal Amount { get; set; }
}

public class DecimalTextGroupedAggregateParityTests
{
    [Fact]
    public void GroupedMinMatchesLinq()
    {
        using TestDatabase db = Seed();
        List<H21mBucketRow> local = Rows();

        List<(int Key, decimal Min)> expected = local
            .GroupBy(r => r.Grp)
            .Select(g => (g.Key, Min: g.Min(r => r.Amount)))
            .OrderBy(x => x.Key)
            .ToList();

        List<(int Key, decimal Min)> actual = db.Table<H21mBucketRow>()
            .GroupBy(r => r.Grp)
            .Select(g => new { g.Key, Min = g.Min(r => r.Amount) })
            .AsEnumerable()
            .Select(x => (x.Key, x.Min))
            .OrderBy(x => x.Key)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GroupedMaxMatchesLinq()
    {
        using TestDatabase db = Seed();
        List<H21mBucketRow> local = Rows();

        List<(int Key, decimal Max)> expected = local
            .GroupBy(r => r.Grp)
            .Select(g => (g.Key, Max: g.Max(r => r.Amount)))
            .OrderBy(x => x.Key)
            .ToList();

        List<(int Key, decimal Max)> actual = db.Table<H21mBucketRow>()
            .GroupBy(r => r.Grp)
            .Select(g => new { g.Key, Max = g.Max(r => r.Amount) })
            .AsEnumerable()
            .Select(x => (x.Key, x.Max))
            .OrderBy(x => x.Key)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RootMinMatchesLinq()
    {
        using TestDatabase db = Seed();
        List<H21mBucketRow> local = Rows();

        decimal expected = local.Min(r => r.Amount);

        decimal actual = db.Table<H21mBucketRow>().Min(r => r.Amount);

        Assert.Equal(expected, actual);
    }

    private static List<H21mBucketRow> Rows()
    {
        return
        [
            new H21mBucketRow { Id = 1, Grp = 1, Amount = 9.99m },
            new H21mBucketRow { Id = 2, Grp = 1, Amount = 10.11m },
            new H21mBucketRow { Id = 3, Grp = 2, Amount = 2.5m }
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new(b => b.UseDecimalStorage(DecimalStorageMode.Text));
        db.Table<H21mBucketRow>().Schema.CreateTable();
        db.Table<H21mBucketRow>().AddRange(Rows());
        return db;
    }
}
