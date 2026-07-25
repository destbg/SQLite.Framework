using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21fRepeatedNameLefts")]
public class H21fRepeatedNameLeft
{
    [Key]
    public int Id { get; set; }

    public int K1 { get; set; }

    public int K2 { get; set; }
}

[Table("H21fRepeatedNameRights")]
public class H21fRepeatedNameRight
{
    [Key]
    public int Id { get; set; }

    public int K { get; set; }
}

public class GroupJoinRepeatedGroupNameTests
{
    [Fact]
    public void SecondGroupJoinReusingTheGroupNameLeftFlattensItsOwnGroup()
    {
        using TestDatabase db = new();
        (List<H21fRepeatedNameLeft> ls, List<H21fRepeatedNameRight> rs) = Seed(db);

        var expected = ls
            .GroupJoin(rs, l => l.K1, r => r.K, (l, g) => new { l, g })
            .GroupJoin(rs, x => x.l.K2, r => r.K, (x, g) => new { x.l, g })
            .SelectMany(y => y.g.DefaultIfEmpty(), (y, r2) => new { LId = y.l.Id, RId = r2 == null ? -1 : r2.Id })
            .OrderBy(t => t.LId)
            .ThenBy(t => t.RId)
            .ToList();

        var actual = db.Table<H21fRepeatedNameLeft>()
            .GroupJoin(db.Table<H21fRepeatedNameRight>(), l => l.K1, r => r.K, (l, g) => new { l, g })
            .GroupJoin(db.Table<H21fRepeatedNameRight>(), x => x.l.K2, r => r.K, (x, g) => new { x.l, g })
            .SelectMany(y => y.g.DefaultIfEmpty(), (y, r2) => new { LId = y.l.Id, RId = r2 == null ? -1 : r2.Id })
            .ToList()
            .OrderBy(t => t.LId)
            .ThenBy(t => t.RId)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SecondGroupJoinReusingTheGroupNameInnerFlattensItsOwnGroup()
    {
        using TestDatabase db = new();
        (List<H21fRepeatedNameLeft> ls, List<H21fRepeatedNameRight> rs) = Seed(db);

        var expected = ls
            .GroupJoin(rs, l => l.K1, r => r.K, (l, g) => new { l, g })
            .GroupJoin(rs, x => x.l.K2, r => r.K, (x, g) => new { x.l, g })
            .SelectMany(y => y.g, (y, r2) => new { LId = y.l.Id, RId = r2.Id })
            .OrderBy(t => t.LId)
            .ThenBy(t => t.RId)
            .ToList();

        var actual = db.Table<H21fRepeatedNameLeft>()
            .GroupJoin(db.Table<H21fRepeatedNameRight>(), l => l.K1, r => r.K, (l, g) => new { l, g })
            .GroupJoin(db.Table<H21fRepeatedNameRight>(), x => x.l.K2, r => r.K, (x, g) => new { x.l, g })
            .SelectMany(y => y.g, (y, r2) => new { LId = y.l.Id, RId = r2.Id })
            .ToList()
            .OrderBy(t => t.LId)
            .ThenBy(t => t.RId)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static (List<H21fRepeatedNameLeft> Ls, List<H21fRepeatedNameRight> Rs) Seed(TestDatabase db)
    {
        db.Table<H21fRepeatedNameLeft>().Schema.CreateTable();
        db.Table<H21fRepeatedNameRight>().Schema.CreateTable();

        List<H21fRepeatedNameLeft> ls =
        [
            new H21fRepeatedNameLeft { Id = 1, K1 = 10, K2 = 100 },
            new H21fRepeatedNameLeft { Id = 2, K1 = 20, K2 = 200 },
            new H21fRepeatedNameLeft { Id = 3, K1 = 30, K2 = 300 }
        ];
        List<H21fRepeatedNameRight> rs =
        [
            new H21fRepeatedNameRight { Id = 1, K = 10 },
            new H21fRepeatedNameRight { Id = 2, K = 30 },
            new H21fRepeatedNameRight { Id = 3, K = 100 },
            new H21fRepeatedNameRight { Id = 4, K = 200 }
        ];

        db.Table<H21fRepeatedNameLeft>().AddRange(ls);
        db.Table<H21fRepeatedNameRight>().AddRange(rs);
        return (ls, rs);
    }
}
