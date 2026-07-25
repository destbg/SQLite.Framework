using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21GjLefts")]
public class H21GjLeft
{
    [Key]
    public int Id { get; set; }

    public int Key { get; set; }
}

[Table("H21GjRights")]
public class H21GjRight
{
    [Key]
    public int Id { get; set; }

    public int Key { get; set; }
}

public class H21GjPair
{
    public H21GjLeft L { get; set; } = new();

    public IEnumerable<H21GjRight> G { get; set; } = [];
}

public class GroupJoinGroupMemberNameShapeTests
{
    private static (List<H21GjLeft> Ls, List<H21GjRight> Rs) Seed(TestDatabase db)
    {
        db.Table<H21GjLeft>().Schema.CreateTable();
        db.Table<H21GjRight>().Schema.CreateTable();

        List<H21GjLeft> ls =
        [
            new H21GjLeft { Id = 1, Key = 10 },
            new H21GjLeft { Id = 2, Key = 20 },
            new H21GjLeft { Id = 3, Key = 30 },
        ];
        List<H21GjRight> rs =
        [
            new H21GjRight { Id = 1, Key = 10 },
            new H21GjRight { Id = 2, Key = 10 },
            new H21GjRight { Id = 3, Key = 30 },
        ];

        db.Table<H21GjLeft>().AddRange(ls);
        db.Table<H21GjRight>().AddRange(rs);
        return (ls, rs);
    }

    [Fact]
    public void MemberInitSelectorFlattensByGroupMemberName()
    {
        using TestDatabase db = new();
        var (ls, rs) = Seed(db);

        var expected = (from l in ls
            join r in rs on l.Key equals r.Key into g
            from r in g.DefaultIfEmpty()
            orderby l.Id, r == null ? -1 : r.Id
            select new { l.Id, RId = r == null ? -1 : r.Id }).ToList();

        var actual = db.Table<H21GjLeft>()
            .GroupJoin(db.Table<H21GjRight>(), l => l.Key, r => r.Key, (l, g) => new H21GjPair { L = l, G = g })
            .SelectMany(p => p.G.DefaultIfEmpty(), (p, r) => new { p.L.Id, RId = r == null ? -1 : r.Id })
            .ToList()
            .OrderBy(x => x.Id)
            .ThenBy(x => x.RId)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TupleSelectorWithoutMemberNamesCannotBeFlattened()
    {
        using TestDatabase db = new();
        Seed(db);

        var query = db.Table<H21GjLeft>()
            .GroupJoin(db.Table<H21GjRight>(), l => l.Key, r => r.Key, (l, g) => new ValueTuple<H21GjLeft, IEnumerable<H21GjRight>>(l, g))
            .SelectMany(p => p.Item2.DefaultIfEmpty(), (p, r) => new { p.Item1.Id, RId = r == null ? -1 : r.Id });

        Assert.Throws<NotSupportedException>(() => query.ToList());
    }

    [Fact]
    public void MemberInitSelectorWithoutTheGroupMemberStaysUnflattened()
    {
        using TestDatabase db = new();
        Seed(db);

        var query = db.Table<H21GjLeft>()
            .GroupJoin(db.Table<H21GjRight>(), l => l.Key, r => r.Key, (l, g) => new H21GjPair { L = l });

        Assert.Throws<NotSupportedException>(() => query.ToList());
    }
}
