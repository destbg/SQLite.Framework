using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class GroupJoinBareGroupSelectManyTests
{
    private static (List<QjgLeft> Ls, List<QjgRight> Rs) Seed(TestDatabase db)
    {
        db.Table<QjgLeft>().Schema.CreateTable();
        db.Table<QjgRight>().Schema.CreateTable();

        List<QjgLeft> ls = new()
        {
            new QjgLeft { Id = 1, Key = 10, Tag = "a" },
            new QjgLeft { Id = 2, Key = 20, Tag = "b" },
            new QjgLeft { Id = 3, Key = 30, Tag = "c" },
        };
        List<QjgRight> rs = new()
        {
            new QjgRight { Id = 1, Key = 10, RTag = "A" },
            new QjgRight { Id = 2, Key = 10, RTag = "B" },
            new QjgRight { Id = 3, Key = 30, RTag = "C" },
        };

        db.Table<QjgLeft>().AddRange(ls);
        db.Table<QjgRight>().AddRange(rs);
        return (ls, rs);
    }

    [Fact]
    public void BareGroupSelectManyActsAsInnerJoin()
    {
        using TestDatabase db = new();
        var (ls, rs) = Seed(db);

        var expected = (from l in ls
            join r in rs on l.Key equals r.Key into g
            from r in g
            orderby l.Id, r.Id
            select new { l.Id, RId = r.Id }).ToList();

        var actual = (from l in db.Table<QjgLeft>()
            join r in db.Table<QjgRight>() on l.Key equals r.Key into g
            from r in g
            orderby l.Id, r.Id
            select new { l.Id, RId = r.Id }).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BareGroupSelectManyScalarProjection()
    {
        using TestDatabase db = new();
        var (ls, rs) = Seed(db);

        List<string> expected = (from l in ls
            join r in rs on l.Key equals r.Key into g
            from r in g
            orderby l.Id, r.Id
            select l.Tag + r.RTag).ToList();

        List<string> actual = (from l in db.Table<QjgLeft>()
            join r in db.Table<QjgRight>() on l.Key equals r.Key into g
            from r in g
            orderby l.Id, r.Id
            select l.Tag + r.RTag).ToList();

        Assert.Equal(expected, actual);
    }
}

[Table("QjgLefts")]
public class QjgLeft
{
    [Key]
    public int Id { get; set; }

    public int Key { get; set; }

    public required string Tag { get; set; }
}

[Table("QjgRights")]
public class QjgRight
{
    [Key]
    public int Id { get; set; }

    public int Key { get; set; }

    public required string RTag { get; set; }
}
