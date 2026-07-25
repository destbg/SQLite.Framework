using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class GroupJoinSameTypeOutOfOrderFlattenTests
{
    private static (List<H20OoLeft> Ls, List<H20OoRight> Rs) Seed(TestDatabase db)
    {
        db.Table<H20OoLeft>().Schema.CreateTable();
        db.Table<H20OoRight>().Schema.CreateTable();

        List<H20OoLeft> ls = new()
        {
            new H20OoLeft { Id = 1, K1 = 10, K2 = 100, Tag = "a" },
            new H20OoLeft { Id = 2, K1 = 20, K2 = 200, Tag = "b" },
            new H20OoLeft { Id = 3, K1 = 30, K2 = 300, Tag = "c" },
        };
        List<H20OoRight> rs = new()
        {
            new H20OoRight { Id = 1, K = 10, RTag = "A" },
            new H20OoRight { Id = 2, K = 30, RTag = "B" },
            new H20OoRight { Id = 3, K = 100, RTag = "C" },
            new H20OoRight { Id = 4, K = 200, RTag = "D" },
        };

        db.Table<H20OoLeft>().AddRange(ls);
        db.Table<H20OoRight>().AddRange(rs);
        return (ls, rs);
    }

    [Fact]
    public void OutOfOrderFlattenCrossesJoinSemantics()
    {
        using TestDatabase db = new();
        var (ls, rs) = Seed(db);

        var expected = ls
            .GroupJoin(rs, l => l.K1, r => r.K, (l, g1) => new { l, g1 })
            .GroupJoin(rs, x => x.l.K2, r => r.K, (x, g2) => new { x.l, x.g1, g2 })
            .SelectMany(x => x.g2.DefaultIfEmpty(), (x, r2) => new { x.l, x.g1, r2 })
            .SelectMany(x => x.g1, (x, r1) => new { x.l.Id, R2 = x.r2 == null ? -1 : x.r2.Id, R1 = r1.Id })
            .OrderBy(t => t.Id)
            .ToList();

        var actual = db.Table<H20OoLeft>()
            .GroupJoin(db.Table<H20OoRight>(), l => l.K1, r => r.K, (l, g1) => new { l, g1 })
            .GroupJoin(db.Table<H20OoRight>(), x => x.l.K2, r => r.K, (x, g2) => new { x.l, x.g1, g2 })
            .SelectMany(x => x.g2.DefaultIfEmpty(), (x, r2) => new { x.l, x.g1, r2 })
            .SelectMany(x => x.g1, (x, r1) => new { x.l.Id, R2 = x.r2 == null ? -1 : x.r2.Id, R1 = r1.Id })
            .OrderBy(t => t.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OutOfOrderFlattenCrossesGroupFilters()
    {
        using TestDatabase db = new();
        var (ls, rs) = Seed(db);

        var expected = ls
            .GroupJoin(rs, l => l.K1, r => r.K, (l, g1) => new { l, g1 })
            .GroupJoin(rs, x => x.l.K2, r => r.K, (x, g2) => new { x.l, x.g1, g2 })
            .SelectMany(x => x.g2.Where(r2 => r2.Id > 3).DefaultIfEmpty(), (x, r2) => new { x.l, x.g1, r2 })
            .SelectMany(x => x.g1, (x, r1) => new { x.l.Id, R2 = x.r2 == null ? -1 : x.r2.Id, R1 = r1.Id })
            .OrderBy(t => t.Id)
            .ToList();

        var actual = db.Table<H20OoLeft>()
            .GroupJoin(db.Table<H20OoRight>(), l => l.K1, r => r.K, (l, g1) => new { l, g1 })
            .GroupJoin(db.Table<H20OoRight>(), x => x.l.K2, r => r.K, (x, g2) => new { x.l, x.g1, g2 })
            .SelectMany(x => x.g2.Where(r2 => r2.Id > 3).DefaultIfEmpty(), (x, r2) => new { x.l, x.g1, r2 })
            .SelectMany(x => x.g1, (x, r1) => new { x.l.Id, R2 = x.r2 == null ? -1 : x.r2.Id, R1 = r1.Id })
            .OrderBy(t => t.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InOrderFlattenMatchesLinq()
    {
        using TestDatabase db = new();
        var (ls, rs) = Seed(db);

        var expected = ls
            .GroupJoin(rs, l => l.K1, r => r.K, (l, g1) => new { l, g1 })
            .GroupJoin(rs, x => x.l.K2, r => r.K, (x, g2) => new { x.l, x.g1, g2 })
            .SelectMany(x => x.g1.DefaultIfEmpty(), (x, r1) => new { x.l, x.g2, r1 })
            .SelectMany(x => x.g2, (x, r2) => new { x.l.Id, R1 = x.r1 == null ? -1 : x.r1.Id, R2 = r2.Id })
            .OrderBy(t => t.Id)
            .ToList();

        var actual = db.Table<H20OoLeft>()
            .GroupJoin(db.Table<H20OoRight>(), l => l.K1, r => r.K, (l, g1) => new { l, g1 })
            .GroupJoin(db.Table<H20OoRight>(), x => x.l.K2, r => r.K, (x, g2) => new { x.l, x.g1, g2 })
            .SelectMany(x => x.g1.DefaultIfEmpty(), (x, r1) => new { x.l, x.g2, r1 })
            .SelectMany(x => x.g2, (x, r2) => new { x.l.Id, R1 = x.r1 == null ? -1 : x.r1.Id, R2 = r2.Id })
            .OrderBy(t => t.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DifferentTypesOutOfOrderFlattenMatchesLinq()
    {
        using TestDatabase db = new();
        var (ls, rs) = Seed(db);
        db.Table<H20OoExtra>().Schema.CreateTable();
        List<H20OoExtra> es = new()
        {
            new H20OoExtra { Id = 1, K = 100 },
            new H20OoExtra { Id = 2, K = 200 },
        };
        db.Table<H20OoExtra>().AddRange(es);

        var expected = ls
            .GroupJoin(rs, l => l.K1, r => r.K, (l, g1) => new { l, g1 })
            .GroupJoin(es, x => x.l.K2, e => e.K, (x, g2) => new { x.l, x.g1, g2 })
            .SelectMany(x => x.g2.DefaultIfEmpty(), (x, e) => new { x.l, x.g1, e })
            .SelectMany(x => x.g1, (x, r) => new { x.l.Id, E = x.e == null ? -1 : x.e.Id, R = r.Id })
            .OrderBy(t => t.Id)
            .ToList();

        var actual = db.Table<H20OoLeft>()
            .GroupJoin(db.Table<H20OoRight>(), l => l.K1, r => r.K, (l, g1) => new { l, g1 })
            .GroupJoin(db.Table<H20OoExtra>(), x => x.l.K2, e => e.K, (x, g2) => new { x.l, x.g1, g2 })
            .SelectMany(x => x.g2.DefaultIfEmpty(), (x, e) => new { x.l, x.g1, e })
            .SelectMany(x => x.g1, (x, r) => new { x.l.Id, E = x.e == null ? -1 : x.e.Id, R = r.Id })
            .OrderBy(t => t.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }
}

[Table("H20JoinOoLefts")]
public class H20OoLeft
{
    [Key]
    public int Id { get; set; }

    public int K1 { get; set; }

    public int K2 { get; set; }

    public required string Tag { get; set; }
}

[Table("H20JoinOoRights")]
public class H20OoRight
{
    [Key]
    public int Id { get; set; }

    public int K { get; set; }

    public required string RTag { get; set; }
}

[Table("H20JoinOoExtras")]
public class H20OoExtra
{
    [Key]
    public int Id { get; set; }

    public int K { get; set; }
}
