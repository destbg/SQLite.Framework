using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21fCarryLefts")]
public class H21fCarryLeft
{
    [Key]
    public int Id { get; set; }

    public int K1 { get; set; }

    public int K2 { get; set; }
}

[Table("H21fCarryRights")]
public class H21fCarryRight
{
    [Key]
    public int Id { get; set; }

    public int K { get; set; }
}

[Table("H21fCarryExtras")]
public class H21fCarryExtra
{
    [Key]
    public int Id { get; set; }

    public int K { get; set; }
}

public class GroupJoinCarriedGroupMemberRenameTests
{
    [Fact]
    public void RenamedCarriedGroupMemberLeftFlattensMatchesLinq()
    {
        using TestDatabase db = new();
        (List<H21fCarryLeft> ls, List<H21fCarryRight> rs, List<H21fCarryExtra> es) = Seed(db);

        var expected = ls
            .GroupJoin(rs, l => l.K1, r => r.K, (l, g) => new { l, g })
            .GroupJoin(es, x => x.l.K2, e => e.K, (x, g2) => new { x.l, carried = x.g, g2 })
            .SelectMany(y => y.g2.DefaultIfEmpty(), (y, e) => new { y.l, y.carried, e })
            .SelectMany(y => y.carried.DefaultIfEmpty(), (y, r) => new
            {
                LId = y.l.Id,
                EId = y.e == null ? -1 : y.e.Id,
                RId = r == null ? -1 : r.Id
            })
            .OrderBy(t => t.LId)
            .ThenBy(t => t.EId)
            .ThenBy(t => t.RId)
            .ToList();

        var actual = db.Table<H21fCarryLeft>()
            .GroupJoin(db.Table<H21fCarryRight>(), l => l.K1, r => r.K, (l, g) => new { l, g })
            .GroupJoin(db.Table<H21fCarryExtra>(), x => x.l.K2, e => e.K, (x, g2) => new { x.l, carried = x.g, g2 })
            .SelectMany(y => y.g2.DefaultIfEmpty(), (y, e) => new { y.l, y.carried, e })
            .SelectMany(y => y.carried.DefaultIfEmpty(), (y, r) => new
            {
                LId = y.l.Id,
                EId = y.e == null ? -1 : y.e.Id,
                RId = r == null ? -1 : r.Id
            })
            .ToList()
            .OrderBy(t => t.LId)
            .ThenBy(t => t.EId)
            .ThenBy(t => t.RId)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RenamedCarriedGroupMemberInnerFlattensMatchesLinq()
    {
        using TestDatabase db = new();
        (List<H21fCarryLeft> ls, List<H21fCarryRight> rs, List<H21fCarryExtra> es) = Seed(db);

        var expected = ls
            .GroupJoin(rs, l => l.K1, r => r.K, (l, g) => new { l, g })
            .GroupJoin(es, x => x.l.K2, e => e.K, (x, g2) => new { x.l, carried = x.g, g2 })
            .SelectMany(y => y.g2.DefaultIfEmpty(), (y, e) => new { y.l, y.carried, e })
            .SelectMany(y => y.carried, (y, r) => new
            {
                LId = y.l.Id,
                EId = y.e == null ? -1 : y.e.Id,
                RId = r.Id
            })
            .OrderBy(t => t.LId)
            .ThenBy(t => t.EId)
            .ThenBy(t => t.RId)
            .ToList();

        var actual = db.Table<H21fCarryLeft>()
            .GroupJoin(db.Table<H21fCarryRight>(), l => l.K1, r => r.K, (l, g) => new { l, g })
            .GroupJoin(db.Table<H21fCarryExtra>(), x => x.l.K2, e => e.K, (x, g2) => new { x.l, carried = x.g, g2 })
            .SelectMany(y => y.g2.DefaultIfEmpty(), (y, e) => new { y.l, y.carried, e })
            .SelectMany(y => y.carried, (y, r) => new
            {
                LId = y.l.Id,
                EId = y.e == null ? -1 : y.e.Id,
                RId = r.Id
            })
            .ToList()
            .OrderBy(t => t.LId)
            .ThenBy(t => t.EId)
            .ThenBy(t => t.RId)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static (List<H21fCarryLeft> Ls, List<H21fCarryRight> Rs, List<H21fCarryExtra> Es) Seed(TestDatabase db)
    {
        db.Table<H21fCarryLeft>().Schema.CreateTable();
        db.Table<H21fCarryRight>().Schema.CreateTable();
        db.Table<H21fCarryExtra>().Schema.CreateTable();

        List<H21fCarryLeft> ls =
        [
            new H21fCarryLeft { Id = 1, K1 = 10, K2 = 100 },
            new H21fCarryLeft { Id = 2, K1 = 20, K2 = 200 },
            new H21fCarryLeft { Id = 3, K1 = 30, K2 = 300 }
        ];
        List<H21fCarryRight> rs =
        [
            new H21fCarryRight { Id = 1, K = 10 },
            new H21fCarryRight { Id = 2, K = 10 },
            new H21fCarryRight { Id = 3, K = 30 }
        ];
        List<H21fCarryExtra> es =
        [
            new H21fCarryExtra { Id = 1, K = 100 },
            new H21fCarryExtra { Id = 2, K = 300 }
        ];

        db.Table<H21fCarryLeft>().AddRange(ls);
        db.Table<H21fCarryRight>().AddRange(rs);
        db.Table<H21fCarryExtra>().AddRange(es);
        return (ls, rs, es);
    }
}
