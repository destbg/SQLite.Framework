using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ChainedLeftJoinParityTests
{
    private static (List<JoinEdgeLeft> Lefts, List<JoinEdgeRight> Rights) SeedSimple(TestDatabase db)
    {
        db.Table<JoinEdgeLeft>().Schema.CreateTable();
        db.Table<JoinEdgeRight>().Schema.CreateTable();

        List<JoinEdgeLeft> lefts = new()
        {
            new JoinEdgeLeft { Id = 1, Key = 10, NullableKey = 10, Tag = "L1" },
            new JoinEdgeLeft { Id = 2, Key = 20, NullableKey = null, Tag = "L2" },
            new JoinEdgeLeft { Id = 3, Key = 30, NullableKey = 30, Tag = "L3" },
            new JoinEdgeLeft { Id = 4, Key = 10, NullableKey = null, Tag = "L4" },
        };
        List<JoinEdgeRight> rights = new()
        {
            new JoinEdgeRight { Id = 1, Key = 10, NullableKey = 10, RightTag = "R1" },
            new JoinEdgeRight { Id = 2, Key = 10, NullableKey = null, RightTag = "R2" },
            new JoinEdgeRight { Id = 3, Key = 99, NullableKey = 30, RightTag = "R3" },
            new JoinEdgeRight { Id = 4, Key = 30, NullableKey = null, RightTag = "R4" },
        };

        foreach (JoinEdgeLeft l in lefts)
        {
            db.Table<JoinEdgeLeft>().Add(l);
        }

        foreach (JoinEdgeRight r in rights)
        {
            db.Table<JoinEdgeRight>().Add(r);
        }

        return (lefts, rights);
    }

    [Fact]
    public void LeftJoinSumOfNullableInner()
    {
        using TestDatabase db = new();
        var (lefts, rights) = SeedSimple(db);

        int expected = (from l in lefts
            join r in rights on l.Key equals r.Key into g
            from r in g.DefaultIfEmpty()
            select r == null ? 0 : r.Key).Sum();

        int actual = (from l in db.Table<JoinEdgeLeft>()
            join r in db.Table<JoinEdgeRight>() on l.Key equals r.Key into g
            from r in g.DefaultIfEmpty()
            select r == null ? 0 : r.Key).Sum();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TwoChainedLeftJoinsSimpleKeys()
    {
        using TestDatabase db = new();
        var (lefts, rights) = SeedSimple(db);
        db.Table<JoinEdgeWide>().Schema.CreateTable();
        List<JoinEdgeWide> wides = new()
        {
            new JoinEdgeWide { Id = 10, BigKey = 1 },
            new JoinEdgeWide { Id = 20, BigKey = 4 },
        };
        foreach (JoinEdgeWide w in wides)
        {
            db.Table<JoinEdgeWide>().Add(w);
        }

        var expected = (from l in lefts
            join r in rights on l.Key equals r.Key into rg
            from r in rg.DefaultIfEmpty()
            join w in wides on (long?)(r == null ? null : (int?)r.Id) equals w.BigKey into wg
            from w in wg.DefaultIfEmpty()
            orderby l.Id, r == null ? -1 : r.Id, w == null ? -1 : w.Id
            select new { l.Tag, RId = r == null ? -1 : r.Id, WId = w == null ? -1 : w.Id }).ToList();

        var actual = (from l in db.Table<JoinEdgeLeft>()
            join r in db.Table<JoinEdgeRight>() on l.Key equals r.Key into rg
            from r in rg.DefaultIfEmpty()
            join w in db.Table<JoinEdgeWide>() on (long?)(r == null ? null : (int?)r.Id) equals w.BigKey into wg
            from w in wg.DefaultIfEmpty()
            orderby l.Id, r == null ? -1 : r.Id, w == null ? -1 : w.Id
            select new { l.Tag, RId = r == null ? -1 : r.Id, WId = w == null ? -1 : w.Id }).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LeftJoinFollowedBySecondLeftJoin()
    {
        using TestDatabase db = new();
        var (lefts, rights) = SeedSimple(db);
        db.Table<JoinEdgeWide>().Schema.CreateTable();
        List<JoinEdgeWide> wides = new()
        {
            new JoinEdgeWide { Id = 1, BigKey = 1 },
        };
        db.Table<JoinEdgeWide>().Add(wides[0]);

        var expected = (from l in lefts
            join r in rights on l.Key equals r.Key into rg
            from r in rg.DefaultIfEmpty()
            join w in wides on (r == null ? -1L : r.Id) equals w.BigKey into wg
            from w in wg.DefaultIfEmpty()
            orderby l.Id, r == null ? -1 : r.Id, w == null ? -1 : w.Id
            select new { l.Tag, RId = r == null ? -1 : r.Id, WId = w == null ? -1 : w.Id }).ToList();

        var actual = (from l in db.Table<JoinEdgeLeft>()
            join r in db.Table<JoinEdgeRight>() on l.Key equals r.Key into rg
            from r in rg.DefaultIfEmpty()
            join w in db.Table<JoinEdgeWide>() on (r == null ? -1L : r.Id) equals w.BigKey into wg
            from w in wg.DefaultIfEmpty()
            orderby l.Id, r == null ? -1 : r.Id, w == null ? -1 : w.Id
            select new { l.Tag, RId = r == null ? -1 : r.Id, WId = w == null ? -1 : w.Id }).ToList();

        Assert.Equal(expected, actual);
    }
}

[Table("JoinEdgeLefts")]
public class JoinEdgeLeft
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("Key")]
    public int Key { get; set; }

    [Column("NullableKey")]
    public int? NullableKey { get; set; }

    [Column("Tag")]
    public required string Tag { get; set; }
}

[Table("JoinEdgeRights")]
public class JoinEdgeRight
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("Key")]
    public int Key { get; set; }

    [Column("NullableKey")]
    public int? NullableKey { get; set; }

    [Column("RightTag")]
    public required string RightTag { get; set; }
}

[Table("JoinEdgeWides")]
public class JoinEdgeWide
{
    [Key]
    [Column("Id")]
    public int Id { get; set; }

    [Column("BigKey")]
    public long BigKey { get; set; }
}
