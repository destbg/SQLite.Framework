using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class GroupJoinFlattenShapeTests
{
    private static (List<QgfsLeft> Ls, List<QgfsRight> Rs, List<QgfsExtra> Es) Seed(TestDatabase db)
    {
        db.Table<QgfsLeft>().Schema.CreateTable();
        db.Table<QgfsRight>().Schema.CreateTable();
        db.Table<QgfsExtra>().Schema.CreateTable();

        List<QgfsLeft> ls = new()
        {
            new QgfsLeft { Id = 1, Key = 10, MinId = 2, Tag = "a" },
            new QgfsLeft { Id = 2, Key = 20, MinId = 1, Tag = "b" },
            new QgfsLeft { Id = 3, Key = 30, MinId = 1, Tag = "c" },
        };
        List<QgfsRight> rs = new()
        {
            new QgfsRight { Id = 1, Key = 10, NVal = null, RTag = "A" },
            new QgfsRight { Id = 2, Key = 10, NVal = 7, RTag = "B" },
            new QgfsRight { Id = 3, Key = 30, NVal = 4, RTag = "C" },
        };
        List<QgfsExtra> es = new()
        {
            new QgfsExtra { Id = 1, Key = 10 },
            new QgfsExtra { Id = 2, Key = 30 },
        };

        db.Table<QgfsLeft>().AddRange(ls);
        db.Table<QgfsRight>().AddRange(rs);
        db.Table<QgfsExtra>().AddRange(es);
        return (ls, rs, es);
    }

    [Fact]
    public void BareGroupFlattenEntityResultSelector()
    {
        using TestDatabase db = new();
        var (ls, rs, _) = Seed(db);

        List<int> expected = (from l in ls
            join r in rs on l.Key equals r.Key into g
            from r in g
            select r).Select(r => r.Id).OrderBy(x => x).ToList();

        List<int> actual = (from l in db.Table<QgfsLeft>()
            join r in db.Table<QgfsRight>() on l.Key equals r.Key into g
            from r in g
            select r).ToList().Select(r => r.Id).OrderBy(x => x).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BareGroupFlattenProjectionResultSelector()
    {
        using TestDatabase db = new();
        var (ls, rs, _) = Seed(db);

        var expected = (from l in ls
            join r in rs on l.Key equals r.Key into g
            from r in g
            select new { l.Id, r.RTag }).OrderBy(x => x.Id).ThenBy(x => x.RTag).ToList();

        var actual = (from l in db.Table<QgfsLeft>()
            join r in db.Table<QgfsRight>() on l.Key equals r.Key into g
            from r in g
            select new { l.Id, r.RTag }).ToList().OrderBy(x => x.Id).ThenBy(x => x.RTag).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BareGroupFlattenScalarResultSelector()
    {
        using TestDatabase db = new();
        var (ls, rs, _) = Seed(db);

        List<string> expected = (from l in ls
            join r in rs on l.Key equals r.Key into g
            from r in g
            select l.Tag + r.RTag).OrderBy(x => x, StringComparer.Ordinal).ToList();

        List<string> actual = (from l in db.Table<QgfsLeft>()
            join r in db.Table<QgfsRight>() on l.Key equals r.Key into g
            from r in g
            select l.Tag + r.RTag).ToList().OrderBy(x => x, StringComparer.Ordinal).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BareGroupWithFilterActsAsFilteredInnerJoin()
    {
        using TestDatabase db = new();
        var (ls, rs, _) = Seed(db);

        var expected = (from l in ls
            join r in rs on l.Key equals r.Key into g
            from r in g.Where(x => x.Id > 1)
            orderby l.Id, r.Id
            select new { l.Id, RId = r.Id }).ToList();

        var actual = (from l in db.Table<QgfsLeft>()
            join r in db.Table<QgfsRight>() on l.Key equals r.Key into g
            from r in g.Where(x => x.Id > 1)
            orderby l.Id, r.Id
            select new { l.Id, RId = r.Id }).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ChainedFiltersBeforeDefaultIfEmpty()
    {
        using TestDatabase db = new();
        var (ls, rs, _) = Seed(db);

        var expected = (from l in ls
            join r in rs on l.Key equals r.Key into g
            from r in g.Where(x => x.Id > 1).Where(x => x.RTag != "C").DefaultIfEmpty()
            orderby l.Id, r == null ? -1 : r.Id
            select new { l.Id, RId = r == null ? -1 : r.Id }).ToList();

        var actual = (from l in db.Table<QgfsLeft>()
            join r in db.Table<QgfsRight>() on l.Key equals r.Key into g
            from r in g.Where(x => x.Id > 1).Where(x => x.RTag != "C").DefaultIfEmpty()
            orderby l.Id, r == null ? -1 : r.Id
            select new { l.Id, RId = r == null ? -1 : r.Id }).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FilterUsingOuterRowBeforeDefaultIfEmpty()
    {
        using TestDatabase db = new();
        var (ls, rs, _) = Seed(db);

        var expected = (from l in ls
            join r in rs on l.Key equals r.Key into g
            from r in g.Where(x => x.Id >= l.MinId).DefaultIfEmpty()
            orderby l.Id, r == null ? -1 : r.Id
            select new { l.Id, RId = r == null ? -1 : r.Id }).ToList();

        var actual = (from l in db.Table<QgfsLeft>()
            join r in db.Table<QgfsRight>() on l.Key equals r.Key into g
            from r in g.Where(x => x.Id >= l.MinId).DefaultIfEmpty()
            orderby l.Id, r == null ? -1 : r.Id
            select new { l.Id, RId = r == null ? -1 : r.Id }).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FilterWithLiftedNullableComparisonBeforeDefaultIfEmpty()
    {
        using TestDatabase db = new();
        var (ls, rs, _) = Seed(db);

        var expected = (from l in ls
            join r in rs on l.Key equals r.Key into g
            from r in g.Where(x => x.NVal > 5).DefaultIfEmpty()
            orderby l.Id, r == null ? -1 : r.Id
            select new { l.Id, RId = r == null ? -1 : r.Id }).ToList();

        var actual = (from l in db.Table<QgfsLeft>()
            join r in db.Table<QgfsRight>() on l.Key equals r.Key into g
            from r in g.Where(x => x.NVal > 5).DefaultIfEmpty()
            orderby l.Id, r == null ? -1 : r.Id
            select new { l.Id, RId = r == null ? -1 : r.Id }).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FilterWithOrConditionBeforeDefaultIfEmpty()
    {
        using TestDatabase db = new();
        var (ls, rs, _) = Seed(db);

        var expected = (from l in ls
            join r in rs on l.Key equals r.Key into g
            from r in g.Where(x => x.Id == 1 || x.Id == 3).DefaultIfEmpty()
            orderby l.Id, r == null ? -1 : r.Id
            select new { l.Id, RId = r == null ? -1 : r.Id }).ToList();

        var actual = (from l in db.Table<QgfsLeft>()
            join r in db.Table<QgfsRight>() on l.Key equals r.Key into g
            from r in g.Where(x => x.Id == 1 || x.Id == 3).DefaultIfEmpty()
            orderby l.Id, r == null ? -1 : r.Id
            select new { l.Id, RId = r == null ? -1 : r.Id }).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FilterOnBareGroupWithoutDefaultIfEmptyUsingOuterRow()
    {
        using TestDatabase db = new();
        var (ls, rs, _) = Seed(db);

        var expected = (from l in ls
            join r in rs on l.Key equals r.Key into g
            from r in g.Where(x => x.Id >= l.MinId)
            orderby l.Id, r.Id
            select new { l.Id, RId = r.Id }).ToList();

        var actual = (from l in db.Table<QgfsLeft>()
            join r in db.Table<QgfsRight>() on l.Key equals r.Key into g
            from r in g.Where(x => x.Id >= l.MinId)
            orderby l.Id, r.Id
            select new { l.Id, RId = r.Id }).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InnerJoinThenBareGroupFlatten()
    {
        using TestDatabase db = new();
        var (ls, rs, es) = Seed(db);

        var expected = (from l in ls
            join e in es on l.Key equals e.Key
            join r in rs on l.Key equals r.Key into g
            from r in g
            orderby l.Id, e.Id, r.Id
            select new { l.Id, EId = e.Id, RId = r.Id }).ToList();

        var actual = (from l in db.Table<QgfsLeft>()
            join e in db.Table<QgfsExtra>() on l.Key equals e.Key
            join r in db.Table<QgfsRight>() on l.Key equals r.Key into g
            from r in g
            orderby l.Id, e.Id, r.Id
            select new { l.Id, EId = e.Id, RId = r.Id }).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BareGroupFlattenThenInnerJoin()
    {
        using TestDatabase db = new();
        var (ls, rs, es) = Seed(db);

        var expected = (from l in ls
            join r in rs on l.Key equals r.Key into g
            from r in g
            join e in es on r.Key equals e.Key
            orderby l.Id, r.Id, e.Id
            select new { l.Id, RId = r.Id, EId = e.Id }).ToList();

        var actual = (from l in db.Table<QgfsLeft>()
            join r in db.Table<QgfsRight>() on l.Key equals r.Key into g
            from r in g
            join e in db.Table<QgfsExtra>() on r.Key equals e.Key
            orderby l.Id, r.Id, e.Id
            select new { l.Id, RId = r.Id, EId = e.Id }).ToList();

        Assert.Equal(expected, actual);
    }
}

[Table("QgfsLefts")]
public class QgfsLeft
{
    [Key]
    public int Id { get; set; }

    public int Key { get; set; }

    public int MinId { get; set; }

    public required string Tag { get; set; }
}

[Table("QgfsRights")]
public class QgfsRight
{
    [Key]
    public int Id { get; set; }

    public int Key { get; set; }

    public int? NVal { get; set; }

    public required string RTag { get; set; }
}

[Table("QgfsExtras")]
public class QgfsExtra
{
    [Key]
    public int Id { get; set; }

    public int Key { get; set; }
}
