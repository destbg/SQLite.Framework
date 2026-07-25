using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class JoinAfterEntityCarryingSelectTests
{
    private static (List<QjcLeft> Ls, List<QjcRight> Rs) Seed(TestDatabase db)
    {
        db.Table<QjcLeft>().Schema.CreateTable();
        db.Table<QjcRight>().Schema.CreateTable();

        List<QjcLeft> ls = new()
        {
            new QjcLeft { Id = 1, Key = 10, Tag = "a" },
            new QjcLeft { Id = 2, Key = 20, Tag = "b" },
            new QjcLeft { Id = 3, Key = 30, Tag = "c" },
        };
        List<QjcRight> rs = new()
        {
            new QjcRight { Id = 1, Key = 10, RTag = "A" },
            new QjcRight { Id = 2, Key = 10, RTag = "B" },
            new QjcRight { Id = 3, Key = 30, RTag = "C" },
        };

        db.Table<QjcLeft>().AddRange(ls);
        db.Table<QjcRight>().AddRange(rs);
        return (ls, rs);
    }

    [Fact]
    public void ResultSelectorReadsCarriedEntityMember()
    {
        using TestDatabase db = new();
        var (ls, rs) = Seed(db);

        var expected = ls
            .Select(l => new { E = l, K = l.Key })
            .Join(rs, p => p.K, r => r.Key, (p, r) => new { T = p.E.Tag, R = r.RTag })
            .OrderBy(x => x.T).ThenBy(x => x.R)
            .ToList();

        var actual = db.Table<QjcLeft>()
            .Select(l => new { E = l, K = l.Key })
            .Join(db.Table<QjcRight>(), p => p.K, r => r.Key, (p, r) => new { T = p.E.Tag, R = r.RTag })
            .OrderBy(x => x.T).ThenBy(x => x.R)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void KeySelectorReadsCarriedEntityMember()
    {
        using TestDatabase db = new();
        var (ls, rs) = Seed(db);

        var expected = ls
            .Select(l => new { E = l, K = l.Key })
            .Join(rs, p => p.E.Key, r => r.Key, (p, r) => new { p.K, RId = r.Id })
            .OrderBy(x => x.K).ThenBy(x => x.RId)
            .ToList();

        var actual = db.Table<QjcLeft>()
            .Select(l => new { E = l, K = l.Key })
            .Join(db.Table<QjcRight>(), p => p.E.Key, r => r.Key, (p, r) => new { p.K, RId = r.Id })
            .OrderBy(x => x.K).ThenBy(x => x.RId)
            .ToList();

        Assert.Equal(expected, actual);
    }
}

[Table("QjcLefts")]
public class QjcLeft
{
    [Key]
    public int Id { get; set; }

    public int Key { get; set; }

    public required string Tag { get; set; }
}

[Table("QjcRights")]
public class QjcRight
{
    [Key]
    public int Id { get; set; }

    public int Key { get; set; }

    public required string RTag { get; set; }
}
