using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21fNameCollisionLefts")]
public class H21fNameCollisionLeft
{
    [Key]
    public int Id { get; set; }

    public int Key { get; set; }
}

[Table("H21fNameCollisionRights")]
public class H21fNameCollisionRight
{
    [Key]
    public int Id { get; set; }

    public int Key { get; set; }
}

[Table("H21fNameCollisionExtras")]
public class H21fNameCollisionExtra
{
    [Key]
    public int Id { get; set; }
}

public class GroupJoinGroupNameCollisionSourceTests
{
    [Fact]
    public void CapturedTableSourceNamedLikeTheGroupMemberAfterFlatten()
    {
        using TestDatabase db = new();
        (List<H21fNameCollisionLeft> ls, List<H21fNameCollisionRight> rs, List<H21fNameCollisionExtra> es) = Seed(db);
        SQLiteTable<H21fNameCollisionExtra> g = db.Table<H21fNameCollisionExtra>();

        var expected = ls
            .GroupJoin(rs, l => l.Key, r => r.Key, (l, grp) => new { l, g = grp })
            .SelectMany(x => x.g.DefaultIfEmpty(), (x, r) => new { x.l, r })
            .SelectMany(x => es, (x, e) => new { LId = x.l.Id, RId = x.r == null ? -1 : x.r.Id, EId = e.Id })
            .OrderBy(t => t.LId)
            .ThenBy(t => t.RId)
            .ThenBy(t => t.EId)
            .ToList();

        var actual = db.Table<H21fNameCollisionLeft>()
            .GroupJoin(db.Table<H21fNameCollisionRight>(), l => l.Key, r => r.Key, (l, grp) => new { l, g = grp })
            .SelectMany(x => x.g.DefaultIfEmpty(), (x, r) => new { x.l, r })
            .SelectMany(x => g, (x, e) => new { LId = x.l.Id, RId = x.r == null ? -1 : x.r.Id, EId = e.Id })
            .ToList()
            .OrderBy(t => t.LId)
            .ThenBy(t => t.RId)
            .ThenBy(t => t.EId)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedTableSourceNamedLikeTheGroupMemberBeforeFlatten()
    {
        using TestDatabase db = new();
        (List<H21fNameCollisionLeft> ls, List<H21fNameCollisionRight> rs, List<H21fNameCollisionExtra> es) = Seed(db);
        SQLiteTable<H21fNameCollisionExtra> g = db.Table<H21fNameCollisionExtra>();

        var expected = ls
            .GroupJoin(rs, l => l.Key, r => r.Key, (l, grp) => new { l, g = grp })
            .SelectMany(x => es, (x, e) => new { x.l, x.g, e })
            .SelectMany(x => x.g.DefaultIfEmpty(), (x, r) => new { LId = x.l.Id, EId = x.e.Id, RId = r == null ? -1 : r.Id })
            .OrderBy(t => t.LId)
            .ThenBy(t => t.EId)
            .ThenBy(t => t.RId)
            .ToList();

        var actual = db.Table<H21fNameCollisionLeft>()
            .GroupJoin(db.Table<H21fNameCollisionRight>(), l => l.Key, r => r.Key, (l, grp) => new { l, g = grp })
            .SelectMany(x => g, (x, e) => new { x.l, x.g, e })
            .SelectMany(x => x.g.DefaultIfEmpty(), (x, r) => new { LId = x.l.Id, EId = x.e.Id, RId = r == null ? -1 : r.Id })
            .ToList()
            .OrderBy(t => t.LId)
            .ThenBy(t => t.EId)
            .ThenBy(t => t.RId)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedCteSourceNamedLikeTheGroupMemberAfterFlatten()
    {
        using TestDatabase db = new();
        (List<H21fNameCollisionLeft> ls, List<H21fNameCollisionRight> rs, List<H21fNameCollisionExtra> es) = Seed(db);
        SQLiteCte<H21fNameCollisionExtra> g = db.With(() => db.Table<H21fNameCollisionExtra>());

        var expected = ls
            .GroupJoin(rs, l => l.Key, r => r.Key, (l, grp) => new { l, g = grp })
            .SelectMany(x => x.g.DefaultIfEmpty(), (x, r) => new { x.l, r })
            .SelectMany(x => es, (x, e) => new { LId = x.l.Id, RId = x.r == null ? -1 : x.r.Id, EId = e.Id })
            .OrderBy(t => t.LId)
            .ThenBy(t => t.RId)
            .ThenBy(t => t.EId)
            .ToList();

        var actual = db.Table<H21fNameCollisionLeft>()
            .GroupJoin(db.Table<H21fNameCollisionRight>(), l => l.Key, r => r.Key, (l, grp) => new { l, g = grp })
            .SelectMany(x => x.g.DefaultIfEmpty(), (x, r) => new { x.l, r })
            .SelectMany(x => g, (x, e) => new { LId = x.l.Id, RId = x.r == null ? -1 : x.r.Id, EId = e.Id })
            .ToList()
            .OrderBy(t => t.LId)
            .ThenBy(t => t.RId)
            .ThenBy(t => t.EId)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static (List<H21fNameCollisionLeft> Ls, List<H21fNameCollisionRight> Rs, List<H21fNameCollisionExtra> Es) Seed(TestDatabase db)
    {
        db.Table<H21fNameCollisionLeft>().Schema.CreateTable();
        db.Table<H21fNameCollisionRight>().Schema.CreateTable();
        db.Table<H21fNameCollisionExtra>().Schema.CreateTable();

        List<H21fNameCollisionLeft> ls =
        [
            new H21fNameCollisionLeft { Id = 1, Key = 10 },
            new H21fNameCollisionLeft { Id = 2, Key = 20 },
            new H21fNameCollisionLeft { Id = 3, Key = 30 }
        ];
        List<H21fNameCollisionRight> rs =
        [
            new H21fNameCollisionRight { Id = 1, Key = 10 },
            new H21fNameCollisionRight { Id = 2, Key = 10 },
            new H21fNameCollisionRight { Id = 3, Key = 30 }
        ];
        List<H21fNameCollisionExtra> es =
        [
            new H21fNameCollisionExtra { Id = 1 },
            new H21fNameCollisionExtra { Id = 2 }
        ];

        db.Table<H21fNameCollisionLeft>().AddRange(ls);
        db.Table<H21fNameCollisionRight>().AddRange(rs);
        db.Table<H21fNameCollisionExtra>().AddRange(es);
        return (ls, rs, es);
    }
}
