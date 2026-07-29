using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24jPagedLefts")]
public class H24jPagedLeft
{
    [Key]
    public int Id { get; set; }

    public int K { get; set; }
}

[Table("H24jPagedRights")]
public class H24jPagedRight
{
    [Key]
    public int Id { get; set; }

    public int K { get; set; }

    public int V { get; set; }
}

public class CrossJoinClientProjectionDistinctPagingTests
{
    public static string BucketLabel(int value)
    {
        return value < 100 ? "low" : "high";
    }

    [Fact]
    public void SkipAfterDistinctOverCrossJoinCountsDistinctProjectedValues()
    {
        using TestDatabase db = Setup(nameof(SkipAfterDistinctOverCrossJoinCountsDistinctProjectedValues));
        List<H24jPagedLeft> lefts = Lefts();
        List<H24jPagedRight> rights = Rights();

        List<(int L, string T)> expected = lefts
            .SelectMany(l => rights, (l, r) => new { L = l.Id, T = BucketLabel(r.V) })
            .Distinct()
            .Skip(2)
            .Select(x => (x.L, x.T))
            .ToList();

        List<(int L, string T)> actual = db.Table<H24jPagedLeft>()
            .SelectMany(l => db.Table<H24jPagedRight>(), (l, r) => new { L = l.Id, T = BucketLabel(r.V) })
            .Distinct()
            .Skip(2)
            .AsEnumerable()
            .Select(x => (x.L, x.T))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SkipAndTakeAfterDistinctOverCrossJoinPageOverDistinctProjectedValues()
    {
        using TestDatabase db = Setup(nameof(SkipAndTakeAfterDistinctOverCrossJoinPageOverDistinctProjectedValues));
        List<H24jPagedLeft> lefts = Lefts();
        List<H24jPagedRight> rights = Rights();

        List<(int L, string T)> expected = lefts
            .SelectMany(l => rights, (l, r) => new { L = l.Id, T = BucketLabel(r.V) })
            .Distinct()
            .Skip(1)
            .Take(1)
            .Select(x => (x.L, x.T))
            .ToList();

        List<(int L, string T)> actual = db.Table<H24jPagedLeft>()
            .SelectMany(l => db.Table<H24jPagedRight>(), (l, r) => new { L = l.Id, T = BucketLabel(r.V) })
            .Distinct()
            .Skip(1)
            .Take(1)
            .AsEnumerable()
            .Select(x => (x.L, x.T))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SkipAfterDistinctOverFlattenedLeftJoinCountsDistinctProjectedValues()
    {
        using TestDatabase db = Setup(nameof(SkipAfterDistinctOverFlattenedLeftJoinCountsDistinctProjectedValues));
        List<H24jPagedLeft> lefts = Lefts();
        List<H24jPagedRight> rights = Rights();

        List<(int L, string T)> expected = (from l in lefts
                join r in rights on l.K equals r.K into g
                from r in g.DefaultIfEmpty()
                select new { L = l.Id, T = BucketLabel(r.V) })
            .Distinct()
            .Skip(2)
            .Select(x => (x.L, x.T))
            .ToList();

        List<(int L, string T)> actual = (from l in db.Table<H24jPagedLeft>()
                join r in db.Table<H24jPagedRight>() on l.K equals r.K into g
                from r in g.DefaultIfEmpty()
                select new { L = l.Id, T = BucketLabel(r.V) })
            .Distinct()
            .Skip(2)
            .AsEnumerable()
            .Select(x => (x.L, x.T))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H24jPagedLeft> Lefts()
    {
        return [new H24jPagedLeft { Id = 1, K = 7 }];
    }

    private static List<H24jPagedRight> Rights()
    {
        return
        [
            new H24jPagedRight { Id = 1, K = 7, V = 1 },
            new H24jPagedRight { Id = 2, K = 7, V = 2 },
            new H24jPagedRight { Id = 3, K = 7, V = 3 },
            new H24jPagedRight { Id = 4, K = 7, V = 4 },
            new H24jPagedRight { Id = 5, K = 7, V = 5 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H24jPagedLeft>().Schema.CreateTable();
        db.Table<H24jPagedRight>().Schema.CreateTable();
        db.Table<H24jPagedLeft>().AddRange(Lefts());
        db.Table<H24jPagedRight>().AddRange(Rights());
        return db;
    }
}
