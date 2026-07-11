using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ljn_we_parent")]
public class LjnWeParent
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("ljn_we_child")]
public class LjnWeChild
{
    [Key]
    public int Id { get; set; }

    public int ParentId { get; set; }

    public string Title { get; set; } = "";
}

public class LjnWeWrap
{
    public string? Tag { get; set; }

    public LjnWeChild? Entity { get; set; }
}

public class LeftJoinNestedWholeEntityProjectionTests
{
    private static List<LjnWeParent> Parents()
    {
        return
        [
            new LjnWeParent { Id = 1, Name = "Ann" },
            new LjnWeParent { Id = 2, Name = "Bob" },
        ];
    }

    private static List<LjnWeChild> Children()
    {
        return
        [
            new LjnWeChild { Id = 10, ParentId = 1, Title = "t1" },
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<LjnWeParent>().Schema.CreateTable();
        db.Table<LjnWeParent>().AddRange(Parents());
        db.Table<LjnWeChild>().Schema.CreateTable();
        db.Table<LjnWeChild>().AddRange(Children());
        return db;
    }

    [Fact]
    public void TopLevelAnonymousWholeEntityNullsOrphan()
    {
        using TestDatabase db = Seed();
        List<LjnWeParent> ps = Parents();
        List<LjnWeChild> cs = Children();

        List<(int, bool, int)> expected = (from p in ps
            join c in cs on p.Id equals c.ParentId into g
            from c in g.DefaultIfEmpty()
            select new { p.Id, Entity = c })
            .OrderBy(x => x.Id)
            .Select(x => (x.Id, x.Entity == null, x.Entity == null ? -1 : x.Entity.Id)).ToList();

        List<(int, bool, int)> actual = (from p in db.Table<LjnWeParent>()
            join c in db.Table<LjnWeChild>() on p.Id equals c.ParentId into g
            from c in g.DefaultIfEmpty()
            select new { p.Id, Entity = c })
            .AsEnumerable()
            .OrderBy(x => x.Id)
            .Select(x => (x.Id, x.Entity == null, x.Entity == null ? -1 : x.Entity.Id)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NestedAnonymousWholeEntityNullsOrphan()
    {
        using TestDatabase db = Seed();
        List<LjnWeParent> ps = Parents();
        List<LjnWeChild> cs = Children();

        List<(int, string, bool, int)> expected = (from p in ps
            join c in cs on p.Id equals c.ParentId into g
            from c in g.DefaultIfEmpty()
            select new { p.Id, W = new { Tag = p.Name, Entity = c } })
            .OrderBy(x => x.Id)
            .Select(x => (x.Id, x.W.Tag, x.W.Entity == null, x.W.Entity == null ? -1 : x.W.Entity.Id)).ToList();

        List<(int, string, bool, int)> actual = (from p in db.Table<LjnWeParent>()
            join c in db.Table<LjnWeChild>() on p.Id equals c.ParentId into g
            from c in g.DefaultIfEmpty()
            select new { p.Id, W = new { Tag = p.Name, Entity = c } })
            .AsEnumerable()
            .OrderBy(x => x.Id)
            .Select(x => (x.Id, x.W.Tag, x.W.Entity == null, x.W.Entity == null ? -1 : x.W.Entity.Id)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NestedNamedDtoWholeEntityNullsOrphan()
    {
        using TestDatabase db = Seed();
        List<LjnWeParent> ps = Parents();
        List<LjnWeChild> cs = Children();

        List<(int, string?, bool, int)> expected = (from p in ps
            join c in cs on p.Id equals c.ParentId into g
            from c in g.DefaultIfEmpty()
            select new { p.Id, W = new LjnWeWrap { Tag = p.Name, Entity = c } })
            .OrderBy(x => x.Id)
            .Select(x => (x.Id, x.W.Tag, x.W.Entity == null, x.W.Entity == null ? -1 : x.W.Entity.Id)).ToList();

        List<(int, string?, bool, int)> actual = (from p in db.Table<LjnWeParent>()
            join c in db.Table<LjnWeChild>() on p.Id equals c.ParentId into g
            from c in g.DefaultIfEmpty()
            select new { p.Id, W = new LjnWeWrap { Tag = p.Name, Entity = c } })
            .AsEnumerable()
            .OrderBy(x => x.Id)
            .Select(x => (x.Id, x.W.Tag, x.W.Entity == null, x.W.Entity == null ? -1 : x.W.Entity.Id)).ToList();

        Assert.Equal(expected, actual);
    }
}
