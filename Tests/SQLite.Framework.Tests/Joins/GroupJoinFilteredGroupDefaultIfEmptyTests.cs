using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class GroupJoinFilteredGroupDefaultIfEmptyTests
{
    private static (List<QjfLeft> Ls, List<QjfRight> Rs) Seed(TestDatabase db)
    {
        db.Table<QjfLeft>().Schema.CreateTable();
        db.Table<QjfRight>().Schema.CreateTable();

        List<QjfLeft> ls = new()
        {
            new QjfLeft { Id = 1, Key = 10 },
            new QjfLeft { Id = 2, Key = 20 },
            new QjfLeft { Id = 3, Key = 30 },
        };
        List<QjfRight> rs = new()
        {
            new QjfRight { Id = 1, Key = 10 },
            new QjfRight { Id = 2, Key = 10 },
            new QjfRight { Id = 3, Key = 30 },
        };

        db.Table<QjfLeft>().AddRange(ls);
        db.Table<QjfRight>().AddRange(rs);
        return (ls, rs);
    }

    [Fact]
    public void FilteredGroupBeforeDefaultIfEmpty()
    {
        using TestDatabase db = new();
        var (ls, rs) = Seed(db);

        var expected = (from l in ls
            join r in rs on l.Key equals r.Key into g
            from r in g.Where(x => x.Id > 1).DefaultIfEmpty()
            orderby l.Id, r == null ? -1 : r.Id
            select new { l.Id, RId = r == null ? -1 : r.Id }).ToList();

        var actual = (from l in db.Table<QjfLeft>()
            join r in db.Table<QjfRight>() on l.Key equals r.Key into g
            from r in g.Where(x => x.Id > 1).DefaultIfEmpty()
            orderby l.Id, r == null ? -1 : r.Id
            select new { l.Id, RId = r == null ? -1 : r.Id }).ToList();

        Assert.Equal(expected, actual);
    }
}

[Table("QjfLefts")]
public class QjfLeft
{
    [Key]
    public int Id { get; set; }

    public int Key { get; set; }
}

[Table("QjfRights")]
public class QjfRight
{
    [Key]
    public int Id { get; set; }

    public int Key { get; set; }
}
