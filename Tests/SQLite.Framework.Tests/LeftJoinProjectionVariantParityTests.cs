using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ljv_root")]
public class LjvRoot
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("ljv_mid")]
public class LjvMid
{
    [Key]
    public int Id { get; set; }

    public int RootId { get; set; }

    public string Label { get; set; } = "";
}

[Table("ljv_leaf")]
public class LjvLeaf
{
    [Key]
    public int Id { get; set; }

    public int MidId { get; set; }

    [Column("leaf_text")]
    public string Text { get; set; } = "";
}

public class LjvWrap
{
    public LjvWrap(string tag)
    {
        Tag = tag;
    }

    public string Tag { get; }

    public LjvMid? Entity { get; set; }
}

public class LeftJoinProjectionVariantParityTests
{
    private static List<LjvRoot> Roots()
    {
        return
        [
            new LjvRoot { Id = 1, Name = "Ann" },
            new LjvRoot { Id = 2, Name = "Bob" },
            new LjvRoot { Id = 3, Name = "Cid" },
        ];
    }

    private static List<LjvMid> Mids()
    {
        return
        [
            new LjvMid { Id = 10, RootId = 1, Label = "m1" },
            new LjvMid { Id = 11, RootId = 2, Label = "m2" },
        ];
    }

    private static List<LjvLeaf> Leaves()
    {
        return
        [
            new LjvLeaf { Id = 100, MidId = 10, Text = "x1" },
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<LjvRoot>().Schema.CreateTable();
        db.Table<LjvRoot>().AddRange(Roots());
        db.Table<LjvMid>().Schema.CreateTable();
        db.Table<LjvMid>().AddRange(Mids());
        db.Table<LjvLeaf>().Schema.CreateTable();
        db.Table<LjvLeaf>().AddRange(Leaves());
        return db;
    }

    [Fact]
    public void ChainedLeftJoinsTwoNullableEntities()
    {
        using TestDatabase db = Seed();
        List<LjvRoot> rs = Roots();
        List<LjvMid> ms = Mids();
        List<LjvLeaf> ls = Leaves();

        List<(int, bool, bool, int, string)> expected = (from r in rs
                join m in ms on r.Id equals m.RootId into gm
                from m in gm.DefaultIfEmpty()
                join l in ls on (m == null ? -1 : m.Id) equals l.MidId into gl
                from l in gl.DefaultIfEmpty()
                select new { r.Id, M = m, L = l })
            .OrderBy(x => x.Id)
            .Select(x => (x.Id, x.M == null, x.L == null, x.M == null ? -1 : x.M.Id, x.L == null ? "" : x.L.Text))
            .ToList();

        List<(int, bool, bool, int, string)> actual = (from r in db.Table<LjvRoot>()
                join m in db.Table<LjvMid>() on r.Id equals m.RootId into gm
                from m in gm.DefaultIfEmpty()
                join l in db.Table<LjvLeaf>() on m.Id equals l.MidId into gl
                from l in gl.DefaultIfEmpty()
                select new { r.Id, M = m, L = l })
            .AsEnumerable()
            .OrderBy(x => x.Id)
            .Select(x => (x.Id, x.M == null, x.L == null, x.M == null ? -1 : x.M.Id, x.L == null ? "" : x.L.Text))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableEntityProjectedTwice()
    {
        using TestDatabase db = Seed();
        List<LjvRoot> rs = Roots();
        List<LjvMid> ms = Mids();

        List<(int, bool, bool)> expected = (from r in rs
                join m in ms on r.Id equals m.RootId into gm
                from m in gm.DefaultIfEmpty()
                select new { r.Id, A = m, B = m })
            .OrderBy(x => x.Id)
            .Select(x => (x.Id, x.A == null, x.B == null))
            .ToList();

        List<(int, bool, bool)> actual = (from r in db.Table<LjvRoot>()
                join m in db.Table<LjvMid>() on r.Id equals m.RootId into gm
                from m in gm.DefaultIfEmpty()
                select new { r.Id, A = m, B = m })
            .AsEnumerable()
            .OrderBy(x => x.Id)
            .Select(x => (x.Id, x.A == null, x.B == null))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConstructorArgumentWrapWithNullableEntityInitializer()
    {
        using TestDatabase db = Seed();
        List<LjvRoot> rs = Roots();
        List<LjvMid> ms = Mids();

        List<(string, bool, int)> expected = (from r in rs
                join m in ms on r.Id equals m.RootId into gm
                from m in gm.DefaultIfEmpty()
                select new LjvWrap(r.Name) { Entity = m })
            .OrderBy(x => x.Tag)
            .Select(x => (x.Tag, x.Entity == null, x.Entity == null ? -1 : x.Entity.Id))
            .ToList();

        List<(string, bool, int)> actual = (from r in db.Table<LjvRoot>()
                join m in db.Table<LjvMid>() on r.Id equals m.RootId into gm
                from m in gm.DefaultIfEmpty()
                select new LjvWrap(r.Name) { Entity = m })
            .AsEnumerable()
            .OrderBy(x => x.Tag)
            .Select(x => (x.Tag, x.Entity == null, x.Entity == null ? -1 : x.Entity.Id))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatOfLeftJoinNestedEntityProjections()
    {
        using TestDatabase db = Seed();
        List<LjvRoot> rs = Roots();
        List<LjvMid> ms = Mids();

        List<(int, bool, string)> expected = (from r in rs
                join m in ms on r.Id equals m.RootId into gm
                from m in gm.DefaultIfEmpty()
                where r.Id <= 1
                select new { r.Id, M = m })
            .Concat(from r in rs
                join m in ms on r.Id equals m.RootId into gm
                from m in gm.DefaultIfEmpty()
                where r.Id >= 3
                select new { r.Id, M = m })
            .OrderBy(x => x.Id)
            .Select(x => (x.Id, x.M == null, x.M == null ? "" : x.M.Label))
            .ToList();

        List<(int, bool, string)> actual = (from r in db.Table<LjvRoot>()
                join m in db.Table<LjvMid>() on r.Id equals m.RootId into gm
                from m in gm.DefaultIfEmpty()
                where r.Id <= 1
                select new { r.Id, M = m })
            .Concat(from r in db.Table<LjvRoot>()
                join m in db.Table<LjvMid>() on r.Id equals m.RootId into gm
                from m in gm.DefaultIfEmpty()
                where r.Id >= 3
                select new { r.Id, M = m })
            .AsEnumerable()
            .OrderBy(x => x.Id)
            .Select(x => (x.Id, x.M == null, x.M == null ? "" : x.M.Label))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RenamedColumnEntityNullsOrphan()
    {
        using TestDatabase db = Seed();
        List<LjvMid> ms = Mids();
        List<LjvLeaf> ls = Leaves();

        List<(int, bool, string)> expected = (from m in ms
                join l in ls on m.Id equals l.MidId into gl
                from l in gl.DefaultIfEmpty()
                select new { m.Id, W = new { L = l } })
            .OrderBy(x => x.Id)
            .Select(x => (x.Id, x.W.L == null, x.W.L == null ? "" : x.W.L.Text))
            .ToList();

        List<(int, bool, string)> actual = (from m in db.Table<LjvMid>()
                join l in db.Table<LjvLeaf>() on m.Id equals l.MidId into gl
                from l in gl.DefaultIfEmpty()
                select new { m.Id, W = new { L = l } })
            .AsEnumerable()
            .OrderBy(x => x.Id)
            .Select(x => (x.Id, x.W.L == null, x.W.L == null ? "" : x.W.L.Text))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
