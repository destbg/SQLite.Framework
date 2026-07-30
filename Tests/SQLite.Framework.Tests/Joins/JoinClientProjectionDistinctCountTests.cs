using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24jCountLefts")]
public class H24jCountLeft
{
    [Key]
    public int LId { get; set; }

    public int LK { get; set; }
}

[Table("H24jCountRights")]
public class H24jCountRight
{
    [Key]
    public int RId { get; set; }

    public int RK { get; set; }

    public int V { get; set; }
}

public class JoinClientProjectionDistinctCountTests
{
    public static string BucketLabel(int value)
    {
        return value < 100 ? "low" : "high";
    }

    [Fact]
    public void CountAfterDistinctOverAJoinClientProjectionCountsDistinctProjectedValues()
    {
        using TestDatabase db = Setup(nameof(CountAfterDistinctOverAJoinClientProjectionCountsDistinctProjectedValues));

        int expected = Lefts()
            .Join(Rights(), l => l.LK, r => r.RK, (l, r) => new { L = l.LId, T = BucketLabel(r.V) })
            .Distinct()
            .Count();

        int actual = db.Table<H24jCountLeft>()
            .Join(db.Table<H24jCountRight>(), l => l.LK, r => r.RK, (l, r) => new { L = l.LId, T = BucketLabel(r.V) })
            .Distinct()
            .Count();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LongCountAfterDistinctOverAJoinClientProjectionCountsDistinctProjectedValues()
    {
        using TestDatabase db = Setup(nameof(LongCountAfterDistinctOverAJoinClientProjectionCountsDistinctProjectedValues));

        long expected = Lefts()
            .Join(Rights(), l => l.LK, r => r.RK, (l, r) => new { L = l.LId, T = BucketLabel(r.V) })
            .Distinct()
            .LongCount();

        long actual = db.Table<H24jCountLeft>()
            .Join(db.Table<H24jCountRight>(), l => l.LK, r => r.RK, (l, r) => new { L = l.LId, T = BucketLabel(r.V) })
            .Distinct()
            .LongCount();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CountAfterATakeOnADistinctJoinClientProjectionCountsTheTakenRows()
    {
        using TestDatabase db = Setup(nameof(CountAfterATakeOnADistinctJoinClientProjectionCountsTheTakenRows));

        int expected = Lefts()
            .Join(Rights(), l => l.LK, r => r.RK, (l, r) => new { L = l.LId, T = BucketLabel(r.V) })
            .Distinct()
            .Take(2)
            .Count();

        int actual = db.Table<H24jCountLeft>()
            .Join(db.Table<H24jCountRight>(), l => l.LK, r => r.RK, (l, r) => new { L = l.LId, T = BucketLabel(r.V) })
            .Distinct()
            .Take(2)
            .Count();

        Assert.Equal(2, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CountAfterASkipOnADistinctJoinClientProjectionCountsTheRemainingRows()
    {
        using TestDatabase db = Setup(nameof(CountAfterASkipOnADistinctJoinClientProjectionCountsTheRemainingRows));

        int expected = Lefts()
            .Join(Rights(), l => l.LK, r => r.RK, (l, r) => new { L = l.LId, T = BucketLabel(r.V) })
            .Distinct()
            .Skip(2)
            .Count();

        int actual = db.Table<H24jCountLeft>()
            .Join(db.Table<H24jCountRight>(), l => l.LK, r => r.RK, (l, r) => new { L = l.LId, T = BucketLabel(r.V) })
            .Distinct()
            .Skip(2)
            .Count();

        Assert.Equal(4, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CountAfterDistinctOverACrossJoinClientProjectionCountsDistinctProjectedValues()
    {
        using TestDatabase db = Setup(nameof(CountAfterDistinctOverACrossJoinClientProjectionCountsDistinctProjectedValues));

        int expected = Lefts()
            .SelectMany(l => Rights(), (l, r) => new { T = BucketLabel(r.V) })
            .Distinct()
            .Count();

        int actual = db.Table<H24jCountLeft>()
            .SelectMany(l => db.Table<H24jCountRight>(), (l, r) => new { T = BucketLabel(r.V) })
            .Distinct()
            .Count();

        Assert.Equal(expected, actual);
    }

    private static List<H24jCountLeft> Lefts()
    {
        return
        [
            new H24jCountLeft { LId = 4, LK = 7 },
            new H24jCountLeft { LId = 5, LK = 7 },
            new H24jCountLeft { LId = 6, LK = 7 }
        ];
    }

    private static List<H24jCountRight> Rights()
    {
        return
        [
            new H24jCountRight { RId = 1, RK = 7, V = 1 },
            new H24jCountRight { RId = 2, RK = 7, V = 2 },
            new H24jCountRight { RId = 3, RK = 7, V = 150 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H24jCountLeft>().Schema.CreateTable();
        db.Table<H24jCountRight>().Schema.CreateTable();
        db.Table<H24jCountLeft>().AddRange(Lefts());
        db.Table<H24jCountRight>().AddRange(Rights());
        return db;
    }
}
