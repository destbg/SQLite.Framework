using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23hPagedLefts")]
public class H23hPagedLeft
{
    [Key]
    public int Id { get; set; }

    public int K { get; set; }
}

[Table("H23hPagedRights")]
public class H23hPagedRight
{
    [Key]
    public int Id { get; set; }

    public int K { get; set; }

    public int V { get; set; }
}

public class JoinClientProjectionDistinctPagingTests
{
    public static string BucketLabel(int value)
    {
        return value < 100 ? "low" : "high";
    }

    [Fact]
    public void SkipAfterDistinctCountsDistinctProjectedValues()
    {
        using TestDatabase db = Setup(nameof(SkipAfterDistinctCountsDistinctProjectedValues));
        List<H23hPagedLeft> lefts = Lefts();
        List<H23hPagedRight> rights = Rights();

        List<(int L, string T)> expected = lefts
            .Join(rights, l => l.K, r => r.K, (l, r) => new { L = l.Id, T = BucketLabel(r.V) })
            .Distinct()
            .Skip(2)
            .Select(x => (x.L, x.T))
            .ToList();

        List<(int L, string T)> actual = db.Table<H23hPagedLeft>()
            .Join(db.Table<H23hPagedRight>(), l => l.K, r => r.K, (l, r) => new { L = l.Id, T = BucketLabel(r.V) })
            .Distinct()
            .Skip(2)
            .AsEnumerable()
            .Select(x => (x.L, x.T))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SkipAndTakeAfterDistinctPageOverDistinctProjectedValues()
    {
        using TestDatabase db = Setup(nameof(SkipAndTakeAfterDistinctPageOverDistinctProjectedValues));
        List<H23hPagedLeft> lefts = Lefts();
        List<H23hPagedRight> rights = Rights();

        List<(int L, string T)> expected = lefts
            .Join(rights, l => l.K, r => r.K, (l, r) => new { L = l.Id, T = BucketLabel(r.V) })
            .Distinct()
            .Skip(1)
            .Take(1)
            .Select(x => (x.L, x.T))
            .ToList();

        List<(int L, string T)> actual = db.Table<H23hPagedLeft>()
            .Join(db.Table<H23hPagedRight>(), l => l.K, r => r.K, (l, r) => new { L = l.Id, T = BucketLabel(r.V) })
            .Distinct()
            .Skip(1)
            .Take(1)
            .AsEnumerable()
            .Select(x => (x.L, x.T))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H23hPagedLeft> Lefts()
    {
        return [new H23hPagedLeft { Id = 1, K = 7 }];
    }

    private static List<H23hPagedRight> Rights()
    {
        return
        [
            new H23hPagedRight { Id = 1, K = 7, V = 1 },
            new H23hPagedRight { Id = 2, K = 7, V = 2 },
            new H23hPagedRight { Id = 3, K = 7, V = 3 },
            new H23hPagedRight { Id = 4, K = 7, V = 4 },
            new H23hPagedRight { Id = 5, K = 7, V = 5 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H23hPagedLeft>().Schema.CreateTable();
        db.Table<H23hPagedRight>().Schema.CreateTable();
        db.Table<H23hPagedLeft>().AddRange(Lefts());
        db.Table<H23hPagedRight>().AddRange(Rights());
        return db;
    }
}
