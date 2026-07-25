using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21fGroupPathLefts")]
public class H21fGroupPathLeft
{
    [Key]
    public int Id { get; set; }

    public int Key { get; set; }
}

[Table("H21fGroupPathRights")]
public class H21fGroupPathRight
{
    [Key]
    public int Id { get; set; }

    public int Key { get; set; }
}

[Table("H21fGroupPathExtras")]
public class H21fGroupPathExtra
{
    [Key]
    public int Id { get; set; }

    public int Key { get; set; }
}

public class GroupJoinNestedGroupPathFlattenTests
{
    [Fact]
    public void InnerJoinBetweenGroupJoinAndLeftFlattenMatchesLinq()
    {
        using TestDatabase db = new();
        (List<H21fGroupPathLeft> ls, List<H21fGroupPathRight> rs, List<H21fGroupPathExtra> es) = Seed(db);

        var expected = (from l in ls
            join r in rs on l.Key equals r.Key into g
            join e in es on l.Key equals e.Key
            from r2 in g.DefaultIfEmpty()
            select new { l.Id, EId = e.Id, RId = r2 == null ? -1 : r2.Id })
            .OrderBy(t => t.Id)
            .ThenBy(t => t.EId)
            .ThenBy(t => t.RId)
            .ToList();

        var actual = (from l in db.Table<H21fGroupPathLeft>()
            join r in db.Table<H21fGroupPathRight>() on l.Key equals r.Key into g
            join e in db.Table<H21fGroupPathExtra>() on l.Key equals e.Key
            from r2 in g.DefaultIfEmpty()
            select new { l.Id, EId = e.Id, RId = r2 == null ? -1 : r2.Id })
            .ToList()
            .OrderBy(t => t.Id)
            .ThenBy(t => t.EId)
            .ThenBy(t => t.RId)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InnerJoinBetweenGroupJoinAndBareFlattenMatchesLinq()
    {
        using TestDatabase db = new();
        (List<H21fGroupPathLeft> ls, List<H21fGroupPathRight> rs, List<H21fGroupPathExtra> es) = Seed(db);

        var expected = (from l in ls
            join r in rs on l.Key equals r.Key into g
            join e in es on l.Key equals e.Key
            from r2 in g
            select new { l.Id, EId = e.Id, RId = r2.Id })
            .OrderBy(t => t.Id)
            .ThenBy(t => t.EId)
            .ThenBy(t => t.RId)
            .ToList();

        var actual = (from l in db.Table<H21fGroupPathLeft>()
            join r in db.Table<H21fGroupPathRight>() on l.Key equals r.Key into g
            join e in db.Table<H21fGroupPathExtra>() on l.Key equals e.Key
            from r2 in g
            select new { l.Id, EId = e.Id, RId = r2.Id })
            .ToList()
            .OrderBy(t => t.Id)
            .ThenBy(t => t.EId)
            .ThenBy(t => t.RId)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static (List<H21fGroupPathLeft> Ls, List<H21fGroupPathRight> Rs, List<H21fGroupPathExtra> Es) Seed(TestDatabase db)
    {
        db.Table<H21fGroupPathLeft>().Schema.CreateTable();
        db.Table<H21fGroupPathRight>().Schema.CreateTable();
        db.Table<H21fGroupPathExtra>().Schema.CreateTable();

        List<H21fGroupPathLeft> ls =
        [
            new H21fGroupPathLeft { Id = 1, Key = 10 },
            new H21fGroupPathLeft { Id = 2, Key = 20 },
            new H21fGroupPathLeft { Id = 3, Key = 30 }
        ];
        List<H21fGroupPathRight> rs =
        [
            new H21fGroupPathRight { Id = 1, Key = 10 },
            new H21fGroupPathRight { Id = 2, Key = 10 },
            new H21fGroupPathRight { Id = 3, Key = 30 }
        ];
        List<H21fGroupPathExtra> es =
        [
            new H21fGroupPathExtra { Id = 1, Key = 10 },
            new H21fGroupPathExtra { Id = 2, Key = 20 },
            new H21fGroupPathExtra { Id = 3, Key = 30 }
        ];

        db.Table<H21fGroupPathLeft>().AddRange(ls);
        db.Table<H21fGroupPathRight>().AddRange(rs);
        db.Table<H21fGroupPathExtra>().AddRange(es);
        return (ls, rs, es);
    }
}
