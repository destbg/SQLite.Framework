using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22jSwapLefts")]
public class H22jSwapLeft
{
    [Key]
    public int Id { get; set; }

    public int K1 { get; set; }

    public int K2 { get; set; }

    public int K3 { get; set; }
}

[Table("H22jSwapRights")]
public class H22jSwapRight
{
    [Key]
    public int Id { get; set; }

    public int K { get; set; }
}

[Table("H22jSwapSides")]
public class H22jSwapSide
{
    [Key]
    public int Id { get; set; }

    public int K { get; set; }
}

public class GroupJoinSwappedGroupMemberNameFlattenTests
{
    [Fact]
    public void SwappedGroupMemberNamesFlattenTheGroupTheNameNowHolds()
    {
        using TestDatabase db = new();
        (List<H22jSwapLeft> ls, List<H22jSwapRight> rs, List<H22jSwapSide> ss) = Seed(db);

        var expected = ls
            .GroupJoin(rs, l => l.K1, r => r.K, (l, g) => new { l, a = g })
            .GroupJoin(rs, x => x.l.K2, r => r.K, (x, g) => new { x.l, x.a, b = g })
            .Join(ss, y => y.l.K3, s => s.K, (y, s) => new { y.l, a = y.b, b = y.a, s })
            .SelectMany(z => z.a, (z, ra) => new { z.l, z.b, z.s, ra })
            .SelectMany(z => z.b.DefaultIfEmpty(), (z, rb) => new
            {
                LId = z.l.Id,
                SId = z.s.Id,
                A = z.ra.Id,
                B = rb == null ? -1 : rb.Id
            })
            .OrderBy(t => t.LId)
            .ThenBy(t => t.A)
            .ThenBy(t => t.B)
            .ToList();

        var actual = db.Table<H22jSwapLeft>()
            .GroupJoin(db.Table<H22jSwapRight>(), l => l.K1, r => r.K, (l, g) => new { l, a = g })
            .GroupJoin(db.Table<H22jSwapRight>(), x => x.l.K2, r => r.K, (x, g) => new { x.l, x.a, b = g })
            .Join(db.Table<H22jSwapSide>(), y => y.l.K3, s => s.K, (y, s) => new { y.l, a = y.b, b = y.a, s })
            .SelectMany(z => z.a, (z, ra) => new { z.l, z.b, z.s, ra })
            .SelectMany(z => z.b.DefaultIfEmpty(), (z, rb) => new
            {
                LId = z.l.Id,
                SId = z.s.Id,
                A = z.ra.Id,
                B = rb == null ? -1 : rb.Id
            })
            .ToList()
            .OrderBy(t => t.LId)
            .ThenBy(t => t.A)
            .ThenBy(t => t.B)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GroupMovedOntoAnotherGroupMemberNameFlattensTheGroupTheNameNowHolds()
    {
        using TestDatabase db = new();
        (List<H22jSwapLeft> ls, List<H22jSwapRight> rs, List<H22jSwapSide> ss) = Seed(db);

        var expected = ls
            .GroupJoin(rs, l => l.K1, r => r.K, (l, g) => new { l, a = g })
            .GroupJoin(rs, x => x.l.K2, r => r.K, (x, g) => new { x.l, x.a, b = g })
            .Join(ss, y => y.l.K3, s => s.K, (y, s) => new { y.l, b = y.a, kept = y.b, s })
            .SelectMany(z => z.b, (z, rb) => new { z.l, z.kept, z.s, rb })
            .SelectMany(z => z.kept.DefaultIfEmpty(), (z, rk) => new
            {
                LId = z.l.Id,
                SId = z.s.Id,
                B = z.rb.Id,
                K = rk == null ? -1 : rk.Id
            })
            .OrderBy(t => t.LId)
            .ThenBy(t => t.B)
            .ThenBy(t => t.K)
            .ToList();

        var actual = db.Table<H22jSwapLeft>()
            .GroupJoin(db.Table<H22jSwapRight>(), l => l.K1, r => r.K, (l, g) => new { l, a = g })
            .GroupJoin(db.Table<H22jSwapRight>(), x => x.l.K2, r => r.K, (x, g) => new { x.l, x.a, b = g })
            .Join(db.Table<H22jSwapSide>(), y => y.l.K3, s => s.K, (y, s) => new { y.l, b = y.a, kept = y.b, s })
            .SelectMany(z => z.b, (z, rb) => new { z.l, z.kept, z.s, rb })
            .SelectMany(z => z.kept.DefaultIfEmpty(), (z, rk) => new
            {
                LId = z.l.Id,
                SId = z.s.Id,
                B = z.rb.Id,
                K = rk == null ? -1 : rk.Id
            })
            .ToList()
            .OrderBy(t => t.LId)
            .ThenBy(t => t.B)
            .ThenBy(t => t.K)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static (List<H22jSwapLeft> Ls, List<H22jSwapRight> Rs, List<H22jSwapSide> Ss) Seed(TestDatabase db)
    {
        db.Table<H22jSwapLeft>().Schema.CreateTable();
        db.Table<H22jSwapRight>().Schema.CreateTable();
        db.Table<H22jSwapSide>().Schema.CreateTable();

        List<H22jSwapLeft> ls =
        [
            new H22jSwapLeft { Id = 1, K1 = 10, K2 = 20, K3 = 1 },
            new H22jSwapLeft { Id = 2, K1 = 11, K2 = 20, K3 = 1 },
            new H22jSwapLeft { Id = 3, K1 = 10, K2 = 21, K3 = 1 }
        ];
        List<H22jSwapRight> rs =
        [
            new H22jSwapRight { Id = 100, K = 10 },
            new H22jSwapRight { Id = 200, K = 20 }
        ];
        List<H22jSwapSide> ss =
        [
            new H22jSwapSide { Id = 1, K = 1 }
        ];

        db.Table<H22jSwapLeft>().AddRange(ls);
        db.Table<H22jSwapRight>().AddRange(rs);
        db.Table<H22jSwapSide>().AddRange(ss);
        return (ls, rs, ss);
    }
}
