using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("pce_parent")]
public class PceParent
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("pce_child")]
public class PceChild
{
    [Key]
    public int Id { get; set; }

    public int ParentId { get; set; }

    public string? Title { get; set; }
}

public class PceReadOnlyWrap
{
    public PceReadOnlyWrap(string tag, PceChild? entity)
    {
        Tag = tag;
        Entity = entity;
    }

    public string Tag { get; }

    public PceChild? Entity { get; }
}

public class PceWritableWrap
{
    public PceWritableWrap(string tag, PceChild? entity)
    {
        Tag = tag;
        Entity = entity;
    }

    public string Tag { get; set; }

    public PceChild? Entity { get; set; }
}

public class PositionalConstructorEntityArgumentProjectionTests
{
    private static List<PceParent> Parents()
    {
        return
        [
            new PceParent { Id = 1, Name = "Ann" },
            new PceParent { Id = 2, Name = "Bob" },
            new PceParent { Id = 3, Name = "Cid" },
        ];
    }

    private static List<PceChild> Children()
    {
        return
        [
            new PceChild { Id = 10, ParentId = 1, Title = "t1" },
            new PceChild { Id = 11, ParentId = 3, Title = null },
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<PceParent>().Schema.CreateTable();
        db.Table<PceParent>().AddRange(Parents());
        db.Table<PceChild>().Schema.CreateTable();
        db.Table<PceChild>().AddRange(Children());
        return db;
    }

    [Fact]
    public void ReadOnlyPropertiesReceiveTheEntityArgument()
    {
        using TestDatabase db = Seed();
        List<PceParent> ps = Parents();
        List<PceChild> cs = Children();

        List<string> expected = (from p in ps
            join c in cs on p.Id equals c.ParentId into g
            from c in g.DefaultIfEmpty()
            select new PceReadOnlyWrap(p.Name, c))
            .OrderBy(x => x.Tag, StringComparer.Ordinal)
            .Select(x => x.Tag + "|" + (x.Entity == null ? "nullE" : "E" + x.Entity.Id + ":" + (x.Entity.Title ?? "-")))
            .ToList();

        List<string> actual = (from p in db.Table<PceParent>()
            join c in db.Table<PceChild>() on p.Id equals c.ParentId into g
            from c in g.DefaultIfEmpty()
            select new PceReadOnlyWrap(p.Name, c))
            .AsEnumerable()
            .OrderBy(x => x.Tag, StringComparer.Ordinal)
            .Select(x => x.Tag + "|" + (x.Entity == null ? "nullE" : "E" + x.Entity.Id + ":" + (x.Entity.Title ?? "-")))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WritablePropertiesReceiveTheEntityArgument()
    {
        using TestDatabase db = Seed();
        List<PceParent> ps = Parents();
        List<PceChild> cs = Children();

        List<string> expected = (from p in ps
            join c in cs on p.Id equals c.ParentId into g
            from c in g.DefaultIfEmpty()
            select new PceWritableWrap(p.Name, c))
            .OrderBy(x => x.Tag, StringComparer.Ordinal)
            .Select(x => x.Tag + "|" + (x.Entity == null ? "nullE" : "E" + x.Entity.Id + ":" + (x.Entity.Title ?? "-")))
            .ToList();

        List<string> actual = (from p in db.Table<PceParent>()
            join c in db.Table<PceChild>() on p.Id equals c.ParentId into g
            from c in g.DefaultIfEmpty()
            select new PceWritableWrap(p.Name, c))
            .AsEnumerable()
            .OrderBy(x => x.Tag, StringComparer.Ordinal)
            .Select(x => x.Tag + "|" + (x.Entity == null ? "nullE" : "E" + x.Entity.Id + ":" + (x.Entity.Title ?? "-")))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
